using System;
using System.Buffers;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Network.Bedrock;
using Network.Protocol.Messages;

namespace Network.Protocol.Transport
{
   /// <summary>
   /// Class to handle the common transport protocol.
   /// Based on BOLT8 and BOLT1
   /// </summary>
   public class TransportMessageSerializer : INetworkProtocolMessageSerializer
   {
      private readonly ILogger<TransportMessageSerializer> logger;
      private readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      private readonly NodeContext nodeContext;

      private NetworkPeerContext networkPeerContext;
      private IHandshakeProtocol handshakeProtocol;

      public TransportMessageSerializer(
         ILogger<TransportMessageSerializer> logger,
         INetworkMessageSerializerManager networkMessageSerializerManager,
         NodeContext nodeContext)
      {
         this.logger = logger;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.nodeContext = nodeContext;

         this.networkPeerContext = null!; //initialized by SetPeerContext
      }

      public void SetPeerContext(IPeerContext peerContext)
      {
         this.networkPeerContext = peerContext as NetworkPeerContext ?? throw new ArgumentException("Expected NetworkPeerContext", nameof(peerContext)); ;

         LightningEndpoint lightningEndpoint = null;
         if (peerContext.Direction == PeerConnectionDirection.Outbound)
         {
            OutgoingConnectionEndPoint endpoint = peerContext.Features.Get<OutgoingConnectionEndPoint>();

            if (endpoint == null || !endpoint.Items.TryGetValue(nameof(LightningEndpoint), out object res))
            {
               this.logger.LogError("Remote connection was not found ");
               throw new ApplicationException("Initiator connection must have a public key of the remote node");
            }

            lightningEndpoint = (LightningEndpoint)res;
         }

         this.handshakeProtocol = new HandshakeNoiseProtocol
         {
            Initiator = this.networkPeerContext.Direction == PeerConnectionDirection.Outbound,
            LocalPubKey = this.nodeContext.LocalPubKey,
            PrivateLey = this.nodeContext.PrivateLey,
            RemotePubKey = lightningEndpoint?.NodeId
         };

         this.networkPeerContext.SetHandshakeProtocol(this.handshakeProtocol);
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed,
         out SequencePosition examined, /*[MaybeNullWhen(false)]*/ out INetworkMessage message)
      {
         var reader = new SequenceReader<byte>(input);

         if (this.networkPeerContext.HandshakeComplete)
         {
            var decryptedOutput = new ArrayBufferWriter<byte>();
            this.handshakeProtocol.ReadMessage(reader.CurrentSpan, decryptedOutput);
            this.networkPeerContext.Metrics.Received(decryptedOutput.WrittenCount);

            // now try to read the payload
            var payload = new ReadOnlySequence<byte>(decryptedOutput.WrittenMemory);
            reader = new SequenceReader<byte>(payload);

            ushort command = reader.ReadUShort(isBigEndian: true);
            string commandName = command.ToString();
            examined = consumed = input.End;

            ushort payloadLength = reader.ReadUShort(isBigEndian: true);
            payload = payload.Slice(reader.Consumed, payloadLength);
            reader.Advance(payloadLength);

            if (this.networkMessageSerializerManager.TryDeserialize(
               commandName,
               ref payload,
               this.networkPeerContext.NegotiatedProtocolVersion.Version,
               this.networkPeerContext, out message!))
            {
               // extension: an optional TLV stream
               // TODO:

               return true;
            }
            else
            {
               this.logger.LogWarning("Serializer for message '{Command}' not found.", commandName);
               message = new UnknownMessage(commandName, payload.ToArray());
               this.networkPeerContext.Metrics.Wasted(decryptedOutput.WrittenCount);
               return true;
            }
         }
         else
         {
            // During the handshake the byte sequence is passed as is to the
            // underline processor which will handle serialization of the message.

            ReadOnlySequence<byte> payload = input.Slice(reader.Position, reader.Remaining);
            message = new HandshakeMessage { Payload = payload };
            consumed = payload.End;
            examined = consumed;
            return true;
         }
      }

      public void WriteMessage(INetworkMessage message, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         if (this.networkPeerContext.HandshakeComplete)
         {
            string command = message.Command;
            using (this.logger.BeginScope("Serializing and sending '{Command}'", command))
            {
               var payloadOutput = new ArrayBufferWriter<byte>();

               // type: write command type, a 2-byte big-endian field indicating the type of message
               payloadOutput.WriteUShort(ushort.Parse(command), isBigEndian: true);

               var serializationOutput = new ArrayBufferWriter<byte>();

               if (this.networkMessageSerializerManager.TrySerialize(
                  message,
                  this.networkPeerContext.NegotiatedProtocolVersion.Version,
                  this.networkPeerContext,
                  serializationOutput))
               {
                  // payload: a variable-length payload that comprises the remainder
                  // of the message and that conforms to a format matching the type
                  // The size of the message is required by the transport layer to fit
                  // into a 2-byte unsigned int; therefore, the maximum possible size is 65535 bytes.
                  payloadOutput.WriteUShort((ushort)serializationOutput.WrittenCount, isBigEndian: true);
                  payloadOutput.Write(serializationOutput.WrittenSpan);

                  // extension: an optional TLV stream
                  // TODO:

                  // encrypted Lightning message:
                  // 2-byte encrypted message length
                  // 16-byte MAC of the encrypted message length
                  // The encrypted Lightning message
                  // 16-byte MAC of the Lightning message
                  var encryptedOutput = new ArrayBufferWriter<byte>();
                  this.handshakeProtocol.WriteMessage(payloadOutput.WrittenSpan, encryptedOutput);

                  // write the lightning message to the underline buffer.
                  output.Write(encryptedOutput.WrittenSpan);

                  this.networkPeerContext.Metrics.Sent(payloadOutput.WrittenCount);
                  this.logger.LogDebug("Sent message '{Command}' with payload size {PayloadSize}.", command, payloadOutput.WrittenCount);
               }
               else
               {
                  this.logger.LogError("Serialize for message '{Command}' not found.", command);
               }
            }
         }
         else
         {
            using (this.logger.BeginScope("Noise handshake"))
            {
               // During the handshake the byte sequence is passed as is to the
               // remote peer which, the serialization was handled by the processor.

               if (message is HandshakeMessage noiseMessage)
               {
                  foreach (ReadOnlyMemory<byte> memory in noiseMessage.Payload)
                  {
                     output.Write(memory.Span);
                  }
               }
               else
               {
                  this.logger.LogError("Invalid handshake message");
                  throw new ApplicationException("Invalid handshake message");
               }
            }
         }
      }
   }
}
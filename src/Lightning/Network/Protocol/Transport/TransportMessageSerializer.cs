using System;
using System.Buffers;
using System.Runtime.Serialization;
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
      private readonly ILogger<TransportMessageSerializer> _logger;
      private readonly INetworkMessageSerializerManager _networkMessageSerializerManager;
      private readonly NodeContext _nodeContext;

      private NetworkPeerContext? _networkPeerContext;
      private IHandshakeProtocol _handshakeProtocol;
      private HandshakeState? _handshakeState;

      public TransportMessageSerializer(
         ILogger<TransportMessageSerializer> logger,
         INetworkMessageSerializerManager networkMessageSerializerManager,
         NodeContext nodeContext)
      {
         _logger = logger;
         _networkMessageSerializerManager = networkMessageSerializerManager;
         _nodeContext = nodeContext;
         _handshakeState = null;
         _networkPeerContext = null!; //initialized by SetPeerContext
      }

      public void SetPeerContext(IPeerContext peerContext)
      {
         _networkPeerContext = peerContext as NetworkPeerContext ?? throw new ArgumentException("Expected NetworkPeerContext", nameof(peerContext)); ;
         _handshakeState = new HandshakeState(_networkPeerContext.Direction == PeerConnectionDirection.Outbound);

         LightningEndpoint lightningEndpoint = null;
         if (peerContext.Direction == PeerConnectionDirection.Outbound)
         {
            OutgoingConnectionEndPoint endpoint = peerContext.Features.Get<OutgoingConnectionEndPoint>();

            if (endpoint == null || !endpoint.Items.TryGetValue(nameof(LightningEndpoint), out object res))
            {
               _logger.LogError("Remote connection was not found ");
               throw new ApplicationException("Initiator connection must have a public key of the remote node");
            }

            lightningEndpoint = (LightningEndpoint)res;
         }

         _handshakeProtocol = new HandshakeNoiseProtocol
         {
            Initiator = _networkPeerContext.Direction == PeerConnectionDirection.Outbound,
            LocalPubKey = _nodeContext.LocalPubKey,
            PrivateLey = _nodeContext.PrivateLey,
            RemotePubKey = lightningEndpoint?.NodeId
         };

         _networkPeerContext.SetHandshakeProtocol(_handshakeProtocol);
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, /*[MaybeNullWhen(false)]*/  out INetworkMessage message)
      {
         var reader = new SequenceReader<byte>(input);

         if (_networkPeerContext.HandshakeComplete)
         {
            const int headerLength = 18;
            if (reader.Remaining < headerLength)
            {
               // the payload header is 18 bytes (2 byte payload length and 16 byte MAC)
               // if the buffer does not have that amount of bytes we wait for more
               // bytes to arrive in the stream before parsing the header
               message = default;
               return false;
            }

            ReadOnlySequence<byte> encryptedHeader = reader.Sequence.Slice(reader.Position, headerLength);

            // decrypt the message length
            long encryptedMessageLength = _handshakeProtocol.ReadMessageLength(encryptedHeader);

            if (reader.Remaining < headerLength + encryptedMessageLength)
            {
               // if the reader does not have the entire length of the header
               // and message we wait for more data from the stream
               message = default;
               return false;
            }

            var decryptedOutput = new ArrayBufferWriter<byte>();
            _handshakeProtocol.ReadMessage(reader.CurrentSpan.Slice(0, headerLength + (int)encryptedMessageLength), decryptedOutput);
            _networkPeerContext.Metrics.Received(headerLength + (int)encryptedMessageLength);
            reader.Advance(headerLength + encryptedMessageLength);
            examined = consumed = reader.Position;

            // now try to read the payload
            var payload = new ReadOnlySequence<byte>(decryptedOutput.WrittenMemory);
            var payloadReader = new SequenceReader<byte>(payload);

            ushort command = payloadReader.ReadUShort(isBigEndian: true);
            string commandName = command.ToString();

            ushort payloadLength = payloadReader.ReadUShort(isBigEndian: true);
            payload = payload.Slice(payloadReader.Consumed, payloadLength);
            payloadReader.Advance(payloadLength);

            if (_networkMessageSerializerManager.TryDeserialize(
               commandName,
               ref payload,
               _networkPeerContext.NegotiatedProtocolVersion.Version,
               _networkPeerContext, out message!))
            {
               return true;
            }
            else
            {
               _logger.LogWarning("Serializer for message '{Command}' not found.", commandName);
               message = new UnknownMessage(commandName, payload.ToArray());
               _networkPeerContext.Metrics.Wasted(decryptedOutput.WrittenCount);
               return true;
            }
         }
         else
         {
            // During the handshake the byte sequence is passed as is to the
            // underline processor which will handle serialization of the message.

            long nextLength = _handshakeState.NextLength();
            if (reader.Remaining >= nextLength)
            {
               message = new HandshakeMessage { Payload = input.Slice(reader.Position, nextLength) };
               reader.Advance(nextLength);
               examined = consumed = reader.Position;
               _handshakeState.Advance();
               return true;
            }

            message = default;
            return false;
         }
      }

      public void WriteMessage(INetworkMessage message, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         if (_networkPeerContext.HandshakeComplete)
         {
            string command = message.Command;
            using (_logger.BeginScope("Serializing and sending '{Command}'", command))
            {
               var payloadOutput = new ArrayBufferWriter<byte>();

               // type: write command type, a 2-byte big-endian field indicating the type of message
               payloadOutput.WriteUShort(ushort.Parse(command), isBigEndian: true);

               var serializationOutput = new ArrayBufferWriter<byte>();

               if (_networkMessageSerializerManager.TrySerialize(
                  message,
                  _networkPeerContext.NegotiatedProtocolVersion.Version,
                  _networkPeerContext,
                  serializationOutput))
               {
                  // payload: a variable-length payload that comprises the remainder
                  // of the message and that conforms to a format matching the type
                  // The size of the message is required by the transport layer to fit
                  // into a 2-byte unsigned int; therefore, the maximum possible size is 65535 bytes.
                  payloadOutput.WriteUShort((ushort)serializationOutput.WrittenCount, isBigEndian: true);
                  payloadOutput.Write(serializationOutput.WrittenSpan);

                  // encrypted Lightning message:
                  // 2-byte encrypted message length
                  // 16-byte MAC of the encrypted message length
                  // The encrypted Lightning message
                  // 16-byte MAC of the Lightning message
                  var encryptedOutput = new ArrayBufferWriter<byte>();
                  _handshakeProtocol.WriteMessage(payloadOutput.WrittenSpan, encryptedOutput);

                  // write the lightning message to the underline buffer.
                  output.Write(encryptedOutput.WrittenSpan);

                  _networkPeerContext.Metrics.Sent(payloadOutput.WrittenCount);
                  _logger.LogDebug("Sent message '{Command}' with payload size {PayloadSize}.", command, payloadOutput.WrittenCount);
               }
               else
               {
                  _logger.LogError("Serialize for message '{Command}' not found.", command);
               }
            }
         }
         else
         {
            using (_logger.BeginScope("Noise handshake"))
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
                  _logger.LogError("Invalid handshake message");
                  throw new ApplicationException("Invalid handshake message");
               }
            }
         }
      }

      internal class HandshakeState
      {
         public HandshakeState(bool initiator)
         {
            _initiator = initiator;
            _position = 1;
         }

         private readonly bool _initiator;
         private byte _position;

         public long NextLength()
         {
            if (_initiator)
            {
               if (_position == 1)
               {
                  return 50; // act two
               }

               throw new SerializationException("Invalid handshake state");
            }
            else
            {
               if (_position == 1)
               {
                  return 50; // act two
               }

               if (_position == 2)
               {
                  return 66; // act two
               }

               throw new SerializationException("Invalid handshake state");
            }
         }

         public void Advance()
         {
            _position++;
         }
      }
   }
}
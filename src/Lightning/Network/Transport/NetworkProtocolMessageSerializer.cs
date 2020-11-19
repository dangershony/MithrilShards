using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Example.Protocol.Messages;
using MithrilShards.Example.Protocol.Serialization;
using MithrilShards.Network.Bedrock;
using Network.Transport;

namespace MithrilShards.Example.Network.Bedrock
{
   /// <summary>
   /// Class to handle the common transport protocol.
   /// Based on BOLT8 and BOLT1
   /// </summary>
   public class NetworkProtocolMessageSerializer : INetworkProtocolMessageSerializer
   {
      private readonly ILogger<NetworkProtocolMessageSerializer> logger;
      private readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      private readonly DeserializationContext deserializationContext;

      private NetworkPeerContext peerContext;

      public NetworkProtocolMessageSerializer(
         ILogger<NetworkProtocolMessageSerializer> logger,
         INetworkMessageSerializerManager networkMessageSerializerManager)
      {
         this.logger = logger;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.deserializationContext = new DeserializationContext();

         this.peerContext = null!; //initialized by SetPeerContext
      }

      public void SetPeerContext(IPeerContext peerContext)
      {
         this.peerContext = peerContext as NetworkPeerContext ?? throw new ArgumentException("Expected NetworkPeerContext", nameof(peerContext)); ;
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, /*[MaybeNullWhen(false)]*/ out INetworkMessage message)
      {
         var reader = new SequenceReader<byte>(input);

         if (this.peerContext.Handshaked)
         {
            // now try to read the payload
            if (reader.Remaining >= this.deserializationContext.PayloadLength)
            {
               ReadOnlySequence<byte> payload = input.Slice(reader.Position, this.deserializationContext.PayloadLength);

               //check checksum
               //we consumed and examined everything, no matter if the message was a known message or not
               examined = consumed = payload.End;
               this.deserializationContext.ResetFlags();

               string commandName = this.deserializationContext.CommandName!;
               if (this.networkMessageSerializerManager
                  .TryDeserialize(commandName, ref payload, this.peerContext.NegotiatedProtocolVersion.Version,
                     this.peerContext, out message!))
               {
                  this.peerContext.Metrics.Received(this.deserializationContext.GetTotalMessageLength());
                  return true;
               }
               else
               {
                  this.logger.LogWarning("Serializer for message '{Command}' not found.", commandName);
                  message = new UnknownMessage(commandName, payload.ToArray());
                  this.peerContext.Metrics.Wasted(this.deserializationContext.GetTotalMessageLength());
                  return true;
               }
            }
         }
         else
         {
         }

         // not enough data do read the full payload, so mark as examined the whole reader but let consumed just consume the expected payload length.
         consumed = reader.Position;
         examined = input.End;
         message = default!;
         return false;
      }

      public void WriteMessage(INetworkMessage message, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         if (this.peerContext.Handshaked)
         {
            this.MessageSend(message, output);
         }
         else
         {
            this.HandshakeSend(message, output);
         }
      }

      private void MessageSend(INetworkMessage message, IBufferWriter<byte> output)
      {
         string command = message.Command;
         using (this.logger.BeginScope("Serializing and sending '{Command}'", command))
         {
            var payloadOutput = new ArrayBufferWriter<byte>();

            // write command name
            payloadOutput.WriteInt(int.Parse(command));

            if (this.networkMessageSerializerManager.TrySerialize(message,
               this.peerContext.NegotiatedProtocolVersion.Version,
               this.peerContext,
               payloadOutput))
            {
               int payloadSize = payloadOutput.WrittenCount;

               var encryptdOutput = new ArrayBufferWriter<byte>();

               NoiseProtocol.Encrypt(payloadOutput.WrittenSpan, encryptdOutput);

               output.Write(encryptdOutput.WrittenSpan);

               this.peerContext.Metrics.Sent(payloadSize);
               this.logger.LogDebug("Sent message '{Command}' with payload size {PayloadSize}.", command,
                  payloadSize);
            }
            else
            {
               this.logger.LogError("Serialize for message '{Command}' not found.", command);
            }
         }
      }

      private void HandshakeSend(INetworkMessage message, IBufferWriter<byte> output)
      {
         using (this.logger.BeginScope("Noise handshake"))
         {
            if (message is NoiseMessage noiseMessage)
            {
               foreach (ReadOnlyMemory<byte> memory in noiseMessage.Payload)
               {
                  output.Write(memory.Span);
               }
            }
            else
            {
               this.logger.LogError("Invalid noise message");
            }
         }
      }
   }
}
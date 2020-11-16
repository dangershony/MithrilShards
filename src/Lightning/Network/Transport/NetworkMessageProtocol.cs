using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Crypto;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Network.Bedrock
{
   public class NetworkMessageProtocol : IMessageReader<TransportMessage>, IMessageWriter<TransportMessage>
   {
      private readonly ILogger<NetworkMessageProtocol> logger;
      private readonly INetworkDefinition chainDefinition;
      private readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      private readonly ConnectionContextData contextData;
      private IPeerContext peerContext;

      public NetworkMessageProtocol(ILogger<NetworkMessageProtocol> logger,
         INetworkMessageSerializerManager networkMessageSerializerManager,
         ConnectionContextData contextData)
      {
         this.logger = logger;
         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.contextData = contextData;

         this.peerContext = null!; //initialized by SetPeerContext
      }

      internal void SetPeerContext(IPeerContext peerContext)
      {
         this.peerContext = peerContext;
      }

      public bool TryParseMessage(
         in ReadOnlySequence<byte> input,
         ref SequencePosition consumed,
         ref SequencePosition examined,
         /*[MaybeNullWhen(false)]*/ out TransportMessage message)
      {
         var reader = new SequenceReader<byte>(input);

         if (this.TryReadHeader(ref reader))
         {
            // now try to read the payload
            if (reader.Remaining >= this.contextData.PayloadLength)
            {
               ReadOnlySequence<byte> payload = input.Slice(reader.Position, this.contextData.PayloadLength);

               //we consumed and examined everything, no matter if the message was a known message or not
               examined = consumed = payload.End;
               this.contextData.ResetFlags();

               string commandName = this.contextData.CommandName!;
               if (this.networkMessageSerializerManager
                  .TryDeserialize(commandName, ref payload, this.peerContext.NegotiatedProtocolVersion.Version,
                     this.peerContext, out message!))
               {
                  this.peerContext.Metrics.Received(this.contextData.GetTotalMessageLength());
                  return true;
               }
               else
               {
                  this.logger.LogWarning("Serializer for message '{Command}' not found.", commandName);
                  message = new UnknownMessage(commandName, payload.ToArray());
                  this.peerContext.Metrics.Wasted(this.contextData.GetTotalMessageLength());
                  return true;
               }
            }
         }

         // not enough data do read the full payload, so mark as examined the whole reader but let consumed just consume the expected payload length.
         consumed = reader.Position;
         examined = input.End;
         message = default!;
         return false;
      }

      private bool TryReadHeader(ref SequenceReader<byte> reader)
      {
         if (!this.contextData.PayloadLengthRead)
         {
            if (!this.TryReadPayloadLenght(ref reader))
            {
               return false;
            }
         }

         return true;
      }

      /// <summary>
      /// Tries to read the payload length from the buffer.
      /// <paramref name="reader"/> doesn't advance if failing, otherwise it advance by <see cref="SIZE_PAYLOAD_LENGTH"/>
      /// </summary>
      /// <param name="reader">The reader.</param>
      /// <returns>true if the payload length has been read, false otherwise (not enough bytes to read)</returns>
      private bool TryReadPayloadLenght(ref SequenceReader<byte> reader)
      {
         if (reader.TryReadLittleEndian(out int payloadLengthBytes))
         {
            this.contextData.PayloadLength = (uint)payloadLengthBytes;
            return true;
         }
         else
         {
            return false;
         }
      }

      public void WriteMessage(TransportMessage message, IBufferWriter<byte> output)
      {
         using (this.logger.BeginScope("Serializing and sending '{Command}'", message.Command))
         {
            if (this.networkMessageSerializerManager.TrySerialize(message,
               this.peerContext.NegotiatedProtocolVersion.Version,
               this.peerContext,
               output,
               out int sentBytes))
            {
               this.peerContext.Metrics.Sent(sentBytes);
               this.logger.LogDebug("Sent message '{Command}'.", message.Command);
            }
            else
            {
               this.logger.LogError("Serializer for message '{Command}' not found.", message.Command);
            }
         }
      }
   }

   public class NetworkMessageWriter1 : INetworkMessageWriter
   {
      private readonly ProtocolWriter writer;
      private readonly IMessageWriter<INetworkMessage> messageWriter;

      public NetworkMessageWriter(IMessageWriter<INetworkMessage> messageWriter, ProtocolWriter writer)
      {
         this.messageWriter = messageWriter;
         this.writer = writer;
      }

      public ValueTask WriteAsync(INetworkMessage message, CancellationToken cancellationToken = default)
      {
         return this.writer.WriteAsync(this.messageWriter, message, cancellationToken);
      }

      public ValueTask WriteManyAsync(IEnumerable<INetworkMessage> messages, CancellationToken cancellationToken = default)
      {
         return this.writer.WriteManyAsync(this.messageWriter, messages, cancellationToken);
      }
   }
}
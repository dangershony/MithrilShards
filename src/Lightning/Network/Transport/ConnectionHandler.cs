using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server.Guards;

namespace MithrilShards.Network.Bedrock
{
   public interface ITransportConnectionHandler
   {
      IPeerContext PeerContext { get; }

      Task SendMessageAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default);

      void OnReceive(Func<TransportMessage, Task> subscription);
   }

   public struct TransportMessage
   {
      public TransportMessage(byte[] payload) : this(new ReadOnlySequence<byte>(payload))
      {
      }

      public TransportMessage(ReadOnlySequence<byte> payload)
      {
         this.Payload = payload;
      }

      public ReadOnlySequence<byte> Payload { get; }
   }

   public class TransportConnectionHandler : ITransportConnectionHandler
   {
      protected readonly ILogger logger;
      private readonly IEventBus eventBus;
      private readonly INetworkMessageProcessorFactory networkMessageProcessorFactory;
      private readonly ILoggerFactory loggerFactory;

      private readonly INetworkMessageSerializerManager networkMessageSerializerManager;
      private NetworkMessageWriter networkMessageWriter;
      private Func<TransportMessage, Task> messageReceivedEvent;

      public TransportConnectionHandler(
         ILogger<TransportConnectionHandler> logger,
         IEventBus eventBus,
         INetworkMessageProcessorFactory networkMessageProcessorFactory,
         ILoggerFactory loggerFactory,
         INetworkMessageSerializerManager networkMessageSerializerManager)
      {
         this.logger = logger;
         this.loggerFactory = loggerFactory;

         this.networkMessageSerializerManager = networkMessageSerializerManager;
         this.eventBus = eventBus;
         this.networkMessageProcessorFactory = networkMessageProcessorFactory;
      }

      public IPeerContext PeerContext { get; private set; }

      public async Task SendMessageAsync(TransportMessage transportMessage, CancellationToken cancellationToken = default)
      {
         await this.networkMessageWriter.WriteAsync(transportMessage, cancellationToken).ConfigureAwait(false);
      }

      public void OnReceive(Func<TransportMessage, Task> message)
      {
         this.messageReceivedEvent = message;
      }

      protected async Task ProcessMessageAsync(INetworkMessage message,
         ConnectionContextData contextData,
         IPeerContext peerContext,
         CancellationToken cancellation)
      {
      }

      public async Task OnConnectedAsync(Microsoft.AspNetCore.Connections.ConnectionContext connection)
      {
         if (connection is null)
         {
            throw new ArgumentNullException(nameof(connection));
         }

         using IDisposable logScope = this.logger.BeginScope("Peer {PeerId} connected to server {ServerEndpoint}", connection.ConnectionId, connection.LocalEndPoint);
         var contextData = new ConnectionContextData();
         var protocol = new NetworkMessageProtocol(this.loggerFactory.CreateLogger<NetworkMessageProtocol>(),
                                                   this.networkMessageSerializerManager,
                                                   contextData);

         ProtocolReader reader = connection.CreateReader();
         ProtocolWriter writer = connection.CreateWriter();
         this.networkMessageWriter = new NetworkMessageWriter(protocol, writer);

         this.PeerContext = new PeerContext(
            null,
            this.eventBus,
            PeerConnectionDirection.Inbound,
            connection.ConnectionId,
            connection.LocalEndPoint,
            null,
            connection.RemoteEndPoint,
            this.networkMessageWriter);

         connection.ConnectionClosed = this.PeerContext.ConnectionCancellationTokenSource.Token;

         connection.Features.Set(this.PeerContext);
         protocol.SetPeerContext(this.PeerContext);

         this.eventBus.Publish(new PeerConnected(this.PeerContext));

         await this.networkMessageProcessorFactory.StartProcessorsAsync(this.PeerContext).ConfigureAwait(false);

         while (true)
         {
            if (connection.ConnectionClosed.IsCancellationRequested)
            {
               break;
            }

            try
            {
               ProtocolReadResult<INetworkMessage> result = await reader.ReadAsync(protocol, connection.ConnectionClosed).ConfigureAwait(false);

               if (result.IsCompleted)
               {
                  break;
               }

               using IDisposable logScope = this.logger.BeginScope("Processing message '{Command}'", message.Command);
               this.logger.LogDebug("Parsing message '{Command}' with size of {PayloadSize}", message.Command, contextData.PayloadLength);

               this.messageReceivedEvent?.Invoke(message);

               await this.ProcessMessageAsync(result.Message, contextData, this.PeerContext, connection.ConnectionClosed).WithCancellationAsync(connection.ConnectionClosed).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
               break;
            }
            finally
            {
               reader.Advance();
            }
         }

         return;
      }
   }
}
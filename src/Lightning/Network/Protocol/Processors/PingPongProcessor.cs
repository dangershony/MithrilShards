using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Threading;
using Network.Protocol.Messages;

namespace Network.Protocol.Processors
{
   public class PingPongProcessor : BaseProcessor,
      INetworkMessageHandler<PingMessage>,
      INetworkMessageHandler<PongMessage>
   {
      const int PING_INTERVAL_SECS = 30;
      
      private readonly IDateTimeProvider _dateTimeProvider;
      private readonly IPeriodicWork _periodicWork;
      readonly IRandomNumberGenerator _numberGenerator;
      
      private DateTime? _lastPingReceivedDateTime;
      //private ushort? _lastPingBytesLen = null;
      private readonly ConcurrentDictionary<ushort,TrackedPingMessage> _lastSentPingMessages = 
         new ConcurrentDictionary<ushort, TrackedPingMessage>();
      
      public PingPongProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, IDateTimeProvider dateTimeProvider, IPeriodicWork periodicWork, IRandomNumberGenerator numberGenerator) 
         : base(logger, eventBus, peerBehaviorManager, true)
      {
         _dateTimeProvider = dateTimeProvider;
         _periodicWork = periodicWork;
         _numberGenerator = numberGenerator;
      }

      public async ValueTask<bool> ProcessMessageAsync(PingMessage message, CancellationToken cancellation) //TODO David add tests
      {
         logger.LogDebug($"Processing ping from {PeerContext.PeerId}");
         
         if(_lastPingReceivedDateTime > _dateTimeProvider.GetUtcNow()
            .AddSeconds(-30))
            throw new ProtocolViolationException("TODO David check how we fail a channel from here");

         if (message.NumPongBytes > PingMessage.MAX_BYTES_LEN)
            return false;

         _lastPingReceivedDateTime = _dateTimeProvider.GetUtcNow();
         
         await SendMessageAsync(new PongMessage {BytesLen = message.NumPongBytes,
            Ignored = new byte[message.NumPongBytes]}, cancellation)
            .ConfigureAwait(false);

         logger.LogDebug($"Send pong to {PeerContext.PeerId} with length {message.NumPongBytes}");
         
         // will prevent to handle noise messages to other Processors
         return false;
      }

      public ValueTask<bool> ProcessMessageAsync(PongMessage message, CancellationToken cancellation)
      {
         logger.LogDebug($"Processing pong from {PeerContext.PeerId} with length {message.BytesLen}");
         
         if(_lastSentPingMessages.ContainsKey(message.BytesLen) && 
            _lastSentPingMessages.Remove(message.BytesLen,out var trackedPingMessage))
         {
            logger.LogDebug($"Pong received for ping after {_dateTimeProvider.GetUtcNow() - trackedPingMessage.dateTimeSent} on" +
                            $"{PeerContext.PeerId}");
         }

         return new ValueTask<bool>(false);
      }

      private async Task PingAsync(CancellationToken cancellationToken)
      {
         if(!PeerContext.InitComplete || cancellationToken.IsCancellationRequested)
            return;

         var bytesLength = _numberGenerator.GetUint32() % PingMessage.MAX_BYTES_LEN;
         
         while(_lastSentPingMessages.ContainsKey((ushort)bytesLength))
            bytesLength = _numberGenerator.GetUint32() % PingMessage.MAX_BYTES_LEN;
         
         var pingMessage = new PingMessage((ushort)bytesLength);

         _lastSentPingMessages.GetOrAdd(pingMessage.NumPongBytes, _
            => new TrackedPingMessage(pingMessage, _dateTimeProvider.GetUtcNow()));

         logger.LogDebug($"Sending ping to {PeerContext.PeerId} with pong length of {pingMessage.NumPongBytes}");
         
         await SendMessageAsync(pingMessage, cancellationToken).ConfigureAwait(false);
      }
      
      protected override ValueTask OnPeerHandshakedAsync()
      {
         _ = _periodicWork
            .StartAsync(
            label: $"{nameof(_periodicWork)}-{PeerContext.PeerId}",
            work: PingAsync,
            interval: TimeSpan.FromSeconds(PING_INTERVAL_SECS),
            cancellation: PeerContext.ConnectionCancellationTokenSource.Token
         );

         return default;
      }

      private class TrackedPingMessage
      {
         public PingMessage pingMessage;
         public DateTime dateTimeSent;

         public TrackedPingMessage(PingMessage pingMessage, DateTime dateTimeSent)
         {
            this.pingMessage = pingMessage;
            this.dateTimeSent = dateTimeSent;
         }
      }
   }
}
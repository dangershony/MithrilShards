﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Statistics;

namespace MithrilShards.Core.Network.PeerBehaviorManager
{
   public partial class PeerBehaviorManager : IPeerBehaviorManager, IDisposable
   {
      private const int INITIAL_SCORE = 0;
      private readonly ILogger<PeerBehaviorManager> logger;
      private readonly IEventBus eventBus;
      private readonly Dictionary<string, PeerScore> connectedPeers = new Dictionary<string, PeerScore>();

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager eventSubscriptionManager = new EventSubscriptionManager();

      public PeerBehaviorManager(ILogger<PeerBehaviorManager> logger, IEventBus eventBus, IStatisticFeedsCollector statisticFeedsCollector)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.statisticFeedsCollector = statisticFeedsCollector;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         this.RegisterStatisticFeeds();
         this.eventSubscriptionManager.RegisterSubscriptions(
            this.eventBus.Subscribe<PeerConnected>(this.AddConnectedPeer),
            this.eventBus.Subscribe<PeerDisconnected>(this.RemoveConnectedPeer)
            );

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         this.Dispose();
         return Task.CompletedTask;
      }


      public void Misbehave(IPeerContext peerContext, uint penality, string reason)
      {
         if (!this.connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore score))
         {
            this.logger.LogWarning("Cannot attribute bad behavior to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
         }
         else
         {
            this.logger.LogDebug("Peer {PeerId} misbehave: {MisbehaveReason}.", peerContext.PeerId, reason);
            int currentResult = score.UpdateScore(-(int)penality);
         }
      }

      public void AddBonus(IPeerContext peerContext, uint bonus, string reason)
      {
         if (!this.connectedPeers.TryGetValue(peerContext.PeerId, out PeerScore score))
         {
            this.logger.LogWarning("Cannot attribute positive points to the peer {PeerId} because the peer isn't connected.", peerContext.PeerId);
         }
         else
         {
            this.logger.LogDebug("Peer {PeerId} got a bonus {PeerBonus}: {MisbehaveReason}.", peerContext.PeerId, bonus, reason);
            score.UpdateScore((int)bonus);
         }
      }

      /// <summary>
      /// Adds the specified peer to the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void AddConnectedPeer(PeerConnected @event)
      {
         this.connectedPeers[@event.PeerContext.PeerId] = new PeerScore(@event.PeerContext, INITIAL_SCORE);
         this.logger.LogDebug("Added peer {PeerId} to the list of connected peers", @event.PeerContext.PeerId);
      }

      /// <summary>
      /// Removes the specified peer from the list of connected peer.
      /// </summary>
      /// <param name="peerContext">The peer context.</param>
      private void RemoveConnectedPeer(PeerDisconnected @event)
      {
         if (!this.connectedPeers.Remove(@event.PeerContext.PeerId))
         {
            this.logger.LogWarning("Cannot remove peer {PeerId}, peer not found", @event.PeerContext.PeerId);
         }
         else
         {
            this.logger.LogInformation("Peer {PeerId} disconnected.", @event.PeerContext.PeerId);
         }
      }

      public void Dispose()
      {
         this.eventSubscriptionManager.Dispose();
      }
   }
}
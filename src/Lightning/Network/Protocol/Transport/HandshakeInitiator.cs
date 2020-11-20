using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Events;

namespace Network.Protocol.Transport
{
   public class HandshakeInitiator
   {
      private readonly ILogger logger;
      private readonly IEventBus eventBus;
      private SubscriptionToken subscriptionToken;

      public HandshakeInitiator(ILogger logger, IEventBus eventBus)
      {
         this.logger = logger;
         this.eventBus = eventBus;

         this.subscriptionToken = this.eventBus.Subscribe<PeerConnected>(this.PeerConnected);
      }

      private void PeerConnected(PeerConnected @event)
      {
         // only listen
         this.subscriptionToken.Dispose();
      }
   }
}
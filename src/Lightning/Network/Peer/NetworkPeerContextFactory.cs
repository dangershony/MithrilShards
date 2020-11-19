using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;

namespace Network.Peer
{
   public class NetworkPeerContextFactory : PeerContextFactory<NetworkPeerContext>
   {
      public NetworkPeerContextFactory(ILogger<PeerContextFactory<NetworkPeerContext>> logger, IEventBus eventBus, ILoggerFactory loggerFactory, IOptions<ForgeConnectivitySettings> serverSettings)
         : base(logger, eventBus, loggerFactory, serverSettings)
      {
      }
   }
}
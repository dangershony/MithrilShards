using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using Network.Transport;

namespace MithrilShards.Example.Network
{
   public class NetworkPeerContextFactory : PeerContextFactory<NetworkPeerContext>
   {
      public NetworkPeerContextFactory(ILogger<PeerContextFactory<NetworkPeerContext>> logger, IEventBus eventBus, ILoggerFactory loggerFactory, IOptions<ForgeConnectivitySettings> serverSettings)
         : base(logger, eventBus, loggerFactory, serverSettings)
      {
      }
   }
}
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol;

namespace Network.Protocol
{
   public class NetworkPeerContextFactory : PeerContextFactory<NetworkPeerContext>
   {
      public NetworkPeerContextFactory(
         ILogger<PeerContextFactory<NetworkPeerContext>> logger,
         IEventBus eventBus,
         ILoggerFactory loggerFactory,
         IOptions<ForgeConnectivitySettings> serverSettings)
         : base(logger, eventBus, loggerFactory, serverSettings)
      {
      }

      public override IPeerContext CreateOutgoingPeerContext(
         string peerId,
         EndPoint localEndPoint,
         OutgoingConnectionEndPoint outgoingConnectionEndPoint,
         INetworkMessageWriter messageWriter)
      {
         var peerContext = (NetworkPeerContext)base.CreateOutgoingPeerContext(peerId, localEndPoint, outgoingConnectionEndPoint, messageWriter);

         // At this point we can enrich the context from the DI

         return peerContext;
      }

      public override IPeerContext CreateIncomingPeerContext(
         string peerId,
         EndPoint localEndPoint,
         EndPoint remoteEndPoint,
         INetworkMessageWriter messageWriter)
      {
         var peerContext = (NetworkPeerContext)base.CreateIncomingPeerContext(peerId, localEndPoint, remoteEndPoint, messageWriter);

         // At this point we can enrich the context from the DI

         return peerContext;
      }
   }
}
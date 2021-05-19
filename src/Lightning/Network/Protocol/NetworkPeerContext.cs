using System.Net;
using Bitcoin.Primitives.Fundamental;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Utils;
using NBitcoin;
using Network.Protocol.Transport;

namespace Network.Protocol
{
   public class NetworkPeerContext : PeerContext, INetworkPeerContext
   {
      public bool HandshakeComplete { get; set; }

      public bool InitComplete { get; set; }
      
      public IHandshakeProtocol? HandshakeProtocol { get; set; }

      public PublicKey NodeId  => (PublicKey) PeerId.ToByteArray();

      public PrivateKey? BitcoinAddressKey { get; set; } //TODO David gossip set up in a better place

      public PublicKey BitcoinAddress => (PublicKey) new PubKey(BitcoinAddressKey!).ToBytes();
      
      public NetworkPeerContext(ILogger logger,
         IEventBus eventBus,
         PeerConnectionDirection direction,
         string peerId,
         EndPoint localEndPoint,
         EndPoint publicEndPoint,
         EndPoint remoteEndPoint,
         INetworkMessageWriter messageWriter)
         : base(logger, eventBus, direction, peerId, localEndPoint, publicEndPoint, remoteEndPoint, messageWriter)
      { }

      public void SetHandshakeProtocol(IHandshakeProtocol handshakeProtocol)
      {
         HandshakeProtocol = handshakeProtocol;
      }

      public override void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor)
      {
         base.AttachNetworkMessageProcessor(messageProcessor);
      }

      public void OnHandshakeCompleted()
      {
         IsConnected = true;
         HandshakeComplete = true;
         eventBus.Publish(new PeerHandshaked(this));
      }

      public void OnInitMessageCompleted()
      {
         InitComplete = true;
      }
   }
}
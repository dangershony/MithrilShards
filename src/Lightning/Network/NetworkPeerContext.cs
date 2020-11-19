using System;
using System.Net;
using Bedrock.Framework.Protocols;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Transport;

namespace MithrilShards.Example.Network
{
   public class NetworkPeerContext : PeerContext
   {
      public bool Handshaked { get; set; }

      public INoiseProtocol HandshakePotocol { get; set; }

      public NetworkPeerContext(ILogger logger,
         IEventBus eventBus,
         PeerConnectionDirection direction,
         string peerId,
         EndPoint localEndPoint,
         EndPoint publicEndPoint,
         EndPoint remoteEndPoint,
         INetworkMessageWriter messageWriter,
         INoiseProtocol noise)
         : base(logger, eventBus, direction, peerId, localEndPoint, publicEndPoint, remoteEndPoint, messageWriter)
      {
         this.HandshakePotocol = noise;
      }

      public override void AttachNetworkMessageProcessor(INetworkMessageProcessor messageProcessor)
      {
         base.AttachNetworkMessageProcessor(messageProcessor);
      }

      public void OnHandshakeCompleted()
      {
         this.IsConnected = true;
         this.eventBus.Publish(new PeerHandshaked(this));
      }
   }
}
﻿using MithrilShards.Core.Network.Protocol;
using System.Net;

namespace MithrilShards.Core.Network.Events {
   /// <summary>
   /// A peer message has been received and parsed
   /// </summary>
   /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
   public class PeerMessageReceived : PeerEventBase {
      public INetworkMessage Message { get; }

      public int MessageSize { get; }

      public PeerMessageReceived(EndPoint localEndPoint, EndPoint remoteEndPoint, INetworkMessage message, int messageSize) : base(localEndPoint, remoteEndPoint) {
         this.Message = message;
         this.MessageSize = messageSize;
      }
   }
}
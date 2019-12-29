﻿using MithrilShards.Core.Network;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MithrilShards.Network.Legacy {
   public interface IPeerConnection {
      PeerConnectionDirection Direction { get; }
      PeerDisconnectionReason DisconnectReason { get; }
      TimeSpan? TimeOffset { get; }

      IPeerContext PeerContext { get; }

      Task IncomingConnectionAccepted(CancellationToken cancellation = default);
   }
}
﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.Network;

namespace MithrilShards.Chain.Bitcoin.Network.Server.Guards
{
   public class BannedPeerGuard : ServerPeerConnectionGuardBase
   {
      readonly IConnectivityPeerStats peerStats;

      public BannedPeerGuard(ILogger<InitialBlockDownloadStateGuard> logger, IOptions<ForgeConnectivitySettings> settings) : base(logger, settings)
      {
      }

      internal override string TryGetDenyReason(IPeerContext peerContext)
      {
         //TODO implement ban check, bitcoin core ref: https://github.com/bitcoin/bitcoin/blob/e8e79958a7b2a0bf1b02adcce9f4d811eac37dfc/src/net.cpp#L984-L993
         //if (peer is banned)
         //{
         //   return "Current peer is banned.";
         //}

         return null;
      }
   }
}
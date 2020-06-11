﻿using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Chain.Bitcoin.DataTypes;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.ChainDefinitions
{
   public class BitcoinMainDefinition : BitcoinChain
   {
      public override BitcoinNetworkDefinition ConfigureNetwork()
      {
         return new BitcoinNetworkDefinition
         {
            Name = "Bitcoin Main",
            Magic = 0xD9B4BEF9,
            MagicBytes = BitConverter.GetBytes(0xD9B4BEF9),
            DefaultMaxPayloadSize = 32_000_000
         };
      }

      public override ConsensusParameters ConfigureConsensus()
      {
         BlockHeader genesisBlock = this.BuildGenesisBlock("000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f");

         return new ConsensusParameters
         {
            Genesis = genesisBlock.Hash!,
            GenesisHeader = genesisBlock,

            PowLimit = new Target("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"),
            PowTargetTimespan = (uint)TimeSpan.FromDays(14).TotalSeconds, // 2 weeks
            PowTargetSpacing = (uint)TimeSpan.FromMinutes(10).TotalSeconds,
            PowAllowMinDifficultyBlocks = false,
            PowNoRetargeting = false,

            SubsidyHalvingInterval = 210000,
            SegwitHeight = 481824, // 0000000000000000001c8018d9cb3b742ef25114f27563e3fc4a1902167f9893,
            MinimumChainWork = new UInt256("0x000000000000000000000000000000000000000008ea3cf107ae0dec57f03fe8"),
         };
      }

      private BlockHeader BuildGenesisBlock(string genesisHash)
      {
         //TODO complete construction (a Block will be needed and not a BlockHeader)
         return new BlockHeader
         {
            Bits = 0,
            Hash = new UInt256(genesisHash),
         };
      }
   }
}

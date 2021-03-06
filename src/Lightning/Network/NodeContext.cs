﻿using System;
using System.Buffers;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;
using NBitcoin;

namespace Network
{
   public class NodeContext
   {
      private readonly IRandomNumberGenerator? _randomNumberGenerator;
      public byte[] PrivateKey { get; set; } // TODO: this can be private or even hidden behind an interface.
      public string LocalPubKey { get; set; }

      public NodeContext(IRandomNumberGenerator randomNumberGenerator)
      {
         _randomNumberGenerator = randomNumberGenerator;

         byte[] prv = new byte[32];

         _randomNumberGenerator.GetBytes(prv.AsSpan());

         var k = new NBitcoin.Key(prv); //TODO Dan

         // random data
         PrivateKey = k.ToBytes();
         LocalPubKey = k.PubKey.ToHex();
      }
   }
}
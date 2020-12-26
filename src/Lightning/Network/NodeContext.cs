using System;
using System.Buffers;
using Bitcoin.Primitives.Fundamental;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;
using NBitcoin;

namespace Network
{
   public class NodeContext
   {
      public PrivateKey PrivateKey { get; set; } // TODO: this can be private or even hidden behind an interface.
      public string LocalPubKey { get; set; }

      public NodeContext(IRandomNumberGenerator randomNumberGenerator)
      {
         byte[] prv = new byte[32];

         randomNumberGenerator.GetBytes(prv.AsSpan());

         var k = new Key(prv); //TODO Dan

         // random data
         PrivateKey = (PrivateKey) k.ToBytes();
         LocalPubKey = k.PubKey.ToHex();
      }
   }
}
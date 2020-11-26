using System;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;

namespace Network
{
   public class NodeContext
   {
      private readonly IRandomNumberGenerator _randomNumberGenerator;
      public byte[] PrivateLey { get; set; } // TODO: this can be private or even hidden behind an interface.
      public string LocalPubKey { get; set; }

      public NodeContext(IRandomNumberGenerator randomNumberGenerator)
      {
         _randomNumberGenerator = randomNumberGenerator;

         byte[] prv = new byte[32];

         _randomNumberGenerator.GetBytes(prv.AsSpan());

         // random data
         PrivateLey = prv;
         LocalPubKey = PrivateLey.AsSpan().Slice(32).ToString();
      }
   }
}
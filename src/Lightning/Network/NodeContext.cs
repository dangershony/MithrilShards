using System;
using MithrilShards.Core;
using MithrilShards.Core.DataTypes;

namespace Network
{
   public class NodeContext
   {
      private readonly IRandomNumberGenerator randomNumberGenerator;
      public byte[] PrivateLey { get; set; } // TODO: this can be private or even hidden behind an interface.
      public string LocalPubKey { get; set; }

      public NodeContext(IRandomNumberGenerator randomNumberGenerator)
      {
         this.randomNumberGenerator = randomNumberGenerator;

         byte[] prv = new byte[32];

         this.randomNumberGenerator.GetBytes(prv.AsSpan());

         // random data
         this.PrivateLey = prv;
         this.LocalPubKey = this.PrivateLey.AsSpan().Slice(32).ToString();
      }
   }
}
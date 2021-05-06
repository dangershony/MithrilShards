using System;
using System.Collections.Generic;
using System.Text;
using Bitcoin.Primitives.Fundamental;

namespace Protocol.Channels
{
   public struct Keyset
   {
      public PublicKey self_revocation_key { get; set; }
      public PublicKey self_htlc_key { get; set; }
      public PublicKey other_htlc_key { get; set; }
      public PublicKey self_delayed_payment_key { get; set; }
      public PublicKey self_payment_key { get; set; }
      public PublicKey other_payment_key { get; set; }
   };
}
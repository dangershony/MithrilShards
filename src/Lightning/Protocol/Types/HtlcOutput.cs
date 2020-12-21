using MithrilShards.Core.DataTypes;

namespace Protocol.Channels
{
   public class HtlcOutput
   {
      public ulong HtlcId { get; set; }

      /// <summary>
      /// The value, in msat, of the HTLC. The value as it appears in the commitment transaction is
      /// this divided by 1000.
      /// </summary>
      public ulong AmountMsat { get; set; }

      /// <summary>
      /// The CLTV lock-time at which this HTLC expires.
      /// </summary>
      public uint CltvExpiry { get; set; }

      /// <summary>
      /// The hash of the preimage which unlocks this HTLC.
      /// </summary>
      public UInt256 PaymentHash { get; set; }
   }
}
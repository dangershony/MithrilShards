using MithrilShards.Core.DataTypes;

namespace Protocol.Channels
{
   public class HtlcOutput
   {
      public ulong HtlcId { get; set; }
      public ulong AmountMsat { get; set; }
      public uint CltvExpiry { get; set; }
      public UInt256 PaymentHash { get; set; }
   }
}
using MithrilShards.Core.DataTypes;

namespace Protocol.Channels
{
   /// Information about an HTLC as it appears in a commitment transaction
   public class HtlcOutputInCommitment
   {
      /// Whether the HTLC was "offered" (ie outbound in relation to this commitment transaction).
      /// Note that this is not the same as whether it is ountbound *from us*. To determine that you
      /// need to compare this value to whether the commitment transaction in question is that of
      /// the counterparty or our own.
      public bool Offered { get; set; }

      public Htlc HtlcOutput { get; set; }

      /// The position within the commitment transactions' outputs. This may be None if the value is
      /// below the dust limit (in which case no output appears in the commitment transaction and the
      /// value is spent to additional transaction fees).
      public uint TransactionOutputIndex { get; set; }
   }
}
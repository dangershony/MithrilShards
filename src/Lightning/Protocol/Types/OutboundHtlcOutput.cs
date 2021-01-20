namespace Protocol.Channels
{
   public struct OutboundHtlcOutput
   {
      public Htlc Htlc { get; set; }

      public OutboundHtlcState State { get; set; }

      //source: HTLCSource,
   }
}
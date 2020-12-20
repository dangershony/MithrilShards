namespace Protocol.Channels
{
   internal struct OutboundHtlcOutput
   {
      public HtlcOutput Htlc { get; set; }

      public OutboundHtlcState State { get; set; }

      //source: HTLCSource,
   }
}
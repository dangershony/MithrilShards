namespace Protocol.Channels
{
   public class InboundHtlcOutput
   {
      public HtlcOutput Htlc { get; set; }

      public InboundHtlcState State { get; set; }
   }
}
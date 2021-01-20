namespace Protocol.Channels
{
   public class InboundHtlcOutput
   {
      public Htlc Htlc { get; set; }

      public InboundHtlcState State { get; set; }
   }
}
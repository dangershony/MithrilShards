using Bitcoin.Primitives.Fundamental;

namespace Protocol.Channels.Types
{
   public struct Keyset
   {
      public PublicKey SelfRevocationKey { get; set; }
      public PublicKey SelfHtlcKey { get; set; }
      public PublicKey OtherHtlcKey { get; set; }
      public PublicKey SelfDelayedPaymentKey { get; set; }
      public PublicKey SelfPaymentKey { get; set; }
      public PublicKey OtherPaymentKey { get; set; }
   };
}
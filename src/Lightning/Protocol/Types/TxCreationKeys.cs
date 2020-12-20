using System;
using System.Collections.Generic;
using System.Text;
using Network.Protocol.Messages.Types;

namespace Protocol.Channels
{
   /// The set of public keys which are used in the creation of one commitment transaction.
   /// These are derived from the channel base keys and per-commitment data.
   ///
   /// A broadcaster key is provided from potential broadcaster of the computed transaction.
   /// A countersignatory key is coming from a protocol participant unable to broadcast the
   /// transaction.
   ///
   /// These keys are assumed to be good, either because the code derived them from
   /// channel basepoints via the new function, or they were obtained via
   /// PreCalculatedTxCreationKeys.trust_key_derivation because we trusted the source of the
   /// pre-calculated keys.
   public class TxCreationKeys
   {
      /// The broadcaster's per-commitment public key which was used to derive the other keys.
      public PublicKey PerCommitmentPoint { get; set; }

      /// The revocation key which is used to allow the broadcaster of the commitment
      /// transaction to provide their counterparty the ability to punish them if they broadcast
      /// an old state.
      public PublicKey RevocationKey { get; set; }

      /// Broadcaster's HTLC Key
      public PublicKey BroadcasterHtlcKey { get; set; }

      /// Countersignatory's HTLC Key
      public PublicKey CountersignatoryHtlcKey { get; set; }

      /// Broadcaster's Payment Key (which isn't allowed to be spent from for some delay)
      public PublicKey BroadcasterDelayedPaymentKey { get; set; }
   }
}
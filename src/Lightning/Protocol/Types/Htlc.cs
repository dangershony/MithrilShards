using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Types;
using Protocol.Channels;

namespace Protocol.Types
{
   public class Htlc
   {
      /* What's the status. */

      public HtlcState State;

      /* The unique ID for this peer and this direction (LOCAL or REMOTE) */
      public ulong Id;
      /* The amount in millisatoshi. */

      public ulong Amount;

      /* When the HTLC can no longer be redeemed. */

      public uint Expirylocktime;

      /* The hash of the preimage which can redeem this HTLC */

      public UInt256 Rhash;

      /* The preimage which hashes to rhash (if known) */

      public Preimage R;

      /* If they fail the HTLC, we store why here. */

      // failed_htlc *failed;

      /* Routing information sent with this HTLC (outgoing only). */
      public ushort Routing;

      /* Blinding (optional). */

      public PublicKey Blinding;

      public LightningScripts.Side Side
      {
         get
         {
            return State > HtlcState.RcvdAddHtlc ? LightningScripts.Side.Local : LightningScripts.Side.Remote;
         }
      }
   };

   public enum HtlcState
   {
      /* When we add a new htlc, it goes in this order. */
      SentAddHtlc,
      SentAddCommit,
      RcvdAddRevocation,
      RcvdAddAckCommit,
      SentAddAckRevocation,

      /* When they remove an HTLC, it goes from SENT_ADD_ACK_REVOCATION: */
      RcvdRemoveHtlc,
      RcvdRemoveCommit,
      SentRemoveRevocation,
      SentRemoveAckCommit,
      RcvdRemoveAckRevocation,

      /* When they add a new htlc, it goes in this order. */
      RcvdAddHtlc,
      RcvdAddCommit,
      SentAddRevocation,
      SentAddAckCommit,
      RcvdAddAckRevocation,

      /* When we remove an HTLC, it goes from RCVD_ADD_ACK_REVOCATION: */
      SentRemoveHtlc,
      SentRemoveCommit,
      RcvdRemoveRevocation,
      RcvdRemoveAckCommit,
      SentRemoveAckRevocation,

      HtlcStateInvalid
   };
}
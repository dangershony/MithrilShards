namespace Protocol.Channels
{
   public enum OutboundHtlcState
   {
      /// Added by us and included in a commitment_signed (if we were AwaitingRemoteRevoke when we
      /// created it we would have put it in the holding cell instead). When they next revoke_and_ack
      /// we will promote to Committed (note that they may not accept it until the next time we
      /// revoke, but we don't really care about that:
      ///  * they've revoked, so worst case we can announce an old state and get our (option on)
      ///    money back (though we won't), and,
      ///  * we'll send them a revoke when they send a commitment_signed, and since only they're
      ///    allowed to remove it, the "can only be removed once committed on both sides" requirement
      ///    doesn't matter to us and it's up to them to enforce it, worst-case they jump ahead but
      ///    we'll never get out of sync).
      /// Note that we Box the OnionPacket as it's rather large and we don't want to blow up
      /// OutboundHTLCOutput's size just for a temporary bit
      LocalAnnounced,

      Committed,

      /// Remote removed this (outbound) HTLC. We're waiting on their commitment_signed to finalize
      /// the change (though they'll need to revoke before we fail the payment).
      RemoteRemoved,

      /// Remote removed this and sent a commitment_signed (implying we've revoke_and_ack'ed it), but
      /// the remote side hasn't yet revoked their previous state, which we need them to do before we
      /// can do any backwards failing. Implies AwaitingRemoteRevoke.
      /// We also have not yet removed this HTLC in a commitment_signed message, and are waiting on a
      /// remote revoke_and_ack on a previous state before we can do so.
      AwaitingRemoteRevokeToRemove,

      /// Remote removed this and sent a commitment_signed (implying we've revoke_and_ack'ed it), but
      /// the remote side hasn't yet revoked their previous state, which we need them to do before we
      /// can do any backwards failing. Implies AwaitingRemoteRevoke.
      /// We have removed this HTLC in our latest commitment_signed and are now just waiting on a
      /// revoke_and_ack to drop completely.
      AwaitingRemovedRemoteRevoke,
   }
}
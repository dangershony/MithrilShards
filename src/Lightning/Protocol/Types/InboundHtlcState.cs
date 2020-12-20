namespace Protocol.Channels
{
   public enum InboundHtlcState
   {
      /// Offered by remote, to be included in next local commitment tx. I.e., the remote sent an
      /// update_add_htlc message for this HTLC.
      RemoteAnnounced,

      /// Included in a received commitment_signed message (implying we've
      /// revoke_and_ack'd it), but the remote hasn't yet revoked their previous
      /// state (see the example below). We have not yet included this HTLC in a
      /// commitment_signed message because we are waiting on the remote's
      /// aforementioned state revocation. One reason this missing remote RAA
      /// (revoke_and_ack) blocks us from constructing a commitment_signed message
      /// is because every time we create a new "state", i.e. every time we sign a
      /// new commitment tx (see [BOLT #2]), we need a new per_commitment_point,
      /// which are provided one-at-a-time in each RAA. E.g., the last RAA they
      /// sent provided the per_commitment_point for our current commitment tx.
      /// The other reason we should not send a commitment_signed without their RAA
      /// is because their RAA serves to ACK our previous commitment_signed.
      ///
      /// Here's an example of how an HTLC could come to be in this state:
      /// remote --> update_add_htlc(prev_htlc)   --> local
      /// remote --> commitment_signed(prev_htlc) --> local
      /// remote <-- revoke_and_ack               <-- local
      /// remote <-- commitment_signed(prev_htlc) <-- local
      /// [note that here, the remote does not respond with a RAA]
      /// remote --> update_add_htlc(this_htlc)   --> local
      /// remote --> commitment_signed(prev_htlc, this_htlc) --> local
      /// Now `this_htlc` will be assigned this state. It's unable to be officially
      /// accepted, i.e. included in a commitment_signed, because we're missing the
      /// RAA that provides our next per_commitment_point. The per_commitment_point
      /// is used to derive commitment keys, which are used to construct the
      /// signatures in a commitment_signed message.
      /// Implies AwaitingRemoteRevoke.
      /// [BOLT #2]: https://github.com/lightningnetwork/lightning-rfc/blob/master/02-peer-protocol.md
      AwaitingRemoteRevokeToAnnounce,

      /// Included in a received commitment_signed message (implying we've revoke_and_ack'd it).
      /// We have also included this HTLC in our latest commitment_signed and are now just waiting
      /// on the remote's revoke_and_ack to make this HTLC an irrevocable part of the state of the
      /// channel (before it can then get forwarded and/or removed).
      /// Implies AwaitingRemoteRevoke.
      AwaitingAnnouncedRemoteRevoke,

      Committed,

      /// Removed by us and a new commitment_signed was sent (if we were AwaitingRemoteRevoke when we
      /// created it we would have put it in the holding cell instead). When they next revoke_and_ack
      /// we'll drop it.
      /// Note that we have to keep an eye on the HTLC until we've received a broadcastable
      /// commitment transaction without it as otherwise we'll have to force-close the channel to
      /// claim it before the timeout (obviously doesn't apply to revoked HTLCs that we can't claim
      /// anyway). That said, ChannelMonitor does this for us (see
      /// ChannelMonitor::would_broadcast_at_height) so we actually remove the HTLC from our own
      /// local state before then, once we're sure that the next commitment_signed and
      /// ChannelMonitor::provide_latest_local_commitment_tx_info will not include this HTLC.
      LocalRemoved,
   }
}
using MithrilShards.Core.DataTypes;
using Network.Protocol.Messages.Types;

namespace Protocol.Channels
{
   public class Htlc
   {
      /* What's the status. */

      public htlc_state state;

      /* The unique ID for this peer and this direction (LOCAL or REMOTE) */
      public ulong id;
      /* The amount in millisatoshi. */

      public ulong amount;

      /* When the HTLC can no longer be redeemed. */

      public ulong expiry;

      /* The hash of the preimage which can redeem this HTLC */

      public UInt256 rhash;

      /* The preimage which hashes to rhash (if known) */

      public PublicKey r;

      /* If they fail the HTLC, we store why here. */

      // failed_htlc *failed;

      /* Routing information sent with this HTLC (outgoing only). */
      public ushort routing;

      /* Blinding (optional). */

      public PublicKey blinding;

      public LightningScripts.Side Side;
   };

   public enum htlc_state
   {
      /* When we add a new htlc, it goes in this order. */
      SENT_ADD_HTLC,
      SENT_ADD_COMMIT,
      RCVD_ADD_REVOCATION,
      RCVD_ADD_ACK_COMMIT,
      SENT_ADD_ACK_REVOCATION,

      /* When they remove an HTLC, it goes from SENT_ADD_ACK_REVOCATION: */
      RCVD_REMOVE_HTLC,
      RCVD_REMOVE_COMMIT,
      SENT_REMOVE_REVOCATION,
      SENT_REMOVE_ACK_COMMIT,
      RCVD_REMOVE_ACK_REVOCATION,

      /* When they add a new htlc, it goes in this order. */
      RCVD_ADD_HTLC,
      RCVD_ADD_COMMIT,
      SENT_ADD_REVOCATION,
      SENT_ADD_ACK_COMMIT,
      RCVD_ADD_ACK_REVOCATION,

      /* When we remove an HTLC, it goes from RCVD_ADD_ACK_REVOCATION: */
      SENT_REMOVE_HTLC,
      SENT_REMOVE_COMMIT,
      RCVD_REMOVE_REVOCATION,
      RCVD_REMOVE_ACK_COMMIT,
      SENT_REMOVE_ACK_REVOCATION,

      HTLC_STATE_INVALID
   };

   public class Chainparams
   {
      public string network_name;
      public string bip173_name;
      /*'bip70_name' is corresponding to the 'chain' field of
       * the API 'getblockchaininfo' */
      public string bip70_name;

      //public  bitcoin_blkid genesis_blockhash;
      public int rpc_port;

      //  public string cli;
      //public char* cli_args;
      /* The min numeric version of cli supported */

      //public u64 cli_min_supported_version;
      public ulong dust_limit;

      public ulong max_funding;
      public ulong max_payment;
      public uint when_lightning_became_cool;
      public int p2pkh_version;
      public int p2sh_version;

      /* Whether this is a test network or not */
      public bool testnet;

      /* Version codes for BIP32 extended keys in libwally-core*/
      // public  bip32_key_version bip32_key_version;
      // public bool is_elements;
      //public u8* fee_asset_tag;
   };
}
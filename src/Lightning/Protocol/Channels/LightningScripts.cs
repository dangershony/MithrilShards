using System;
using NBitcoin;
using Network.Protocol.Messages.Types;
using Transaction = MithrilShards.Chain.Bitcoin.Protocol.Types.Transaction;

namespace Protocol.Channels
{
   public class LightningScripts
   {
      public byte[] CreaateFundingTransactionScript(PublicKey pubkey1, PublicKey pubkey2)
      {
         // todo: sort pubkeys lexicographically

         var script = new Script(
            OpcodeType.OP_2,
            Op.GetPushOp(pubkey1),
            Op.GetPushOp(pubkey2),
            OpcodeType.OP_2,
            OpcodeType.OP_CHECKMULTISIG
         );

         return script.ToBytes();
      }

      /// A script either spendable by the revocation
      /// key or the broadcaster_delayed_payment_key and satisfying the relative-locktime OP_CSV constrain.
      /// Encumbering a `to_holder` output on a commitment transaction or 2nd-stage HTLC transactions.
      public byte[] GetRevokeableRedeemscript(PublicKey revocationKey, ushort contestDelay, PublicKey broadcasterDelayedPaymentKey)
      {
         var script = new Script(
            OpcodeType.OP_IF,
            Op.GetPushOp(revocationKey),
            OpcodeType.OP_ELSE,
            Op.GetPushOp(contestDelay),
            OpcodeType.OP_CHECKSEQUENCEVERIFY,
            OpcodeType.OP_DROP,
            Op.GetPushOp(broadcasterDelayedPaymentKey),
            OpcodeType.OP_ENDIF,
            OpcodeType.OP_CHECKSIG);

         return script.ToBytes();
      }

      public byte[] GetHtlcRedeemscript(
         HtlcOutputInCommitment htlc,
         PublicKey broadcasterHtlcKey,
         PublicKey countersignatoryHtlcKey,
         PublicKey revocationKey)
      {
         var paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(htlc.PaymentHash.GetBytes().ToArray());
         var revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationKey);
         var revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

         Script script;

         if (htlc.Offered)
         {
            script = new Script(
               OpcodeType.OP_DUP,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(revocationKey160),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_IF,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ELSE,
               Op.GetPushOp(countersignatoryHtlcKey),
               OpcodeType.OP_SWAP,
               OpcodeType.OP_SIZE,
               Op.GetPushOp(32),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_NOTIF,
               OpcodeType.OP_DROP,
               Op.GetPushOp(2),
               OpcodeType.OP_SWAP,
               Op.GetPushOp(broadcasterHtlcKey),
               Op.GetPushOp(2),
               OpcodeType.OP_CHECKMULTISIG,
               OpcodeType.OP_ELSE,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(paymentHash160),
               OpcodeType.OP_EQUALVERIFY,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_ENDIF);
         }
         else
         {
            script = new Script(
               OpcodeType.OP_DUP,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(revocationKey160),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_IF,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ELSE,
               Op.GetPushOp(countersignatoryHtlcKey),
               OpcodeType.OP_SWAP,
               OpcodeType.OP_SIZE,
               Op.GetPushOp(32),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_IF,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(paymentHash160),
               OpcodeType.OP_EQUALVERIFY,
               Op.GetPushOp(2),
               OpcodeType.OP_SWAP,
               Op.GetPushOp(broadcasterHtlcKey),
               Op.GetPushOp(2),
               OpcodeType.OP_CHECKMULTISIG,
               OpcodeType.OP_ELSE,
               OpcodeType.OP_DROP,
               Op.GetPushOp(htlc.CltvExpiry),
               OpcodeType.OP_CHECKLOCKTIMEVERIFY,
               OpcodeType.OP_DROP,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_ENDIF);
         }

         return script.ToBytes();
      }

      public Transaction BuildCommitmentTransaction()
      {
         return null;
      }
   }
}
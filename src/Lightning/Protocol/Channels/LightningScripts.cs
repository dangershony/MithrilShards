using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using NBitcoin;
using NBitcoin.Crypto;
using Network.Protocol.Messages.Types;
using OutPoint = MithrilShards.Chain.Bitcoin.Protocol.Types.OutPoint;
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
      public byte[] GetRevokeableRedeemscript(PublicKey revocationKey, ushort contestDelay,
         PublicKey broadcasterDelayedPaymentKey)
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

      public byte[] AnchorToRemoteRedeem(PublicKey remoteKey)
      {
         var script = new Script(
            Op.GetPushOp(remoteKey),
            OpcodeType.OP_CHECKSIGVERIFY,
            Op.GetPushOp(1),
            OpcodeType.OP_CHECKSEQUENCEVERIFY);

         return script.ToBytes();
      }

      public byte[] bitcoin_wscript_anchor(PublicKey fundingPubkey)
      {
         // BOLT3:
         // `to_local_anchor` and `to_remote_anchor` Output (option_anchor_outputs):
         //    <local_funding_pubkey/remote_funding_pubkey> OP_CHECKSIG OP_IFDUP
         //    OP_NOTIF
         //        OP_16 OP_CHECKSEQUENCEVERIFY
         //    OP_ENDIF

         var script = new Script(
            Op.GetPushOp(fundingPubkey),
            OpcodeType.OP_CHECKSIG,
            OpcodeType.OP_IFDUP,
            OpcodeType.OP_NOTIF,
            Op.GetPushOp(16),
            OpcodeType.OP_CHECKSEQUENCEVERIFY,
            OpcodeType.OP_ENDIF);

         return script.ToBytes();
      }

      public byte[] GetHtlcRedeemscript(
         HtlcOutputInCommitment htlc,
         PublicKey broadcasterHtlcKey,
         PublicKey countersignatoryHtlcKey,
         PublicKey revocationKey,
         bool optionAnchorOutputs)
      {
         // todo: dan - move this to a hashing interface
         var paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(htlc.HtlcOutput.rhash.GetBytes().ToArray());
         var revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationKey);
         var revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

         Script script;

         if (htlc.Offered)
         {
            if (optionAnchorOutputs)
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
                  Op.GetPushOp(1),
                  OpcodeType.OP_CHECKSEQUENCEVERIFY,
                  OpcodeType.OP_DROP,
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
         }
         else
         {
            if (optionAnchorOutputs)
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
                  Op.GetPushOp((long)htlc.HtlcOutput.expiry),
                  OpcodeType.OP_CHECKLOCKTIMEVERIFY,
                  OpcodeType.OP_DROP,
                  OpcodeType.OP_CHECKSIG,
                  OpcodeType.OP_ENDIF,
                  Op.GetPushOp(1),
                  OpcodeType.OP_CHECKSEQUENCEVERIFY,
                  OpcodeType.OP_DROP,
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
               Op.GetPushOp((long)htlc.HtlcOutput.expiry),
               OpcodeType.OP_CHECKLOCKTIMEVERIFY,
               OpcodeType.OP_DROP,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_ENDIF);
            }
         }

         return script.ToBytes();
      }

      private ulong get_commitment_transaction_number_obscure_factor(
         PublicKey openerPaymentBasepoint,
         PublicKey accepterPaymentBasepoint)
      {
         var bytes = new List<byte>();
         bytes.AddRange((byte[])openerPaymentBasepoint);
         bytes.AddRange((byte[])accepterPaymentBasepoint);

         byte[] res = Hashes.SHA256(bytes.ToArray());

         var ret = MemoryMarshal.Cast<byte, ulong>(res.AsSpan().Slice(0, 6));

         return ret[0];
      }

      public enum Side
      {
         LOCAL,
         REMOTE,
      };

      public ulong amount_tx_fee(uint fee_per_kw, ulong weight)
      {
         ulong fee = fee_per_kw * weight / 1000;

         return fee;
      }

      private ulong HtlcTimeoutFee(uint feerate_per_kw, bool option_anchor_outputs)
      {
         /* BOLT #3:
          *
          * The fee for an HTLC-timeout transaction:
          * - MUST BE calculated to match:
          *   1. Multiply `feerate_per_kw` by 663 (666 if `option_anchor_outputs`
          *      applies) and divide by 1000 (rounding down).
          */

         uint baseAmount = option_anchor_outputs ? (uint)666 : (uint)663;

         return baseAmount * feerate_per_kw / 1000;
      }

      private ulong HtlcSuccessFee(uint feerate_per_kw, bool option_anchor_outputs)
      {
         /* BOLT #3:
          *
          * The fee for an HTLC-success transaction:
          * - MUST BE calculated to match:
          *   1. Multiply `feerate_per_kw` by 703 (706 if `option_anchor_outputs`
          *      applies) and divide by 1000 (rounding down).
          */

         uint baseAmount = option_anchor_outputs ? (uint)706 : (uint)703;

         return baseAmount * feerate_per_kw / 1000;
      }

      public Transaction CreateCommitmenTransaction(
         UInt256 funding_txid,
         OutPoint funding_txout,
         ulong funding,
         PublicKey local_funding_key,
         PublicKey remote_funding_key,
         Side opener,
         ushort to_self_delay,
         Keyset Keyset,
         uint feerate_per_kw,
         ulong dust_limit_satoshis,
         ulong self_pay,
         ulong other_pay,
         List<Htlc> htlcs,
         List<Htlc> htlcmap,
         //wally_tx_output *direct_outputs[NUM_SIDES],
         ulong obscured_commitment_number,
         bool option_anchor_outputs,
         Side side)
      {
         // TODO: ADD TRACE LOGS

         // BOLT3 Commitment Transaction Construction
         // 1. Initialize the commitment transaction input and locktime

         var transaction = new Transaction
         {
            Version = 2,
            LockTime = (((uint)0x20) << 8 * 3) | ((uint)(obscured_commitment_number & 0xffffff)),
            Inputs = new[]
            {
               new TransactionInput
               {
                  PreviousOutput = funding_txout,
                  Sequence = (((uint)0x80) << 8 * 3) | ((uint)(obscured_commitment_number >> 3 * 8)),
                  ScriptWitness = new TransactionWitness() // todo: dan - bring signatures
               }
            }
         };

         // BOLT3 Commitment Transaction Construction
         // 1. Calculate which committed HTLCs need to be trimmed
         var htlcsUntrimmed = new List<Htlc>();
         foreach (Htlc htlc in htlcs)
         {
            /* BOLT #3:
             *
             *   - for every offered HTLC:
             *    - if the HTLC amount minus the HTLC-timeout fee would be less than
             *    `dust_limit_satoshis` set by the transaction owner:
             *      - MUST NOT contain that output.
             *    - otherwise:
             *      - MUST be generated as specified in
             *      [Offered HTLC Outputs](#offered-htlc-outputs).
             */

            ulong htlc_fee;
            if (htlc.Side == side)
               htlc_fee = HtlcTimeoutFee(feerate_per_kw, option_anchor_outputs);
            /* BOLT #3:
             *
             *  - for every received HTLC:
             *    - if the HTLC amount minus the HTLC-success fee would be less than
             *    `dust_limit_satoshis` set by the transaction owner:
             *      - MUST NOT contain that output.
             *    - otherwise:
             *      - MUST be generated as specified in
             */
            else
               htlc_fee = HtlcSuccessFee(feerate_per_kw, option_anchor_outputs);

            if (htlc.amount >= htlc_fee + dust_limit_satoshis)
            {
               htlcsUntrimmed.Add(htlc);
            }
         }

         // BOLT3 Commitment Transaction Construction
         // 1. Calculate the base commitment transaction fee.

         ulong weight;
         ulong base_fee;
         ulong num_untrimmed_htlcs = (ulong)htlcsUntrimmed.Count;
         /* BOLT #3:
          *
          * The base fee for a commitment transaction:
          *  - MUST be calculated to match:
          *    1. Start with `weight` = 724 (1124 if `option_anchor_outputs` applies).
          */
         if (option_anchor_outputs)
            weight = 1124;
         else
            weight = 724;

         /* BOLT #3:
          *
          *    2. For each committed HTLC, if that output is not trimmed as
          *       specified in [Trimmed Outputs](#trimmed-outputs), add 172
          *       to `weight`.
          */
         weight += 172 * num_untrimmed_htlcs;

         base_fee = weight;

         // todo log base fee

         // BOLT3 Commitment Transaction Construction
         // 4. Subtract this base fee from the funder (either to_local or to_remote).
         // If option_anchor_outputs applies to the commitment transaction,
         // also subtract two times the fixed anchor size of 330 sats from the funder (either to_local or to_remote).

         if (opener == side)
         {
            self_pay -= base_fee;

            if (option_anchor_outputs)
            {
               self_pay -= 660;
            }
         }
         else
         {
            other_pay -= base_fee;

            if (option_anchor_outputs)
            {
               other_pay -= 660;
            }
         }

         //#ifdef PRINT_ACTUAL_FEE
         //	{
         //		 amount_sat out = private AMOUNT_SAT(0);

         //      private bool ok = true;
         //		for (i = 0; i<tal_count(htlcs); i++) {
         //			if (!private trim(htlcs[i], feerate_per_kw, dust_limit,
         //              option_anchor_outputs, side))

         //				ok &= private amount_sat_add(&out, out, amount_msat_to_sat_round_down(htlcs[i]->amount));
         //		}

         //		if (amount_msat_greater_sat(self_pay, dust_limit))
         //			ok &= amount_sat_add(&out, out, amount_msat_to_sat_round_down(self_pay));
         //		if (amount_msat_greater_sat(other_pay, dust_limit))
         //			ok &= amount_sat_add(&out, out, amount_msat_to_sat_round_down(other_pay));
         //   assert(ok);
         //   SUPERVERBOSE("# actual commitment transaction fee = %"PRIu64"\n",
         //           funding.satoshis - out.satoshis);  /* Raw: test output */
         //}

         //#endif

         ///* We keep cltvs for tie-breaking HTLC outputs; we use the same order
         // * for sending the htlc txs, so it may matter. */
         //cltvs = tal_arr(tmpctx, u32, tx->wtx->outputs_allocation_len);

         var outputs = new List<HtlcOutputsInfo>();

         // BOLT3 Commitment Transaction Construction
         // 5. For every offered HTLC, if it is not trimmed, add an offered HTLC output.

         foreach (Htlc htlc in htlcsUntrimmed)
         {
            if (htlc.Side == side)
            {
               // todo round down msat to sat in s common method
               ulong amount = htlc.amount / 1000;

               var wscript = GetHtlcRedeemscript(new HtlcOutputInCommitment { HtlcOutput = htlc, Offered = true },
                  Keyset.self_htlc_key, Keyset.other_htlc_key, Keyset.self_revocation_key, option_anchor_outputs);

               var p2wsh = ScriptCoin.GetRedeemHash(new Script(wscript)); // todo: dan - move this to interface

               outputs.Add(new HtlcOutputsInfo
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2wsh.ToBytes()
                  },
                  CltvExpirey = 0
               });
            }
         }

         // BOLT3 Commitment Transaction Construction
         // 6. For every offered HTLC, if it is not trimmed, add an offered HTLC output.

         foreach (Htlc htlc in htlcsUntrimmed)
         {
            if (htlc.Side != side)
            {
               // todo round down msat to sat in s common method
               ulong amount = htlc.amount / 1000;

               var wscript = GetHtlcRedeemscript(new HtlcOutputInCommitment { HtlcOutput = htlc, Offered = false }, Keyset.self_htlc_key, Keyset.other_htlc_key, Keyset.self_revocation_key, option_anchor_outputs);

               var p2wsh = ScriptCoin.GetRedeemHash(new Script(wscript)); // todo: dan - move this to interface

               outputs.Add(new HtlcOutputsInfo
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2wsh.ToBytes()
                  },
                  CltvExpirey = 0
               });
            }
         }

         // BOLT3 Commitment Transaction Construction
         // 7. If the to_local amount is greater or equal to dust_limit_satoshis, add a to_local output.

         bool to_local = false;
         if (self_pay >= dust_limit_satoshis)
         {
            // todo round down msat to sat in s common method
            ulong amount = self_pay / 1000;

            var wscript = GetRevokeableRedeemscript(Keyset.self_revocation_key, to_self_delay, Keyset.self_delayed_payment_key);

            var p2wsh = ScriptCoin.GetRedeemHash(new Script(wscript)); // todo: dan - move this to interface

            outputs.Add(new HtlcOutputsInfo
            {
               TransactionOutput = new TransactionOutput
               {
                  Value = (long)amount,
                  PublicKeyScript = p2wsh.ToBytes()
               },
               CltvExpirey = 0
            });

            to_local = true;
         }

         // BOLT3 Commitment Transaction Construction
         // 8. If the to_remote amount is greater or equal to dust_limit_satoshis, add a to_remote output.

         bool to_remote = false;
         if (other_pay >= dust_limit_satoshis)
         {
            // todo round down msat to sat in s common method
            ulong amount = other_pay / 1000;

            // BOLT3:
            // If option_anchor_outputs applies to the commitment transaction,
            // the to_remote output is encumbered by a one block csv lock.
            // <remote_pubkey> OP_CHECKSIGVERIFY 1 OP_CHECKSEQUENCEVERIFY
            // Otherwise, this output is a simple P2WPKH to `remotepubkey`.

            Script p2wsh;
            if (option_anchor_outputs)
            {
               var wscript = AnchorToRemoteRedeem(Keyset.other_payment_key);

               p2wsh = ScriptCoin.GetRedeemHash(new Script(wscript)).ScriptPubKey; // todo: dan - move this to interface
            }
            else
            {
               p2wsh = PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey(new PubKey(Keyset.other_payment_key)); // todo: dan - move this to interface
            }

            outputs.Add(new HtlcOutputsInfo
            {
               TransactionOutput = new TransactionOutput
               {
                  Value = (long)amount,
                  PublicKeyScript = p2wsh.ToBytes()
               },
               CltvExpirey = 0
            });

            to_remote = true;
         }

         // BOLT3 Commitment Transaction Construction
         // 9. If option_anchor_outputs applies to the commitment transaction:
         //   if to_local exists or there are untrimmed HTLCs, add a to_local_anchor output
         //   if to_remote exists or there are untrimmed HTLCs, add a to_remote_anchor output

         if (option_anchor_outputs)
         {
            if (to_local || htlcsUntrimmed.Count != 0)
            {
               // todo round down msat to sat in s common method
               ulong amount = 330;

               var wscript = bitcoin_wscript_anchor(local_funding_key);

               var p2wsh = ScriptCoin.GetRedeemHash(new Script(wscript)); // todo: dan - move this to interface

               outputs.Add(new HtlcOutputsInfo
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2wsh.ToBytes()
                  },
                  CltvExpirey = 0
               });
            }

            if (to_remote || htlcsUntrimmed.Count != 0)
            {
               // todo round down msat to sat in s common method
               ulong amount = 330;

               var wscript = bitcoin_wscript_anchor(remote_funding_key);

               var p2wsh = ScriptCoin.GetRedeemHash(new Script(wscript)); // todo: dan - move this to interface

               outputs.Add(new HtlcOutputsInfo
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2wsh.ToBytes()
                  },
                  CltvExpirey = 0
               });
            }
         }

         // BOLT3 Commitment Transaction Construction
         // 10. Sort the outputs into BIP 69+CLTV order.

         var sorter = new HtlcLexicographicOrdering();

         outputs.Sort(sorter);

         return null;
      }
   }

   public class HtlcOutputsInfo
   {
      public TransactionOutput TransactionOutput { get; set; }
      public ulong CltvExpirey { get; set; }
   }

   public class HtlcLexicographicOrdering : IComparer<HtlcOutputsInfo>
   {
      public int Compare(HtlcOutputsInfo x, HtlcOutputsInfo y)
      {
      }
   }
}
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Types;
using NBitcoin;
using NBitcoin.Crypto;
using System.Linq;
using Bitcoin.Primitives.Serialization;
using Bitcoin.Primitives.Serialization.Serializers;
using NBitcoin.Policy;
using Newtonsoft.Json.Linq;
using OutPoint = Bitcoin.Primitives.Types.OutPoint;
using Transaction = Bitcoin.Primitives.Types.Transaction;
using Protocol.Hashing;

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

      /* BOLT #3:
       *
       * #### Offered HTLC Outputs
       *
       * This output sends funds to either an HTLC-timeout transaction after the
       * HTLC-timeout or to the remote node using the payment preimage or the
       * revocation key. The output is a P2WSH, with a witness script (no
       * option_anchor_outputs):
       *
       *     # To remote node with revocation key
       *     OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
       *     OP_IF
       *         OP_CHECKSIG
       *     OP_ELSE
       *         <remote_htlcpubkey> OP_SWAP OP_SIZE 32 OP_EQUAL
       *         OP_NOTIF
       *             # To local node via HTLC-timeout transaction (timelocked).
       *             OP_DROP 2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
       *         OP_ELSE
       *             # To remote node with preimage.
       *             OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
       *             OP_CHECKSIG
       *         OP_ENDIF
       *     OP_ENDIF
       *
       * Or, with `option_anchor_outputs`:
       *
       *  # To remote node with revocation key
       *  OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
       *  OP_IF
       *      OP_CHECKSIG
       *  OP_ELSE
       *      <remote_htlcpubkey> OP_SWAP OP_SIZE 32 OP_EQUAL
       *      OP_NOTIF
       *          # To local node via HTLC-timeout transaction (timelocked).
       *          OP_DROP 2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
       *      OP_ELSE
       *          # To remote node with preimage.
       *          OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
       *          OP_CHECKSIG
       *      OP_ENDIF
       *      1 OP_CHECKSEQUENCEVERIFY OP_DROP
       *  OP_ENDIF
       */

      public byte[] GetHtlcOfferedRedeemscript(
         PublicKey localhtlckey,
         PublicKey remotehtlckey,
         UInt256 paymenthash,
         PublicKey revocationkey,
         bool optionAnchorOutputs)
      {
         // todo: dan - move this to a hashing interface
         var paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(paymenthash.GetBytes().ToArray());
         var revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationkey);
         var revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

         List<Op> ops = new List<Op>
            {
               OpcodeType.OP_DUP,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(revocationKey160),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_IF,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ELSE,
               Op.GetPushOp(remotehtlckey),
               OpcodeType.OP_SWAP,
               OpcodeType.OP_SIZE,
               Op.GetPushOp(32),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_NOTIF,
               OpcodeType.OP_DROP,
               Op.GetPushOp(2),
               OpcodeType.OP_SWAP,
               Op.GetPushOp(localhtlckey),
               Op.GetPushOp(2),
               OpcodeType.OP_CHECKMULTISIG,
               OpcodeType.OP_ELSE,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(paymentHash160),
               OpcodeType.OP_EQUALVERIFY,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_ENDIF
         };

         if (optionAnchorOutputs)
         {
            ops.Insert(ops.Count - 1, Op.GetPushOp(1));
            ops.Insert(ops.Count - 1, OpcodeType.OP_CHECKSEQUENCEVERIFY);
            ops.Insert(ops.Count - 1, OpcodeType.OP_DROP);
         }

         var script = new Script(ops);
         return script.ToBytes();
      }

      /* BOLT #3:
       *
       * #### Received HTLC Outputs
       *
       * This output sends funds to either the remote node after the HTLC-timeout or
       * using the revocation key, or to an HTLC-success transaction with a
       * successful payment preimage. The output is a P2WSH, with a witness script
       * (no `option_anchor_outputs`):
       *
       *     # To remote node with revocation key
       *     OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
       *     OP_IF
       *         OP_CHECKSIG
       *     OP_ELSE
       *         <remote_htlcpubkey> OP_SWAP
       *             OP_SIZE 32 OP_EQUAL
       *         OP_IF
       *             # To local node via HTLC-success transaction.
       *             OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
       *             2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
       *         OP_ELSE
       *             # To remote node after timeout.
       *             OP_DROP <cltv_expiry> OP_CHECKLOCKTIMEVERIFY OP_DROP
       *             OP_CHECKSIG
       *         OP_ENDIF
       *     OP_ENDIF
       *
       * Or, with `option_anchor_outputs`:
       *
       *  # To remote node with revocation key
       *  OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
       *  OP_IF
       *      OP_CHECKSIG
       *  OP_ELSE
       *      <remote_htlcpubkey> OP_SWAP OP_SIZE 32 OP_EQUAL
       *      OP_IF
       *          # To local node via HTLC-success transaction.
       *          OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
       *          2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
       *      OP_ELSE
       *          # To remote node after timeout.
       *          OP_DROP <cltv_expiry> OP_CHECKLOCKTIMEVERIFY OP_DROP
       *          OP_CHECKSIG
       *      OP_ENDIF
       *      1 OP_CHECKSEQUENCEVERIFY OP_DROP
       *  OP_ENDIF
       */

      public byte[] GetHtlcReceivedRedeemscript(
         long expirylocktime,
         PublicKey localhtlckey,
         PublicKey remotehtlckey,
         UInt256 paymenthash,
         PublicKey revocationkey,
         bool optionAnchorOutputs)
      {
         // todo: dan - move this to a hashing interface
         var paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(paymenthash.GetBytes().ToArray());
         var revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationkey);
         var revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

         List<Op> ops = new List<Op>
         {
            OpcodeType.OP_DUP,
            OpcodeType.OP_HASH160,
            Op.GetPushOp(revocationKey160),
            OpcodeType.OP_EQUAL,
            OpcodeType.OP_IF,
            OpcodeType.OP_CHECKSIG,
            OpcodeType.OP_ELSE,
            Op.GetPushOp(remotehtlckey),
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
            Op.GetPushOp(localhtlckey),
            Op.GetPushOp(2),
            OpcodeType.OP_CHECKMULTISIG,
            OpcodeType.OP_ELSE,
            OpcodeType.OP_DROP,
            Op.GetPushOp(expirylocktime),
            OpcodeType.OP_CHECKLOCKTIMEVERIFY,
            OpcodeType.OP_DROP,
            OpcodeType.OP_CHECKSIG,
            OpcodeType.OP_ENDIF,
            OpcodeType.OP_ENDIF
      };

         if (optionAnchorOutputs)
         {
            ops.Insert(ops.Count - 1, Op.GetPushOp(1));
            ops.Insert(ops.Count - 1, OpcodeType.OP_CHECKSEQUENCEVERIFY);
            ops.Insert(ops.Count - 1, OpcodeType.OP_DROP);
         }

         var script = new Script(ops);
         return script.ToBytes();
      }

      public ulong CommitNumberObscurer(
         PublicKey openerPaymentBasepoint,
         PublicKey accepterPaymentBasepoint)
      {
         Span<byte> bytes = stackalloc byte[66];
         openerPaymentBasepoint.GetSpan().CopyTo(bytes);
         accepterPaymentBasepoint.GetSpan().CopyTo(bytes.Slice(33));

         var hashed = HashGenerator.Sha256(bytes);

         // the lower 48 bits of the hash above
         Span<byte> output = stackalloc byte[6];
         hashed.Slice(26).CopyTo(output);

         Uint48 ret = new Uint48(output);//  BitConverter.ToUInt64(output);

         Span<byte> output2 = stackalloc byte[8];
         hashed.Slice(26).CopyTo(output2.Slice(2));
         output2.Reverse();

         var n2 = BitConverter.ToUInt64(output2);
         return n2;
         //return ret;
      }

      public byte[] FundingRedeemScript(PublicKey pubkey1, PublicKey pubkey2)
      {
         var comparer = new LexicographicByteComparer();

         ReadOnlySpan<byte> first, second;
         if (comparer.Compare(pubkey1, pubkey2) < 0)
         {
            first = pubkey1.GetSpan();
            second = pubkey2.GetSpan();
         }
         else
         {
            first = pubkey2.GetSpan();
            second = pubkey1.GetSpan();
         }

         List<Op> ops = new List<Op>
         {
            Op.GetPushOp(2),
            Op.GetPushOp(first.ToArray()),
            Op.GetPushOp(second.ToArray()),
            Op.GetPushOp(2),
            OpcodeType.OP_CHECKMULTISIG
         };

         var script = new Script(ops);
         return script.ToBytes();
      }

      public Bitcoin.Primitives.Fundamental.TransactionSignature SignCommitmentInput(TransactionSerializer serializer, Transaction transaction, PrivateKey privateKey, uint inputIndex = 0, byte[]? redeemScript = null, ulong? amountSats = null)
      {
         // todo: dan move the trx serializer to the constructor

         // Currently we use NBitcoin to create the transaction hash to be signed,
         // the extra serialization to NBitcoin Transaction is costly so later
         // we will move to generating the hash to sign and signatures directly in code.

         var key = new NBitcoin.Key(privateKey);

         var buffer = new ArrayBufferWriter<byte>();
         serializer.Serialize(transaction, 1, buffer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));
         var trx = NBitcoin.Network.Main.CreateTransaction();
         trx.FromBytes(buffer.WrittenSpan.ToArray());

         // Create the P2WSH redeem script
         var wscript = new Script(redeemScript);
         var utxo = new NBitcoin.TxOut(Money.Satoshis(amountSats.Value), wscript.WitHash);
         var outpoint = new NBitcoin.OutPoint(trx.Inputs[inputIndex].PrevOut);
         ScriptCoin witnessCoin = new ScriptCoin(new Coin(outpoint, utxo), wscript);

         var hashToSigh = trx.GetSignatureHash(witnessCoin.GetScriptCode(), (int)inputIndex, SigHash.All, utxo, HashVersion.WitnessV0);
         var sig = key.Sign(hashToSigh, SigHash.All, useLowR: false);

         return new Bitcoin.Primitives.Fundamental.TransactionSignature(sig.ToBytes());
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

         return feerate_per_kw * baseAmount / 1000;
      }

      public Transaction CreateCommitmenTransaction(
         OutPoint funding_txout,
         ulong funding,
         PublicKey local_funding_key,
         PublicKey remote_funding_key,
         Side opener,
         ushort to_self_delay,
         Keyset Keyset,
         uint feerate_per_kw,
         ulong dust_limit_satoshis,
         ulong self_pay_msat,
         ulong other_pay_msat,
         List<Htlc> htlcs,
         ulong commitment_number,
         ulong cn_obscurer,
         bool option_anchor_outputs,
         Side side)
      {
         // TODO: ADD TRACE LOGS

         // BOLT3 Commitment Transaction Construction
         // 1. Initialize the commitment transaction input and locktime

         var obscured = commitment_number ^ cn_obscurer;

         var transaction = new Transaction
         {
            Version = 2,
            LockTime = (uint)(0x20000000 | (obscured & 0xffffff)),
            Inputs = new[]
            {
               new TransactionInput
               {
                  PreviousOutput = funding_txout,
                  Sequence = (uint)(0x80000000 | ((obscured>>24) & 0xFFFFFF)),
               }
            }
         };

         // BOLT3 Commitment Transaction Construction
         // 1. Calculate which committed HTLCs need to be trimmed
         var htlcsUntrimmed = new List<Htlc>();
         foreach (Htlc htlc in htlcs)
         {
            if (htlc.Side == side)
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

               /* BOLT #3:
               *
               * The fee for an HTLC-timeout transaction:
               * - MUST BE calculated to match:
               *   1. Multiply `feerate_per_kw` by 663 (666 if `option_anchor_outputs`
               *      applies) and divide by 1000 (rounding down).
               */

               uint base_time_out_fee = option_anchor_outputs ? (uint)666 : (uint)663;
               ulong htlc_fee_timeout_fee = feerate_per_kw * base_time_out_fee / 1000;
               ulong dust_plust_fee = dust_limit_satoshis + htlc_fee_timeout_fee;
               ulong dust_plust_fee_msat = dust_plust_fee * 1000;

               if (htlc.amount < dust_plust_fee_msat)
               {
                  // do not add the htlc outpout
               }
               else
               {
                  htlcsUntrimmed.Add(htlc);
               }
            }
            else
            {
               /* BOLT #3:
               *
               *  - for every received HTLC:
               *    - if the HTLC amount minus the HTLC-success fee would be less than
               *    `dust_limit_satoshis` set by the transaction owner:
               *      - MUST NOT contain that output.
               *    - otherwise:
               *      - MUST be generated as specified in
               */

               /* BOLT #3:
                *
                * The fee for an HTLC-success transaction:
                * - MUST BE calculated to match:
                *   1. Multiply `feerate_per_kw` by 703 (706 if `option_anchor_outputs`
                *      applies) and divide by 1000 (rounding down).
                */

               uint base_success_fee = option_anchor_outputs ? (uint)706 : (uint)703;
               ulong htlc_fee_success_fee = feerate_per_kw * base_success_fee / 1000;

               ulong dust_plust_fee = dust_limit_satoshis + htlc_fee_success_fee;
               ulong dust_plust_fee_msat = dust_plust_fee * 1000;

               if (htlc.amount < dust_plust_fee_msat)
               {
                  // do not add the htlc outpout
               }
               else
               {
                  htlcsUntrimmed.Add(htlc);
               }
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

         base_fee = feerate_per_kw * weight / 1000;

         /* BOLT #3:
	       * If `option_anchor_outputs` applies to the commitment
	       * transaction, also subtract two times the fixed anchor size
	       * of 330 sats from the funder (either `to_local` or
	       * `to_remote`).
	       */
         if (option_anchor_outputs)
         {
            base_fee += 660;
         }

         // todo log base fee

         // BOLT3 Commitment Transaction Construction
         // 4. Subtract this base fee from the funder (either to_local or to_remote).
         // If option_anchor_outputs applies to the commitment transaction,
         // also subtract two times the fixed anchor size of 330 sats from the funder (either to_local or to_remote).

         ulong base_fee_msat = base_fee * 1000;

         if (opener == side)
         {
            if (self_pay_msat < base_fee_msat)
            {
               self_pay_msat = 0;
            }
            else
            {
               self_pay_msat = self_pay_msat - base_fee_msat;
            }
         }
         else
         {
            if (other_pay_msat < base_fee_msat)
            {
               other_pay_msat = 0;
            }
            else
            {
               other_pay_msat = other_pay_msat - base_fee_msat;
            }
         }

         //if (opener == side)
         //{
         //   self_pay -= base_fee;

         //   if (option_anchor_outputs)
         //   {
         //      self_pay -= 660;
         //   }
         //}
         //else
         //{
         //   other_pay -= base_fee;

         //   if (option_anchor_outputs)
         //   {
         //      other_pay -= 660;
         //   }
         //}

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

               var wscript = GetHtlcOfferedRedeemscript(
                  Keyset.self_htlc_key,
                  Keyset.other_htlc_key,
                  htlc.rhash,
                  Keyset.self_revocation_key,
                  option_anchor_outputs);

               var wscriptinst = new Script(wscript);

               var p2wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

               outputs.Add(new HtlcOutputsInfo
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2wsh.ToBytes()
                  },
                  CltvExpirey = htlc.expirylocktime
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

               var wscript = GetHtlcReceivedRedeemscript(
                  htlc.expirylocktime,
                  Keyset.self_htlc_key,
                  Keyset.other_htlc_key,
                  htlc.rhash,
                  Keyset.self_revocation_key,
                  option_anchor_outputs);

               var wscriptinst = new Script(wscript);

               var p2wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

               outputs.Add(new HtlcOutputsInfo
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2wsh.ToBytes()
                  },
                  CltvExpirey = htlc.expirylocktime
               });
            }
         }

         ulong dust_limit_msat = dust_limit_satoshis * 1000;

         // BOLT3 Commitment Transaction Construction
         // 7. If the to_local amount is greater or equal to dust_limit_satoshis, add a to_local output.

         bool to_local = false;
         if (self_pay_msat >= dust_limit_msat)
         {
            // todo round down msat to sat in s common method
            ulong amount = self_pay_msat / 1000;

            var wscript = GetRevokeableRedeemscript(Keyset.self_revocation_key, to_self_delay, Keyset.self_delayed_payment_key);

            var wscriptinst = new Script(wscript);

            var p2wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

            outputs.Add(new HtlcOutputsInfo
            {
               TransactionOutput = new TransactionOutput
               {
                  Value = (long)amount,
                  PublicKeyScript = p2wsh.ToBytes()
               },
               CltvExpirey = to_self_delay
            });

            to_local = true;
         }

         // BOLT3 Commitment Transaction Construction
         // 8. If the to_remote amount is greater or equal to dust_limit_satoshis, add a to_remote output.

         bool to_remote = false;
         if (other_pay_msat >= dust_limit_msat)
         {
            // todo round down msat to sat in s common method
            ulong amount = other_pay_msat / 1000;

            // BOLT3:
            // If option_anchor_outputs applies to the commitment transaction,
            // the to_remote output is encumbered by a one block csv lock.
            // <remote_pubkey> OP_CHECKSIGVERIFY 1 OP_CHECKSEQUENCEVERIFY
            // Otherwise, this output is a simple P2WPKH to `remotepubkey`.

            Script p2wsh;
            if (option_anchor_outputs)
            {
               var wscript = AnchorToRemoteRedeem(Keyset.other_payment_key);

               var wscriptinst = new Script(wscript);

               p2wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface
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

               var wscriptinst = new Script(wscript);

               var p2wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

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

               var wscriptinst = new Script(wscript);

               var p2wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

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

         var sorter = new HtlcLexicographicComparer(new LexicographicByteComparer());

         outputs.Sort(sorter);

         transaction.Outputs = outputs.Select(s => s.TransactionOutput).ToArray();

         return transaction;
      }
   }

   public class HtlcOutputsInfo
   {
      public TransactionOutput TransactionOutput { get; set; }
      public ulong CltvExpirey { get; set; }
   }

   public class HtlcLexicographicComparer : IComparer<HtlcOutputsInfo>
   {
      private readonly LexicographicByteComparer _lexicographicByteComparer;

      public HtlcLexicographicComparer(LexicographicByteComparer lexicographicByteComparer)
      {
         _lexicographicByteComparer = lexicographicByteComparer;
      }

      public int Compare(HtlcOutputsInfo x, HtlcOutputsInfo y)
      {
         if (x?.TransactionOutput?.PublicKeyScript == null) return 1;
         if (y?.TransactionOutput?.PublicKeyScript == null) return -1;

         if (x.TransactionOutput.Value > y.TransactionOutput.Value)
         {
            return 1;
         }

         if (x.TransactionOutput.Value < y.TransactionOutput.Value)
         {
            return -1;
         }

         if (x.TransactionOutput.PublicKeyScript != y.TransactionOutput.PublicKeyScript)
         {
            return _lexicographicByteComparer.Compare(x.TransactionOutput.PublicKeyScript, y.TransactionOutput.PublicKeyScript);
         }

         return x.CltvExpirey < y.CltvExpirey ? 1 : -1;
      }
   }

   public class LexicographicByteComparer : IComparer<byte[]>
   {
      public int Compare(byte[] x, byte[] y)
      {
         int lenRet = x.Length.CompareTo(y.Length);

         if (lenRet != 0) return lenRet;

         int len = Math.Min(x.Length, y.Length);
         for (int i = 0; i < len; i++)
         {
            int c = x[i].CompareTo(y[i]);
            if (c != 0)
            {
               return c;
            }
         }

         return 0;
      }
   }

   public class ChannelConfig
   {
      /* Database ID */
      public ulong Id { get; set; }

      /* BOLT #2:
       *
       * `dust_limit_satoshis` is the threshold below which outputs should
       * not be generated for this node's commitment or HTLC transaction */
      public ulong dust_limit { get; set; }

      /* BOLT #2:
       *
       * `max_htlc_value_in_flight_msat` is a cap on total value of
       * outstanding HTLCs, which allows a node to limit its exposure to
       * HTLCs */
      public ulong max_htlc_value_in_flight { get; set; }

      /* BOLT #2:
       *
       * `channel_reserve_satoshis` is the minimum amount that the other
       * node is to keep as a direct payment. */
      public ulong channel_reserve { get; set; }

      /* BOLT #2:
       *
       * `htlc_minimum_msat` indicates the smallest value HTLC this node
       * will accept.
       */
      public ulong htlc_minimum { get; set; }

      /* BOLT #2:
       *
       * `to_self_delay` is the number of blocks that the other node's
       * to-self outputs must be delayed, using `OP_CHECKSEQUENCEVERIFY`
       * delays */
      public ushort to_self_delay { get; set; }

      /* BOLT #2:
       *
       * similarly, `max_accepted_htlcs` limits the number of outstanding
       * HTLCs the other node can offer. */
      public ushort max_accepted_htlcs { get; set; }
   };
}
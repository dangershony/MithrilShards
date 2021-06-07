using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using NBitcoin;
using Protocol.Hashing;
using Protocol.Types;
using OutPoint = Bitcoin.Primitives.Types.OutPoint;
using Transaction = Bitcoin.Primitives.Types.Transaction;

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

      /* BOLT #3:
       *
       * This output sends funds back to the owner of this commitment transaction and
       * thus must be timelocked using `OP_CHECKSEQUENCEVERIFY`. It can be claimed, without delay,
       * by the other party if they know the revocation private key. The output is a
       * version-0 P2WSH, with a witness script:
       *
       *     OP_IF
       *         # Penalty transaction
       *         <revocationpubkey>
       *     OP_ELSE
       *         `to_self_delay`
       *         OP_CHECKSEQUENCEVERIFY
       *         OP_DROP
       *         <local_delayedpubkey>
       *     OP_ENDIF
       *     OP_CHECKSIG
       */

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
         byte[]? paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(paymenthash.GetBytes().ToArray());
         byte[]? revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationkey);
         byte[]? revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

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
         ulong expirylocktime,
         PublicKey localhtlckey,
         PublicKey remotehtlckey,
         UInt256 paymenthash,
         PublicKey revocationkey,
         bool optionAnchorOutputs)
      {
         // todo: dan - move this to a hashing interface
         byte[]? paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(paymenthash.GetBytes().ToArray());
         byte[]? revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationkey);
         byte[]? revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

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
            Op.GetPushOp((long)expirylocktime),
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

         ReadOnlySpan<byte> hashed = HashGenerator.Sha256(bytes);

         // the lower 48 bits of the hash above
         Span<byte> output = stackalloc byte[6];
         hashed.Slice(26).CopyTo(output);

         Uint48 ret = new Uint48(output);//  BitConverter.ToUInt64(output);

         Span<byte> output2 = stackalloc byte[8];
         hashed.Slice(26).CopyTo(output2.Slice(2));
         output2.Reverse();

         ulong n2 = BitConverter.ToUInt64(output2);
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

      public void SetCommitmentInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, byte[] pubkeyScriptToRedeem)
      {
         var redeemScript = new Script(
            OpcodeType.OP_0,
            Op.GetPushOp(localSignature),
            Op.GetPushOp(remoteSignature),
            Op.GetPushOp(pubkeyScriptToRedeem))
            .ToWitScript();

         transactionInput.ScriptWitness = new TransactionWitness
         {
            Components = redeemScript.Pushes.Select(opcode => new TransactionWitnessComponent { RawData = opcode }).ToArray()
         };
      }

      public BitcoinSignature SignInput(TransactionSerializer serializer, Transaction transaction, PrivateKey privateKey, uint inputIndex = 0, byte[]? redeemScript = null, ulong? amountSats = null)
      {
         // todo: dan move the trx serializer to the constructor

         // Currently we use NBitcoin to create the transaction hash to be signed,
         // the extra serialization to NBitcoin Transaction is costly so later
         // we will move to generating the hash to sign and signatures directly in code.

         var key = new NBitcoin.Key(privateKey);

         var buffer = new ArrayBufferWriter<byte>();
         serializer.Serialize(transaction, 1, buffer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));
         NBitcoin.Transaction? trx = NBitcoin.Network.Main.CreateTransaction();
         trx.FromBytes(buffer.WrittenSpan.ToArray());

         // Create the P2WSH redeem script
         var wscript = new Script(redeemScript);
         var utxo = new NBitcoin.TxOut(Money.Satoshis(amountSats.Value), wscript.WitHash);
         var outpoint = new NBitcoin.OutPoint(trx.Inputs[inputIndex].PrevOut);
         ScriptCoin witnessCoin = new ScriptCoin(new Coin(outpoint, utxo), wscript);

         uint256? hashToSigh = trx.GetSignatureHash(witnessCoin.GetScriptCode(), (int)inputIndex, SigHash.All, utxo, HashVersion.WitnessV0);
         TransactionSignature? sig = key.Sign(hashToSigh, SigHash.All, useLowR: false);

         return new BitcoinSignature(sig.ToBytes());
      }

      public enum Side
      {
         Local,
         Remote,
      };

      public Transaction CreateHtlcSuccessTransaction(
         bool optionAnchorOutputs,
         uint feeratePerKw,
         ulong amountMsat,
         OutPoint commitOutPoint,
         PublicKey revocationPubkey,
         PublicKey localDelayedkey,
         ushort toSelfDelay
      )
      {
         ulong htlcFee = HtlcSuccessFee(optionAnchorOutputs, feeratePerKw);

         uint locktime = 0;

         return CreateHtlcTransaction((uint)(optionAnchorOutputs ? 1 : 0),
            locktime,
            amountMsat,
            htlcFee,
            commitOutPoint,
            revocationPubkey,
            localDelayedkey,
            toSelfDelay);
      }

      public Transaction CreateHtlcTimeoutTransaction(
         bool optionAnchorOutputs,
         uint feeratePerKw,
         ulong amountMsat,
         OutPoint commitOutPoint,
         PublicKey revocationPubkey,
         PublicKey localDelayedkey,
         ushort toSelfDelay,
         uint cltvExpiry
      )
      {
         ulong htlcFee = HtlcTimeoutFee(optionAnchorOutputs, feeratePerKw);

         uint locktime = cltvExpiry;

         return CreateHtlcTransaction((uint)(optionAnchorOutputs ? 1 : 0),
            locktime,
            amountMsat,
            htlcFee,
            commitOutPoint,
            revocationPubkey,
            localDelayedkey,
            toSelfDelay);
      }

      public Transaction CreateHtlcTransaction(
         uint sequence,
         uint locktime,
         ulong amountMsat,
         ulong htlcFee,
         OutPoint commitOutPoint,
         PublicKey revocationPubkey,
         PublicKey localDelayedkey,
         ushort toSelfDelay
         )
      {
         var transaction = new Transaction
         {
            Version = 2,
            LockTime = (uint)locktime,
            Inputs = new[]
            {
               new TransactionInput
               {
                  PreviousOutput = commitOutPoint,
                  Sequence = sequence ,
               }
            }
         };

         byte[]? wscript = GetRevokeableRedeemscript(revocationPubkey, toSelfDelay, localDelayedkey);
         var wscriptinst = new Script(wscript);
         Script? p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

         ulong amountSat = amountMsat / 1000;
         if (htlcFee > amountSat) throw new Exception();
         amountSat -= htlcFee;

         transaction.Outputs = new TransactionOutput[]
         {
            new TransactionOutput
            {
               Value = (long)amountSat,
               PublicKeyScript = p2Wsh.ToBytes()
            },
         };

         return transaction;
      }

      /* BOLT #3:
      *
      * The fee for an HTLC-timeout transaction:
      * - MUST BE calculated to match:
      *   1. Multiply `feerate_per_kw` by 663 (666 if `option_anchor_outputs`
      *      applies) and divide by 1000 (rounding down).
      */

      public ulong HtlcTimeoutFee(bool optionAnchorOutputs, uint feeratePerKw)
      {
         uint baseTimeOutFee = optionAnchorOutputs ? (uint)666 : (uint)663;
         ulong htlcFeeTimeoutFee = feeratePerKw * baseTimeOutFee / 1000;
         return htlcFeeTimeoutFee;
      }

      /* BOLT #3:
       *
       * The fee for an HTLC-success transaction:
       * - MUST BE calculated to match:
       *   1. Multiply `feerate_per_kw` by 703 (706 if `option_anchor_outputs`
       *      applies) and divide by 1000 (rounding down).
       */

      public ulong HtlcSuccessFee(bool optionAnchorOutputs, uint feeratePerKw)
      {
         uint baseSuccessFee = optionAnchorOutputs ? (uint)706 : (uint)703;
         ulong htlcFeeSuccessFee = feeratePerKw * baseSuccessFee / 1000;
         return htlcFeeSuccessFee;
      }

      public CommitmenTransactionOut CreateCommitmenTransaction(
         OutPoint fundingTxout,
         ulong funding,
         PublicKey localFundingKey,
         PublicKey remoteFundingKey,
         Side opener,
         ushort toSelfDelay,
         Keyset keyset,
         uint feeratePerKw,
         ulong dustLimitSatoshis,
         ulong selfPayMsat,
         ulong otherPayMsat,
         List<Htlc> htlcs,
         ulong commitmentNumber,
         ulong cnObscurer,
         bool optionAnchorOutputs,
         Side side)
      {
         // TODO: ADD TRACE LOGS

         // BOLT3 Commitment Transaction Construction
         // 1. Initialize the commitment transaction input and locktime

         ulong obscured = commitmentNumber ^ cnObscurer;

         var transaction = new Transaction
         {
            Version = 2,
            LockTime = (uint)(0x20000000 | (obscured & 0xffffff)),
            Inputs = new[]
            {
               new TransactionInput
               {
                  PreviousOutput = fundingTxout,
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

               ulong htlcFeeTimeoutFee = HtlcTimeoutFee(optionAnchorOutputs, feeratePerKw);

               ulong dustPlustFee = dustLimitSatoshis + htlcFeeTimeoutFee;
               ulong dustPlustFeeMsat = dustPlustFee * 1000;

               if (htlc.Amount < dustPlustFeeMsat)
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

               ulong htlcFeeSuccessFee = HtlcSuccessFee(optionAnchorOutputs, feeratePerKw);

               ulong dustPlustFee = dustLimitSatoshis + htlcFeeSuccessFee;
               ulong dustPlustFeeMsat = dustPlustFee * 1000;

               if (htlc.Amount < dustPlustFeeMsat)
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
         ulong baseFee;
         ulong numUntrimmedHtlcs = (ulong)htlcsUntrimmed.Count;
         /* BOLT #3:
          *
          * The base fee for a commitment transaction:
          *  - MUST be calculated to match:
          *    1. Start with `weight` = 724 (1124 if `option_anchor_outputs` applies).
          */
         if (optionAnchorOutputs)
            weight = 1124;
         else
            weight = 724;

         /* BOLT #3:
          *
          *    2. For each committed HTLC, if that output is not trimmed as
          *       specified in [Trimmed Outputs](#trimmed-outputs), add 172
          *       to `weight`.
          */
         weight += 172 * numUntrimmedHtlcs;

         baseFee = feeratePerKw * weight / 1000;

         /* BOLT #3:
          * If `option_anchor_outputs` applies to the commitment
          * transaction, also subtract two times the fixed anchor size
          * of 330 sats from the funder (either `to_local` or
          * `to_remote`).
          */
         if (optionAnchorOutputs)
         {
            baseFee += 660;
         }

         // todo log base fee

         // BOLT3 Commitment Transaction Construction
         // 4. Subtract this base fee from the funder (either to_local or to_remote).
         // If option_anchor_outputs applies to the commitment transaction,
         // also subtract two times the fixed anchor size of 330 sats from the funder (either to_local or to_remote).

         ulong baseFeeMsat = baseFee * 1000;

         if (opener == side)
         {
            if (selfPayMsat < baseFeeMsat)
            {
               selfPayMsat = 0;
            }
            else
            {
               selfPayMsat = selfPayMsat - baseFeeMsat;
            }
         }
         else
         {
            if (otherPayMsat < baseFeeMsat)
            {
               otherPayMsat = 0;
            }
            else
            {
               otherPayMsat = otherPayMsat - baseFeeMsat;
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

         var outputs = new List<HtlcToOutputMaping>();

         // BOLT3 Commitment Transaction Construction
         // 5. For every offered HTLC, if it is not trimmed, add an offered HTLC output.

         foreach (Htlc htlc in htlcsUntrimmed)
         {
            if (htlc.Side == side)
            {
               // todo round down msat to sat in s common method
               ulong amount = htlc.Amount / 1000;

               byte[]? wscript = GetHtlcOfferedRedeemscript(
                  keyset.SelfHtlcKey,
                  keyset.OtherHtlcKey,
                  htlc.Rhash,
                  keyset.SelfRevocationKey,
                  optionAnchorOutputs);

               var wscriptinst = new Script(wscript);

               Script? p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

               outputs.Add(new HtlcToOutputMaping
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2Wsh.ToBytes()
                  },
                  CltvExpirey = htlc.Expirylocktime,
                  WitnessHashRedeemScript = wscript,
                  Htlc = htlc
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
               ulong amount = htlc.Amount / 1000;

               var wscript = GetHtlcReceivedRedeemscript(
                  htlc.Expirylocktime,
                  keyset.SelfHtlcKey,
                  keyset.OtherHtlcKey,
                  htlc.Rhash,
                  keyset.SelfRevocationKey,
                  optionAnchorOutputs);

               var wscriptinst = new Script(wscript);

               var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

               outputs.Add(new HtlcToOutputMaping
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2Wsh.ToBytes()
                  },
                  CltvExpirey = htlc.Expirylocktime,
                  WitnessHashRedeemScript = wscript,
                  Htlc = htlc
               });
            }
         }

         ulong dustLimitMsat = dustLimitSatoshis * 1000;

         // BOLT3 Commitment Transaction Construction
         // 7. If the to_local amount is greater or equal to dust_limit_satoshis, add a to_local output.

         bool toLocal = false;
         if (selfPayMsat >= dustLimitMsat)
         {
            // todo round down msat to sat in s common method
            ulong amount = selfPayMsat / 1000;

            var wscript = GetRevokeableRedeemscript(keyset.SelfRevocationKey, toSelfDelay, keyset.SelfDelayedPaymentKey);

            var wscriptinst = new Script(wscript);

            var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

            outputs.Add(new HtlcToOutputMaping
            {
               TransactionOutput = new TransactionOutput
               {
                  Value = (long)amount,
                  PublicKeyScript = p2Wsh.ToBytes()
               },
               CltvExpirey = toSelfDelay,
            });

            toLocal = true;
         }

         // BOLT3 Commitment Transaction Construction
         // 8. If the to_remote amount is greater or equal to dust_limit_satoshis, add a to_remote output.

         bool toRemote = false;
         if (otherPayMsat >= dustLimitMsat)
         {
            // todo round down msat to sat in s common method
            ulong amount = otherPayMsat / 1000;

            // BOLT3:
            // If option_anchor_outputs applies to the commitment transaction,
            // the to_remote output is encumbered by a one block csv lock.
            // <remote_pubkey> OP_CHECKSIGVERIFY 1 OP_CHECKSEQUENCEVERIFY
            // Otherwise, this output is a simple P2WPKH to `remotepubkey`.

            Script p2Wsh;
            if (optionAnchorOutputs)
            {
               var wscript = AnchorToRemoteRedeem(keyset.OtherPaymentKey);

               var wscriptinst = new Script(wscript);

               p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface
            }
            else
            {
               p2Wsh = PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey(new PubKey(keyset.OtherPaymentKey)); // todo: dan - move this to interface
            }

            outputs.Add(new HtlcToOutputMaping
            {
               TransactionOutput = new TransactionOutput
               {
                  Value = (long)amount,
                  PublicKeyScript = p2Wsh.ToBytes()
               },
               CltvExpirey = 0
            });

            toRemote = true;
         }

         // BOLT3 Commitment Transaction Construction
         // 9. If option_anchor_outputs applies to the commitment transaction:
         //   if to_local exists or there are untrimmed HTLCs, add a to_local_anchor output
         //   if to_remote exists or there are untrimmed HTLCs, add a to_remote_anchor output

         if (optionAnchorOutputs)
         {
            if (toLocal || htlcsUntrimmed.Count != 0)
            {
               // todo round down msat to sat in s common method
               ulong amount = 330;

               var wscript = bitcoin_wscript_anchor(localFundingKey);

               var wscriptinst = new Script(wscript);

               var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

               outputs.Add(new HtlcToOutputMaping
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2Wsh.ToBytes()
                  },
                  CltvExpirey = 0
               });
            }

            if (toRemote || htlcsUntrimmed.Count != 0)
            {
               // todo round down msat to sat in s common method
               ulong amount = 330;

               var wscript = bitcoin_wscript_anchor(remoteFundingKey);

               var wscriptinst = new Script(wscript);

               var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

               outputs.Add(new HtlcToOutputMaping
               {
                  TransactionOutput = new TransactionOutput
                  {
                     Value = (long)amount,
                     PublicKeyScript = p2Wsh.ToBytes()
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

         return new CommitmenTransactionOut { Transaction = transaction, Htlcs = outputs };
      }
   }

   public class HtlcToOutputMaping
   {
      public TransactionOutput TransactionOutput { get; set; }
      public ulong CltvExpirey { get; set; }
      public byte[] WitnessHashRedeemScript { get; set; }
      public Htlc? Htlc { get; set; }
   }

   public class CommitmenTransactionOut
   {
      public List<HtlcToOutputMaping> Htlcs { get; set; }
      public Transaction Transaction { get; set; }
   }

   public class HtlcLexicographicComparer : IComparer<HtlcToOutputMaping>
   {
      private readonly LexicographicByteComparer _lexicographicByteComparer;

      public HtlcLexicographicComparer(LexicographicByteComparer lexicographicByteComparer)
      {
         _lexicographicByteComparer = lexicographicByteComparer;
      }

      public int Compare(HtlcToOutputMaping x, HtlcToOutputMaping y)
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
      public ulong DustLimit { get; set; }

      /* BOLT #2:
       *
       * `max_htlc_value_in_flight_msat` is a cap on total value of
       * outstanding HTLCs, which allows a node to limit its exposure to
       * HTLCs */
      public ulong MaxHtlcValueInFlight { get; set; }

      /* BOLT #2:
       *
       * `channel_reserve_satoshis` is the minimum amount that the other
       * node is to keep as a direct payment. */
      public ulong ChannelReserve { get; set; }

      /* BOLT #2:
       *
       * `htlc_minimum_msat` indicates the smallest value HTLC this node
       * will accept.
       */
      public ulong HtlcMinimum { get; set; }

      /* BOLT #2:
       *
       * `to_self_delay` is the number of blocks that the other node's
       * to-self outputs must be delayed, using `OP_CHECKSEQUENCEVERIFY`
       * delays */
      public ushort ToSelfDelay { get; set; }

      /* BOLT #2:
       *
       * similarly, `max_accepted_htlcs` limits the number of outstanding
       * HTLCs the other node can offer. */
      public ushort MaxAcceptedHtlcs { get; set; }
   };
}
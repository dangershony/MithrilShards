using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using NBitcoin;
using Protocol.Types;
using OutPoint = Bitcoin.Primitives.Types.OutPoint;
using Transaction = Bitcoin.Primitives.Types.Transaction;

namespace Protocol.Channels
{
   public class CommitmentTransaction
   {
      private readonly LightningScripts _lightningScripts;

      public CommitmentTransaction(LightningScripts lightningScripts)
      {
         _lightningScripts = lightningScripts;
      }

      public CommitmenTransactionOut CreateCommitmenTransaction(
         OutPoint fundingTxout,
         ulong funding,
         PublicKey localFundingKey,
         PublicKey remoteFundingKey,
         ChannelSide opener,
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
         ChannelSide side)
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

               byte[]? wscript = _lightningScripts.GetHtlcOfferedRedeemscript(
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

               var wscript = _lightningScripts.GetHtlcReceivedRedeemscript(
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

            var wscript = _lightningScripts.GetRevokeableRedeemscript(keyset.SelfRevocationKey, toSelfDelay, keyset.SelfDelayedPaymentKey);

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
               var wscript = _lightningScripts.AnchorToRemoteRedeem(keyset.OtherPaymentKey);

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

               var wscript = _lightningScripts.bitcoin_wscript_anchor(localFundingKey);

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

               var wscript = _lightningScripts.bitcoin_wscript_anchor(remoteFundingKey);

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

         byte[]? wscript = _lightningScripts.GetRevokeableRedeemscript(revocationPubkey, toSelfDelay, localDelayedkey);
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
   }
}
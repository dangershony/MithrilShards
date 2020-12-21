using System;
using System.Collections.Generic;
using System.Linq;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using NBitcoin;
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

      public byte[] GetHtlcRedeemscript(
         HtlcOutputInCommitment htlc,
         PublicKey broadcasterHtlcKey,
         PublicKey countersignatoryHtlcKey,
         PublicKey revocationKey)
      {
         var paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(htlc.HtlcOutput.PaymentHash.GetBytes().ToArray());
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
               Op.GetPushOp(htlc.HtlcOutput.CltvExpiry),
               OpcodeType.OP_CHECKLOCKTIMEVERIFY,
               OpcodeType.OP_DROP,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_ENDIF);
         }

         return script.ToBytes();
      }

      public (Transaction Trx, int non_dust_htlc_count, List<HtlcOutputInCommitment> htlcs_included) BuildCommitmentTransaction(
         ulong commitmentNumber,
         TxCreationKeys keys,
         bool local,
         bool generatedByLocal,
         uint feeratePerKw,
         OutPoint fundingTxo,
         ulong holderDustLimitSatoshis,
         ulong counterpartyDustLimitSatoshis,
         ulong valueToSelfMsatInput,
         ulong channelValueSatoshis,
         bool channelOutbound,
         ushort counterpartySelectedContestDelay,
         ushort holderSelectedContestDelay,
         ChannelPublicKeys counterpartyPubkeys,
         ChannelPublicKeys holderKeys,
         List<InboundHtlcOutput> inboundHtlcs,
         List<OutboundHtlcOutput> outboundHtlcs)
      {
         ulong obscuredCommitmentTransactionNumber = get_commitment_transaction_number_obscure_factor() ^ (INITIAL_COMMITMENT_NUMBER - commitmentNumber);

         var transaction = new Transaction
         {
            Version = 2,
            LockTime = (((uint)0x20) << 8 * 3) | ((uint)(obscuredCommitmentTransactionNumber & 0xffffff)),
            Inputs = new[]
            {
               new TransactionInput
               {
                  PreviousOutput = fundingTxo,
                  Sequence = (((uint)0x80) << 8 * 3) |
                             ((uint)(obscuredCommitmentTransactionNumber >> 3 * 8)),
               }
            }
         };

         var txouts = new List<(TransactionOutput Trx, HtlcOutputInCommitment? Htlc)>();
         var includedDustHtlcs = new List<HtlcOutputInCommitment>();

         ulong broadcasterDustLimitSatoshis = local ? holderDustLimitSatoshis : counterpartyDustLimitSatoshis;
         ulong remoteHtlcTotalMsat = 0;
         ulong localHtlcTotalMsat = 0;
         ulong valueToSelfMsatOffset = 0;

         // log_trace

         foreach (InboundHtlcOutput inboundHtlc in inboundHtlcs)
         {
            bool include = false;

            switch (inboundHtlc.State)
            {
               case InboundHtlcState.LocalRemoved:
               case InboundHtlcState.RemoteAnnounced:
               case InboundHtlcState.AwaitingRemoteRevokeToAnnounce:
                  {
                     include = !generatedByLocal;
                     break;
                  }

               case InboundHtlcState.Committed:
               case InboundHtlcState.AwaitingAnnouncedRemoteRevoke:
                  {
                     include = true;
                     break;
                  }
            }

            if (include)
            {
               bool outbound = false;
               bool offerd = outbound == local ? true : false;
               ulong txWeight = outbound == local ? HTLC_TIMEOUT_TX_WEIGHT : HTLC_SUCCESS_TX_WEIGHT;

               var htlcInTx = new HtlcOutputInCommitment { Offered = offerd, HtlcOutput = inboundHtlc.Htlc };

               if (inboundHtlc.Htlc.AmountMsat / 1000 >=
                   broadcasterDustLimitSatoshis + (feeratePerKw * txWeight / 1000))
               {
                  //log_trace!(logger,
                  txouts.Add((new TransactionOutput
                  {
                     PublicKeyScript = GetHtlcRedeemscript(
                        htlcInTx,
                        keys.BroadcasterHtlcKey,
                        keys.CountersignatoryHtlcKey,
                        keys.RevocationKey),
                     Value = (long)inboundHtlc.Htlc.AmountMsat / 1000
                  }, htlcInTx));
               }
               else
               {
                  // log_trace!(
                  includedDustHtlcs.Add(htlcInTx);
               }

               remoteHtlcTotalMsat += inboundHtlc.Htlc.AmountMsat;
            }
            else
            {
               // log_trace!(
               if (inboundHtlc.State == InboundHtlcState.LocalRemoved)
               {
                  if (generatedByLocal)
                  {
                     valueToSelfMsatOffset += inboundHtlc.Htlc.AmountMsat;
                  }
               }
            }
         }

         foreach (OutboundHtlcOutput outboundHtlc in outboundHtlcs)
         {
            bool include = false;

            switch (outboundHtlc.State)
            {
               case OutboundHtlcState.RemoteRemoved:
               case OutboundHtlcState.LocalAnnounced:
               case OutboundHtlcState.AwaitingRemoteRevokeToRemove:
                  {
                     include = generatedByLocal;
                     break;
                  }

               case OutboundHtlcState.Committed:
                  {
                     include = true;
                     break;
                  }
               case OutboundHtlcState.AwaitingRemovedRemoteRevoke:
                  {
                     include = false;
                     break;
                  }
            }

            if (include)
            {
               bool outbound = true;
               bool offerd = outbound == local ? true : false;
               ulong txWeight = outbound == local ? HTLC_TIMEOUT_TX_WEIGHT : HTLC_SUCCESS_TX_WEIGHT;

               var htlcInTx = new HtlcOutputInCommitment { Offered = offerd, HtlcOutput = outboundHtlc.Htlc };

               if (outboundHtlc.Htlc.AmountMsat / 1000 >=
                   broadcasterDustLimitSatoshis + (feeratePerKw * txWeight / 1000))
               {
                  //log_trace!(logger,
                  txouts.Add((new TransactionOutput
                  {
                     PublicKeyScript = GetHtlcRedeemscript(
                        htlcInTx,
                        keys.BroadcasterHtlcKey,
                        keys.CountersignatoryHtlcKey,
                        keys.RevocationKey),
                     Value = (long)outboundHtlc.Htlc.AmountMsat / 1000
                  }, htlcInTx));
               }
               else
               {
                  // log_trace!(
                  includedDustHtlcs.Add(htlcInTx);
               }

               remoteHtlcTotalMsat += outboundHtlc.Htlc.AmountMsat;
            }
            else
            {
               // log_trace!(
               if (outboundHtlc.State == OutboundHtlcState.AwaitingRemoteRevokeToRemove ||
                   outboundHtlc.State == OutboundHtlcState.AwaitingRemovedRemoteRevoke)
               {
                  valueToSelfMsatOffset -= outboundHtlc.Htlc.AmountMsat;
               }

               if (outboundHtlc.State == OutboundHtlcState.RemoteRemoved)
               {
                  if (!generatedByLocal)
                  {
                     valueToSelfMsatOffset += outboundHtlc.Htlc.AmountMsat;
                  }
               }
            }
         }

         ulong valueToSelfMsat = (valueToSelfMsatInput - localHtlcTotalMsat) + valueToSelfMsatOffset;

         if (valueToSelfMsat >= 0) throw new ApplicationException();

         // Note that in case they have several just-awaiting-last-RAA fulfills in-progress (ie
         // AwaitingRemoteRevokeToRemove or AwaitingRemovedRemoteRevoke) we may have allowed them to
         // "violate" their reserve value by couting those against it. Thus, we have to convert
         // everything to i64 before subtracting as otherwise we can overflow.
         ulong valueToRemoteMsat = (channelValueSatoshis * 1000) - (valueToSelfMsatInput) - remoteHtlcTotalMsat - valueToSelfMsatOffset;

         if (valueToRemoteMsat >= 0) throw new ApplicationException();

         ulong totalFee = feeratePerKw * (COMMITMENT_TX_BASE_WEIGHT + (ulong)txouts.Count * COMMITMENT_TX_WEIGHT_PER_HTLC) / 1000;

         ulong valueToSelf;
         ulong valueToRemote;

         if (channelOutbound)
         {
            valueToSelf = valueToSelfMsat / 1000 - totalFee;
            valueToRemote = valueToRemoteMsat / 1000;
         }
         else
         {
            valueToSelf = valueToSelfMsat / 1000;
            valueToRemote = valueToRemoteMsat / 1000 - totalFee;
         };

         ulong valueToA = local ? valueToSelf : valueToRemote;
         ulong valueToB = local ? valueToRemote : valueToSelf;

         if (valueToA >= broadcasterDustLimitSatoshis)
         {
            // log_trace!
            txouts.Add((new TransactionOutput
            {
               PublicKeyScript = GetRevokeableRedeemscript(
                  keys.RevocationKey,
                  local ? counterpartySelectedContestDelay : holderSelectedContestDelay,
                  keys.BroadcasterDelayedPaymentKey),
               Value = (long)valueToA
            }, null));
         }

         if (valueToB >= broadcasterDustLimitSatoshis)
         {
            //log_trace!(

            PublicKey staticPaymentPk = local ? counterpartyPubkeys.PaymentPoint : holderKeys.PaymentPoint;

            txouts.Add((new TransactionOutput
            {
               PublicKeyScript = new Script(Op.GetPushOp(staticPaymentPk)).ToBytes(),
               Value = (long)valueToB
            }, null));
         }

         txouts = txouts.OrderBy(o => o.Htlc?.HtlcOutput.CltvExpiry).ThenBy(o => o.Htlc?.HtlcOutput.PaymentHash).ToList();

         var htlcsIncluded = new List<HtlcOutputInCommitment>();

         uint index = 0;
         foreach ((TransactionOutput Trx, HtlcOutputInCommitment? Htlc) txout in txouts)
         {
            if (txout.Htlc != null)
            {
               txout.Htlc.TransactionOutputIndex = index;
               index++;

               htlcsIncluded.Add(txout.Htlc);
            }
         }

         int nonDustHtlcCount = htlcsIncluded.Count;
         htlcsIncluded.AddRange(includedDustHtlcs);

         return (transaction, nonDustHtlcCount, htlcsIncluded);
      }

      private const ulong INITIAL_COMMITMENT_NUMBER = 0;
      private const ulong HTLC_TIMEOUT_TX_WEIGHT = 0;
      private const ulong HTLC_SUCCESS_TX_WEIGHT = 0;
      private const ulong COMMITMENT_TX_BASE_WEIGHT = 0;
      private const ulong COMMITMENT_TX_WEIGHT_PER_HTLC = 0;

      private ulong get_commitment_transaction_number_obscure_factor()
      {
         //let mut sha = Sha256::engine();

         //let counterparty_payment_point = &self.counterparty_pubkeys.as_ref().unwrap().payment_point.serialize();
         //if self.channel_outbound {
         //   sha.input(&self.holder_keys.pubkeys().payment_point.serialize());
         //   sha.input(counterparty_payment_point);
         //} else {
         //   sha.input(counterparty_payment_point);
         //   sha.input(&self.holder_keys.pubkeys().payment_point.serialize());
         //}
         //let res = Sha256::from_engine(sha).into_inner();

         //((res[26] as u64) << 5*8) |
         //   ((res[27] as u64) << 4*8) |
         //   ((res[28] as u64) << 3*8) |
         //   ((res[29] as u64) << 2*8) |
         //   ((res[30] as u64) << 1*8) |
         //   ((res[31] as u64) << 0*8)

         return 0;
      }
   }
}
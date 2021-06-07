using System;
using System.Collections.Generic;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Types;
using Protocol.Channels;
using Protocol.Channels.Types;
using Protocol.Hashing;
using Xunit;
using OutPoint = Bitcoin.Primitives.Types.OutPoint;

#pragma warning disable IDE1006 // Naming Styles

namespace Protocol.Test.bolt3
{
   /// <summary>
   /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
   /// </summary>
   public class Bolt3AppendixCTests : IClassFixture<Bolt3AppendixCTestContext>
   {
      public Bolt3AppendixCTestContext Context { get; set; }

      public Bolt3AppendixCTests(Bolt3AppendixCTestContext context)
      {
         Context = context;
      }

      [Theory]
      [ClassData(typeof(Bolt3AppendixCTestDataStaticRemotekey))]
      public void Bolt3AppendixC_CommitmentAndHTLCTransactionStaticRemotekey(Bolt3AppendixCTestVectors vectors)
      {
         Context.Keyset.OtherPaymentKey = Context.RemotePaymentBasepoint;

         Bolt3AppendixC_CommitmentAndHTLCTransactionTest(vectors);
      }

      [Theory]
      [ClassData(typeof(Bolt3AppendixCTestData))]
      public void Bolt3AppendixC_CommitmentAndHTLCTransaction(Bolt3AppendixCTestVectors vectors)
      {
         Context.Keyset.OtherPaymentKey = Context.Remotekey;

         Bolt3AppendixC_CommitmentAndHTLCTransactionTest(vectors);
      }

      public void Bolt3AppendixC_CommitmentAndHTLCTransactionTest(Bolt3AppendixCTestVectors vectors)
      {
         CommitmenTransactionOut localCommitmenTransactionOut = Context.CommitmentTransaction.CreateCommitmenTransaction(
                  Context.FundingTxOutpoint,
                  Context.FundingAmount,
                  Context.LocalFundingPubkey,
                  Context.RemoteFundingPubkey,
                  ChannelSide.Local,
                  Context.ToSelfDelay,
                  Context.Keyset,
                  vectors.FeeratePerKw,
                  Context.DustLimit,
                  vectors.ToLocalMsat,
                  vectors.ToRemoteMsat,
                  vectors.Htlcs.htlcs,
                  Context.CommitmentNumber,
                  Context.CnObscurer,
                  Context.OptionAnchorOutputs,
                  ChannelSide.Local);

         CommitmenTransactionOut? remoteCommitmenTransactionOut = Context.CommitmentTransaction.CreateCommitmenTransaction(
                  Context.FundingTxOutpoint,
                  Context.FundingAmount,
                  Context.LocalFundingPubkey,
                  Context.RemoteFundingPubkey,
                  ChannelSide.Remote,
                  Context.ToSelfDelay,
                  Context.Keyset,
                  vectors.FeeratePerKw,
                  Context.DustLimit,
                  vectors.ToLocalMsat,
                  vectors.ToRemoteMsat,
                  vectors.Htlcs.invertedhtlcs,
                  Context.CommitmentNumber,
                  Context.CnObscurer,
                  Context.OptionAnchorOutputs,
                  ChannelSide.Remote);

         Transaction? localTransaction = localCommitmenTransactionOut.Transaction;
         Transaction? remoteTransaction = remoteCommitmenTransactionOut.Transaction;

         localTransaction.Hash = Context.TransactionHashCalculator.ComputeHash(localTransaction, 1);
         remoteTransaction.Hash = Context.TransactionHashCalculator.ComputeHash(remoteTransaction, 1);

         Assert.Equal(localTransaction.Hash, remoteTransaction.Hash);

         // == helper code==
         var outputCommitTx = TransactionHelper.SeriaizeTransaction(Context.TransactionSerializer, Hex.FromString(vectors.OutputCommitTx));
         var newtrx = TransactionHelper.ParseToString(localTransaction);
         var curtrx = TransactionHelper.ParseToString(outputCommitTx);

         byte[]? fundingWscript = Context.Scripts.FundingRedeemScript(Context.LocalFundingPubkey, Context.RemoteFundingPubkey);

         var remoteSignature = Context.CommitmentTransaction.SignInput(Context.TransactionSerializer, localTransaction, Context.RemoteFundingPrivkey, inputIndex: 0, redeemScript: fundingWscript, Context.FundingAmount);
         var expectedRemoteSignature = Hex.ToString(outputCommitTx.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
         Assert.Equal(expectedRemoteSignature, Hex.ToString(remoteSignature.GetSpan()));

         var localSignature = Context.CommitmentTransaction.SignInput(Context.TransactionSerializer, localTransaction, Context.LocalFundingPrivkey, inputIndex: 0, redeemScript: fundingWscript, Context.FundingAmount);
         var expectedLocalSignature = Hex.ToString(outputCommitTx.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
         Assert.Equal(expectedLocalSignature, Hex.ToString(localSignature.GetSpan()));

         Context.Scripts.SetCommitmentInputWitness(localTransaction.Inputs[0], localSignature, remoteSignature, fundingWscript);

         byte[] localTransactionBytes = TransactionHelper.DeseriaizeTransaction(Context.TransactionSerializer, localTransaction);

         Assert.Equal(vectors.OutputCommitTx, Hex.ToString(localTransactionBytes.AsSpan()).Substring(2));

         /* FIXME: naming here is kind of backwards: local revocation key
          * is derived from remote revocation basepoint, but it's local */

         Keyset keyset = new Keyset
         {
            SelfRevocationKey = Context.RemoteRevocationKey,
            SelfDelayedPaymentKey = Context.LocalDelayedkey,
            SelfPaymentKey = Context.Localkey,
            OtherPaymentKey = Context.Remotekey,
            SelfHtlcKey = Context.LocalHtlckey,
            OtherHtlcKey = Context.RemoteHtlckey,
         };

         int htlcOutputIndex = 0;
         for (int htlcIndex = 0; htlcIndex < localCommitmenTransactionOut.Htlcs.Count; htlcIndex++)
         {
            HtlcToOutputMaping htlc = localCommitmenTransactionOut.Htlcs[htlcIndex];

            if (htlc.Htlc == null)
            {
               htlcIndex++;
               continue;
            }

            OutPoint outPoint = new OutPoint { Hash = localTransaction.Hash, Index = (uint)htlcIndex };
            Bitcoin.Primitives.Types.Transaction htlcTransaction;
            byte[] redeemScript;
            if (htlc.Htlc.Side == ChannelSide.Local)
            {
               redeemScript = Context.Scripts.GetHtlcOfferedRedeemscript(
                              Context.LocalHtlckey,
                              Context.RemoteHtlckey,
                              htlc.Htlc.Rhash,
                              Context.RemoteRevocationKey,
                              Context.OptionAnchorOutputs);

               htlcTransaction = Context.CommitmentTransaction.CreateHtlcTimeoutTransaction(
                                 Context.OptionAnchorOutputs,
                                 vectors.FeeratePerKw,
                                 htlc.Htlc.Amount,
                                 outPoint,
                                 keyset.SelfRevocationKey,
                                 keyset.SelfDelayedPaymentKey,
                                 Context.ToSelfDelay,
                                 (uint)htlc.CltvExpirey);
            }
            else
            {
               redeemScript = Context.Scripts.GetHtlcReceivedRedeemscript(
                              htlc.Htlc.Expirylocktime,
                              Context.LocalHtlckey,
                              Context.RemoteHtlckey,
                              htlc.Htlc.Rhash,
                              Context.RemoteRevocationKey,
                              Context.OptionAnchorOutputs);

               htlcTransaction = Context.CommitmentTransaction.CreateHtlcSuccessTransaction(
                                 Context.OptionAnchorOutputs,
                                 vectors.FeeratePerKw,
                                 htlc.Htlc.Amount,
                                 outPoint,
                                 keyset.SelfRevocationKey,
                                 keyset.SelfDelayedPaymentKey,
                                 Context.ToSelfDelay);
            }

            var htlcOutput = TransactionHelper.SeriaizeTransaction(Context.TransactionSerializer, Hex.FromString(vectors.HtlcTx[htlcOutputIndex++]));

            var htlcRemoteSignature = Context.CommitmentTransaction.SignInput(
               Context.TransactionSerializer,
               htlcTransaction,
               Context.RemoteHtlcsecretkey,
               inputIndex: 0,
               redeemScript: redeemScript,
               htlc.Htlc.Amount / 1000);

            var expectedHtlcRemoteSignature = Hex.ToString(htlcOutput.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
            Assert.Equal(expectedHtlcRemoteSignature, Hex.ToString(htlcRemoteSignature.GetSpan()));

            var htlcLocalSignature = Context.CommitmentTransaction.SignInput(
               Context.TransactionSerializer,
               htlcTransaction,
               Context.LocalHtlcsecretkey,
               inputIndex: 0,
               redeemScript: redeemScript,
               htlc.Htlc.Amount / 1000);

            var expectedHtlcLocalSignature = Hex.ToString(htlcOutput.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
            Assert.Equal(expectedHtlcLocalSignature, Hex.ToString(htlcLocalSignature.GetSpan()));
         }
      }

      /* BOLT #3:
 *    htlc 5 direction: local->remote
 *    htlc 5 amount_msat: 5000000
 *    htlc 5 expiry: 505
 *    htlc 5 payment_preimage: 0505050505050505050505050505050505050505050505050505050505050505
 *    htlc 6 direction: local->remote
 *    htlc 6 amount_msat: 5000000
 *    htlc 6 expiry: 506
 *    htlc 6 payment_preimage: 0505050505050505050505050505050505050505050505050505050505050505
*/

      public static (List<Htlc>, List<Htlc>) Setup_htlcs_1_5_and_6()
      {
         List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               Amount = 2000000,
               Expirylocktime = 501,
               R = new Preimage(Hex.FromString("0101010101010101010101010101010101010101010101010101010101010101")),
            },

            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               Amount = 5000000,
               Expirylocktime = 505,
               R = new Preimage(Hex.FromString("0505050505050505050505050505050505050505050505050505050505050505")),
            },
            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               Amount = 5000000,
               Expirylocktime = 506,
               R = new Preimage(Hex.FromString("0505050505050505050505050505050505050505050505050505050505050505")),
            },
         };

         foreach (Htlc htlc in htlcs)
         {
            htlc.Rhash = new UInt256(HashGenerator.Sha256(htlc.R));
         }

         List<Htlc>? inverted = InvertHtlcs(htlcs);

         return (htlcs, inverted);
      }

      /* BOLT #3:
     *
     *    htlc 0 direction: remote.local
     *    htlc 0 amount_msat: 1000000
     *    htlc 0 expiry: 500
     *    htlc 0 payment_preimage: 0000000000000000000000000000000000000000000000000000000000000000
     *    htlc 1 direction: remote.local
     *    htlc 1 amount_msat: 2000000
     *    htlc 1 expiry: 501
     *    htlc 1 payment_preimage: 0101010101010101010101010101010101010101010101010101010101010101
     *    htlc 2 direction: local.remote
     *    htlc 2 amount_msat: 2000000
     *    htlc 2 expiry: 502
     *    htlc 2 payment_preimage: 0202020202020202020202020202020202020202020202020202020202020202
     *    htlc 3 direction: local.remote
     *    htlc 3 amount_msat: 3000000
     *    htlc 3 expiry: 503
     *    htlc 3 payment_preimage: 0303030303030303030303030303030303030303030303030303030303030303
     *    htlc 4 direction: remote.local
     *    htlc 4 amount_msat: 4000000
     *    htlc 4 expiry: 504
     *    htlc 4 payment_preimage: 0404040404040404040404040404040404040404040404040404040404040404
     */

      public static (List<Htlc>, List<Htlc>) Setup_htlcs_0_to_4()
      {
         List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               Amount = 1000000,
               Expirylocktime = 500,
               R = new Preimage(Hex.FromString("0000000000000000000000000000000000000000000000000000000000000000")),
            },
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               Amount = 2000000,
               Expirylocktime = 501,
               R = new Preimage(Hex.FromString("0101010101010101010101010101010101010101010101010101010101010101")),
            },
            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               Amount = 2000000,
               Expirylocktime = 502,
               R = new Preimage(Hex.FromString("0202020202020202020202020202020202020202020202020202020202020202")),
            },
            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               Amount = 3000000,
               Expirylocktime = 503,
               R = new Preimage(Hex.FromString("0303030303030303030303030303030303030303030303030303030303030303")),
            },
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               Amount = 4000000,
               Expirylocktime = 504,
               R = new Preimage(Hex.FromString("0404040404040404040404040404040404040404040404040404040404040404")),
            },
         };

         foreach (Htlc htlc in htlcs)
         {
            htlc.Rhash = new UInt256(HashGenerator.Sha256(htlc.R));
         }

         List<Htlc>? inverted = InvertHtlcs(htlcs);

         return (htlcs, inverted);
      }

      /* HTLCs as seen from other side. */

      public static List<Htlc> InvertHtlcs(List<Htlc> htlcs)
      {
         List<Htlc> htlcsinv = new List<Htlc>(htlcs.Count);

         for (int i = 0; i < htlcs.Count; i++)
         {
            Htlc htlc = htlcs[i];

            Htlc inv = new Htlc
            {
               Amount = htlc.Amount,
               Expirylocktime = htlc.Expirylocktime,
               Id = htlc.Id,
               R = htlc.R,
               Rhash = htlc.Rhash,
               State = htlc.State,
            };

            if (inv.State == HtlcState.RcvdAddAckRevocation)
            {
               htlc.State = HtlcState.SentAddAckRevocation;
            }
            else
            {
               Assert.True(inv.State == HtlcState.SentAddAckRevocation);
               htlc.State = HtlcState.RcvdAddAckRevocation;
            }

            htlcsinv.Add(inv);
         }

         return htlcsinv;
      }
   }
}

#pragma warning restore IDE1006 // Naming Styles
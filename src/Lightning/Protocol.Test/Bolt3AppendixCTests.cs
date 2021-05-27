using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization;
using Bitcoin.Primitives.Serialization.Serializers;
using Bitcoin.Primitives.Types;
using Castle.Core.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Protocol.Channels;
using Protocol.Hashing;
using Xunit;
using Block = NBitcoin.Block;
using Transaction = NBitcoin.Transaction;

#pragma warning disable IDE1006 // Naming Styles

namespace Protocol.Test
{
   /// <summary>
   /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
   /// </summary>
   public partial class Bolt3AppendixCTests : IClassFixture<Bolt3AppendixCTestContext>
   {
      public Bolt3AppendixCTestContext Context { get; set; }

      public Bolt3AppendixCTests(Bolt3AppendixCTestContext context)
      {
         Context = context;
      }

      [Theory]
      [ClassData(typeof(Bolt3AppendixCTestData))]
      public void Bolt3AppendixC_CommitmentAndHTLCTransaction(Bolt3AppendixCTestData data)
      {
         Bitcoin.Primitives.Types.Transaction localTransaction = Context.scripts.CreateCommitmenTransaction(
            Context.funding_tx_outpoint,
            Context.funding_amount,
            Context.local_funding_pubkey,
            Context.remote_funding_pubkey,
            LightningScripts.Side.LOCAL,
            Context.to_self_delay,
            Context.keyset,
            data.feerate_per_kw,
            Context.dust_limit,
            data.to_local_msat,
            data.to_remote_msat,
            data.htlcs_0_to_4.htlcs,
            Context.commitment_number,
            Context.cn_obscurer,
            data.option_anchor_outputs,
            LightningScripts.Side.LOCAL);

         Bitcoin.Primitives.Types.Transaction remoteTransaction = Context.scripts.CreateCommitmenTransaction(
            Context.funding_tx_outpoint,
            Context.funding_amount,
            Context.local_funding_pubkey,
            Context.remote_funding_pubkey,
            LightningScripts.Side.REMOTE,
            Context.to_self_delay,
            Context.keyset,
            data.feerate_per_kw,
            Context.dust_limit,
            data.to_local_msat,
            data.to_remote_msat,
            data.htlcs_0_to_4.invertedhtlcs,
            Context.commitment_number,
            Context.cn_obscurer,
            data.option_anchor_outputs,
            LightningScripts.Side.REMOTE);

         localTransaction.Hash = Context.transactionHashCalculator.ComputeHash(localTransaction, 1);
         remoteTransaction.Hash = Context.transactionHashCalculator.ComputeHash(remoteTransaction, 1);

         Assert.Equal(localTransaction.Hash, remoteTransaction.Hash);

         // == helper code==
         var output_commit_tx = TransactionHelper.SeriaizeTransaction(Context.transactionSerializer, StringUtilities.FromHexString(data.output_commit_tx));
         var newtrx = TransactionHelper.ParseToString(localTransaction);
         var curtrx = TransactionHelper.ParseToString(output_commit_tx);

         var remote_signature = Context.scripts.SignCommitmentInput(Context.transactionSerializer, localTransaction, Context.x_remote_funding_privkey, inputIndex: 0, redeemScript: Context.funding_wscript, Context.funding_amount);
         var expected_remote_signature = StringUtilities.ToHexString(output_commit_tx.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
         Assert.Equal(expected_remote_signature, StringUtilities.ToHexString(remote_signature.GetSpan()));

         var local_signature = Context.scripts.SignCommitmentInput(Context.transactionSerializer, localTransaction, Context.local_funding_privkey, inputIndex: 0, redeemScript: Context.funding_wscript, Context.funding_amount);
         var expected_local_signature = StringUtilities.ToHexString(output_commit_tx.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
         Assert.Equal(expected_local_signature, StringUtilities.ToHexString(local_signature.GetSpan()));

         var redeemScript = new Script(OpcodeType.OP_0, Op.GetPushOp(local_signature), Op.GetPushOp(remote_signature), Op.GetPushOp(Context.funding_wscript)).ToWitScript();

         localTransaction.Inputs[0].ScriptWitness = new TransactionWitness
         {
            Components = redeemScript.Pushes.Select(opcode => new TransactionWitnessComponent { RawData = opcode }).ToArray()
         };

         var buffer = new ArrayBufferWriter<byte>();
         Context.transactionSerializer.Serialize(localTransaction, 1, buffer, new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, true)));

         Assert.Equal(data.output_commit_tx, StringUtilities.ToHexString(buffer.WrittenSpan).Substring(2));

         foreach (Htlc htlc in data.htlcs_0_to_4.htlcs)
         {
            // generate and check each htlc sig
         }
      }
   }
}

#pragma warning restore IDE1006 // Naming Styles
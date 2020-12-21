using System;
using NBitcoin;

namespace Protocol.Channels
{
   public class LightningScripts
   {
      /// <summary>
      /// https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#funding-transaction-output
      /// </summary>
      public byte[] CreaateFundingTransactionScript(byte[] pubkey1, byte[] pubkey2)
      {
         var pk1 = new PubKey(pubkey1);
         var pk2 = new PubKey(pubkey2);

         // todo: sort pubkeys lexicographically

         // pub keys must be compressed
         if (!pk1.IsCompressed) throw new ApplicationException();
         if (!pk2.IsCompressed) throw new ApplicationException();

         var fundingTransactionOutput = new Script(
            OpcodeType.OP_2,
            Op.GetPushOp(pk1.ToBytes()),
            Op.GetPushOp(pk2.ToBytes()),
            OpcodeType.OP_2,
            OpcodeType.OP_CHECKMULTISIG
         );

         return fundingTransactionOutput.ToBytes();
      }
   }
}
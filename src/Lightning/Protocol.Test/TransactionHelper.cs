using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Protocol.Test
{
   public class TransactionHelper
   {
      public static string ParseToString(Bitcoin.Primitives.Types.Transaction transaction)
      {
         StringBuilder sb = new StringBuilder();

         sb.AppendLine($"Version={transaction.Version}");
         sb.AppendLine($"LockTime={transaction.LockTime}");
         sb.AppendLine($"Hash={transaction.Hash}");

         foreach (var input in transaction.Inputs)
         {
            sb.AppendLine($"Sequence={input.Sequence}");
            sb.AppendLine($"PreviousOutput={input.PreviousOutput.Index}-{input.PreviousOutput.Hash}");
            sb.AppendLine($"SignatureScript={ (input.SignatureScript == null ? string.Empty : new NBitcoin.Script(input.SignatureScript))}");

            if (input.ScriptWitness?.Components != null)
            {
               NBitcoin.Script witnesScript = new NBitcoin.Script(input.ScriptWitness.Components.Select(p => NBitcoin.Op.GetPushOp(p.RawData)).ToArray());

               sb.AppendLine($"ScriptWitness={witnesScript}");
            }
         }

         foreach (var output in transaction.Outputs)
         {
            sb.AppendLine($"Value={output.Value}");
            sb.AppendLine($"PublicKeyScript={(output.PublicKeyScript == null ? string.Empty : new NBitcoin.Script(output.PublicKeyScript))}");
         }

         return sb.ToString();
      }
   }
}
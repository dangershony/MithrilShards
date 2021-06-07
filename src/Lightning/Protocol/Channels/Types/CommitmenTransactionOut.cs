using System.Collections.Generic;
using Bitcoin.Primitives.Types;

namespace Protocol.Channels.Types
{
   public class CommitmenTransactionOut
   {
      public List<HtlcToOutputMaping> Htlcs { get; set; }
      public Transaction Transaction { get; set; }
   }
}
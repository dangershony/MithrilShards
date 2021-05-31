using System.Collections.Generic;
using Protocol.Channels;

namespace Protocol.Test.bolt3
{
   public class Bolt3AppendixCTestVectors
   {
      public string testName;
      public ulong to_local_msat;
      public ulong to_remote_msat;
      public uint feerate_per_kw;
      public string output_commit_tx;
      public (List<Htlc> htlcs, List<Htlc> invertedhtlcs) htlcs_0_to_4;
      public List<string> HtlcTx;
   }
}
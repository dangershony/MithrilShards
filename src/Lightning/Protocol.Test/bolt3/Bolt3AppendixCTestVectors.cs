using System.Collections.Generic;
using Protocol.Channels;
using Protocol.Channels.Types;

namespace Protocol.Test.bolt3
{
   public class Bolt3AppendixCTestVectors
   {
      public string TestName;
      public ulong ToLocalMsat;
      public ulong ToRemoteMsat;
      public uint FeeratePerKw;
      public string OutputCommitTx;
      public (List<Htlc> htlcs, List<Htlc> invertedhtlcs) Htlcs;
      public List<string> HtlcTx;
   }
}
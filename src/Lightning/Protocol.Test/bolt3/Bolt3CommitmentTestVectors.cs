using System.Collections.Generic;
using Bitcoin.Primitives.Fundamental;
using Protocol.Channels;
using Protocol.Channels.Types;

namespace Protocol.Test.bolt3
{
   public class Bolt3CommitmentTestVectors
   {
      public string TestName;
      public bool LocalAnchorOutputs;
      public bool RemoteAnchorOutputs;
      public MiliSatoshis ToLocalMsat;
      public MiliSatoshis ToRemoteMsat;
      public Satoshis FeeratePerKw;
      public string OutputCommitTx;
      public (List<Htlc> htlcs, List<Htlc> invertedhtlcs) Htlcs;
      public List<string> HtlcTx;
   }
}
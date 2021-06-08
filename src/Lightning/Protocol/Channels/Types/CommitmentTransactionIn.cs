using System.Collections.Generic;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Types;

namespace Protocol.Channels.Types
{
   public class CommitmentTransactionIn
   {
      public OutPoint FundingTxout { get; set; }
      public Satoshis Funding { get; set; }
      public PublicKey LocalFundingKey { get; set; }
      public PublicKey RemoteFundingKey { get; set; }
      public ChannelSide Opener { get; set; }
      public ushort ToSelfDelay { get; set; }
      public Keyset Keyset { get; set; }
      public Satoshis FeeratePerKw { get; set; }
      public Satoshis DustLimitSatoshis { get; set; }
      public MiliSatoshis SelfPayMsat { get; set; }
      public MiliSatoshis OtherPayMsat { get; set; }
      public List<Htlc> Htlcs { get; set; }
      public ulong CommitmentNumber { get; set; }
      public ulong CnObscurer { get; set; }
      public bool OptionAnchorOutputs { get; set; }
      public ChannelSide Side { get; set; }
   }
}
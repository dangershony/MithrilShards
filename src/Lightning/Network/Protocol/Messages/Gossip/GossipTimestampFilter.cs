using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages.Types;

namespace Network.Protocol.Messages.Gossip
{
   [NetworkMessage(COMMAND)]
   public class GossipTimestampFilter : BaseMessage
   {
      private const string COMMAND = "265";
      public override string Command => COMMAND;

      public ChainHash? ChainHash { get; set; }
      
      public uint FirstTimestamp { get; set; }

      public uint TimestampRange { get; set; }
   }
}
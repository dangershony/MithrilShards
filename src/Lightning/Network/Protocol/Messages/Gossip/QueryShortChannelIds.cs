
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages.Types;

namespace Network.Protocol.Messages.Gossip
{
   [NetworkMessage(COMMAND)]
   public class QueryShortChannelIds : BaseMessage
   {
      private const string COMMAND = "261";
      public override string Command => COMMAND;
      
      public ChainHash ChainHash { get; set; }
      
      public ushort Len { get; set; }

      public byte[] EncodedShortIds { get; set; }
   }
}
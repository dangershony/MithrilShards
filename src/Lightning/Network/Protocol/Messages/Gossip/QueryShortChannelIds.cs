
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages.Types;

namespace Network.Protocol.Messages.Gossip
{
   [NetworkMessage(COMMAND)]
   public class QueryShortChannelIds : BaseMessage
   {
      private const string COMMAND = "261";

      public QueryShortChannelIds(ChainHash chainHash, ushort len, byte[] encodedShortIds)
      {
         ChainHash = chainHash;
         Len = len;
         EncodedShortIds = encodedShortIds;
      }

      public override string Command => COMMAND;
      
      public ChainHash ChainHash { get; set; }
      
      public ushort Len { get; set; }

      public byte[] EncodedShortIds { get; set; }
   }
}
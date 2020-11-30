using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public class PongMessage : BaseMessage
   {
      public const ushort MAX_BYTES_LEN = 65531;
      
      private const string COMMAND = "19";
      public override string Command => COMMAND;

      public ushort BytesLen { get; set; }

      public byte[]? Ignored { get; set; }
   }
}
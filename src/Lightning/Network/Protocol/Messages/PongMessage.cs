using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public class PongMessage : BaseMessage
   {
      private const string COMMAND = "19";
      public override string Command => COMMAND;

      public ushort BytesLen { get; set; }

      public byte Ignored { get; set; } = 0x00;
   }
}
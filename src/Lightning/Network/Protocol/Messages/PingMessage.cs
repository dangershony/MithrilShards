using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public class PingMessage : BaseMessage
   {
      private const string COMMAND = "18";
      
      public override string Command => COMMAND;

      public ushort NumPongBytes { get; set; }

      public ushort BytesLen { get; set; }

      public byte Ignored { get; set; } = 0x00;
   }
}
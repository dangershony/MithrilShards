using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public class ErrorMessage : BaseMessage
   {
      private const string COMMAND = "17";

      public ErrorMessage() => ChannelId = new byte[0];

      public override string Command => COMMAND;
         
      public byte[] ChannelId { get; set; }

      public ushort Len { get; set; }
      
      public byte[]? Data { get; set; }
   }
}
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public class PingMessage : BaseMessage
   {
      public const ushort MAX_BYTES_LEN = 65531;

      public PingMessage()
      { }
      
      public PingMessage(byte[] ignored)
      {
         BytesLen = (ushort) ignored.Length;
         Ignored = ignored;
      }
      
      public PingMessage(ushort bytesLen)
      {
         Ignored = new byte[bytesLen];
         BytesLen = bytesLen;
         NumPongBytes = (ushort) (MAX_BYTES_LEN - bytesLen);
      }

      private const string COMMAND = "18";
      
      public override string Command => COMMAND;

      public ushort NumPongBytes { get; set; }

      public ushort BytesLen { get; set; }

      public byte[]? Ignored { get; set; }
   }
}
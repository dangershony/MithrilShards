namespace Network.Protocol.Messages
{
   public class PingMessage : BaseMessage
   {
      private const string COMMAND = "18";
      
      public override string Command => COMMAND;
      
      //public ushort num_pong_bytes 
   }
}
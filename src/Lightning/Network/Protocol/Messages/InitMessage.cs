using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public sealed class InitMessage : BaseMessage
   {
      private const string COMMAND = "16";

      public override string Command => COMMAND;

      public byte[] GlobalFeatures { get; set; }
      public byte[] Features { get; set; }
   }
}
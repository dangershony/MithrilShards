using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public sealed class InitMessage : INetworkMessage
   {
      private const string COMMAND = "16";
      string INetworkMessage.Command => COMMAND;

      public byte[] GlobalFeatures { get; set; }
      public byte[] Features { get; set; }
      public byte[] Tlvs { get; set; }
   }
}
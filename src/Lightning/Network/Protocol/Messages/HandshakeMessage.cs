using System.Buffers;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   [NetworkMessage(COMMAND)]
   public sealed class HandshakeMessage : INetworkMessage
   {
      private const string COMMAND = "-1";
      string INetworkMessage.Command => COMMAND;

      public ReadOnlySequence<byte> Payload { get; set; }
   }
}
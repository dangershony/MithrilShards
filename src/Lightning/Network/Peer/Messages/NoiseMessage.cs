using System.Buffers;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Peer.Messages
{
   [NetworkMessage(COMMAND)]
   public sealed class NoiseMessage : INetworkMessage
   {
      private const string COMMAND = "-1";
      string INetworkMessage.Command => COMMAND;

      public ReadOnlySequence<byte> Payload { get; set; }
   }
}
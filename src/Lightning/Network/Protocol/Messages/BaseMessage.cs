using MithrilShards.Core.Network.Protocol;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Messages
{
   public abstract class BaseMessage : INetworkMessage
   {
      public abstract string Command { get; }

      public TlVStream? Extension { get; set; }
   }
}
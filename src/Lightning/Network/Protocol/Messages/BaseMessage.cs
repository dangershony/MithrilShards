using System.Buffers;
using System.Collections.Generic;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   public abstract class BaseMessage : INetworkMessage
   {
      public Dictionary<ulong, TlvRecord> Extension { get; set; } = new Dictionary<ulong, TlvRecord>();
      public abstract string Command { get; }
   }
}
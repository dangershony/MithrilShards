using System.Buffers;
using System.Collections.Generic;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Types;

namespace Network.Protocol.Messages
{
   public abstract class BaseMessage : INetworkMessage
   {
      //public Dictionary<ulong, TlvRecord> Extension { get; set; } = new Dictionary<ulong, TlvRecord>();
      public abstract string Command { get; }

      public TlVStream Extension { get; set; }
   }
}
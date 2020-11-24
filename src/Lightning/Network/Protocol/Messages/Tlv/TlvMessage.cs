using System.Buffers;
using System.Collections.Generic;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol.Messages
{
   public class TlvMessage
   {
      public Dictionary<ulong, TlvRecord> Messages { get; set; } = new Dictionary<ulong, TlvRecord>();
   }

   public abstract class TlvRecord
   {
      public ulong Type { get; set; }
      public ulong Size { get; set; }
      public ReadOnlySequence<byte> Value { get; set; }
   }
}
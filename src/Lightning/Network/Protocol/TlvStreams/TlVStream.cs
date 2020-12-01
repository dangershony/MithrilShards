using System.Collections.Generic;

namespace Network.Protocol.TlvStreams
{
   public class TlVStream
   {
      public List<TlvRecord> Records { get; set; } = new List<TlvRecord>();
   }
}
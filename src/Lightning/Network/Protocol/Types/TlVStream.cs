using System;
using System.Collections.Generic;
using System.Text;

namespace Network.Protocol.Types
{
   public class TlVStream
   {
      public List<TlvRecord> Records { get; set; } = new List<TlvRecord>();
   }
}
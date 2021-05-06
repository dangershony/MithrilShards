using System;
using System.Collections.Generic;
using System.Text;

namespace Protocol.Channels
{
   public enum InboundHtlcRemovalReason
   {
      FailRelay,
      FailMalformed,
      Fulfill,
   }
}
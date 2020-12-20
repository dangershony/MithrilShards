using System;
using System.Collections.Generic;
using System.Text;
using Network.Protocol.Messages.Types;

namespace Protocol.Channels
{
   public enum InboundHtlcRemovalReason
   {
      FailRelay,
      FailMalformed,
      Fulfill,
   }
}
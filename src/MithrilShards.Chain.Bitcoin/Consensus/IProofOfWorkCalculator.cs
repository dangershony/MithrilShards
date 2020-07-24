﻿using System;
using System.Collections.Generic;
using System.Text;
using MithrilShards.Chain.Bitcoin.Protocol.Types;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// Methods to compute required work (PoW)
   /// </summary>
   public interface IProofOfWorkCalculator
   {
      /// <summary>
      /// Gets the next work required to .
      /// </summary>
      /// <param name="previousHeaderNode">The previous header node.</param>
      /// <param name="header">The header.</param>
      /// <returns></returns>
      uint GetNextWorkRequired(HeaderNode previousHeaderNode, BlockHeader header);

      bool CheckProofOfWork(BlockHeader header);
   }
}

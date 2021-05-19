using System.Collections.Generic;
using MithrilShards.Core.Utils;

namespace Network.Settings
{
   public enum SupportedChains
   {
      Bitcoin = 0
   }

   public static class ChainHashes
   {
      public static readonly Dictionary<SupportedChains, byte[]> SupportedChainHashes = new Dictionary<SupportedChains, byte[]>
      {
         {SupportedChains.Bitcoin, BITCOIN_HEX_CHAIN_HASH.ToByteArray()}
      };

      public const string BITCOIN_HEX_CHAIN_HASH = "6fe28c0ab6f1b372c1a6a246ae63f74f931e8365e15a089c68d6190000000000";
   }
}
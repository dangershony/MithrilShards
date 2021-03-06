using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MithrilShards.Core.Utils
{
    public static class StringUtilities
    {
        public static byte[] ToByteArray(this string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
			
            int startIndex = hex.ToLower().StartsWith("0x") ? 2 : 0;
			
            return Enumerable.Range(startIndex, hex.Length - startIndex)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
        
        public static string ToHexString(this IEnumerable<byte> arr)
        {
           var sb = new StringBuilder();
           sb.Append("0x");
           foreach (byte b in arr)
              sb.Append(b.ToString("X2"));

           return sb.ToString().ToLower();
        }
    }
}
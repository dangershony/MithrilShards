using System;
using System.Linq;

namespace Network.Test.Protocol.Transport.Noise
{
    public static class StringUtilities
    {
        public static byte[] ToByteArray(this string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
			
            var startIndex = hex.ToLower().StartsWith("0x") ? 2 : 0;
			
            return Enumerable.Range(startIndex, hex.Length - startIndex)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}
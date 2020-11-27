using System;
using System.Linq;
using System.Net;
using MithrilShards.Core.DataTypes;

namespace Network
{
   public class LightningEndpoint
   {
      public EndPoint? EndPoint { get; set; }
      public string? NodeId { get; set; }

      public byte[] NodePubKey { get; set; }

      public override string ToString()
      {
         return $@"{NodeId}@{EndPoint}";
      }

      public static bool TryParse(string s, out LightningEndpoint result)
      {
         return TryParse(s.AsSpan(), out result);
      }

      public static bool TryParse(ReadOnlySpan<char> span, out LightningEndpoint result)
      {
         try
         {
            result = Parse(span);
            return true;
         }
         catch (Exception e)
         {
         }

         result = null;
         return false;
      }

      public static LightningEndpoint Parse(string s)
      {
         if (s == null)
         {
            throw new ArgumentNullException(nameof(s));
         }

         return Parse(s.AsSpan());
      }

      public static LightningEndpoint Parse(ReadOnlySpan<char> span)
      {
         string nodeId = span.Slice(0, span.IndexOf("@"))
            .ToString(); 
         
         return new LightningEndpoint
         {
            NodeId = nodeId, // todo: do validation on this
            NodePubKey = ParseNodeId(nodeId), 
            EndPoint = IPEndPoint.Parse(span.Slice(span.IndexOf("@") + 1))
         };
      }
      
      //TODO David move logic to utilities
      private static byte[] ParseNodeId(string nodeId)
      {
         if (string.IsNullOrEmpty(nodeId)) 
            throw new ArgumentException(nameof(nodeId));
			
         int startIndex = nodeId.ToLower().StartsWith("0x") ? 2 : 0;
			
         return Enumerable.Range(startIndex, nodeId.Length - startIndex)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(nodeId.Substring(x, 2), 16))
            .ToArray();
      }
   }
}
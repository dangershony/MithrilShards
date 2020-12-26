using System;
using System.Linq;
using System.Net;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Utils;

namespace Network
{
   public class LightningEndpoint
   {
      public EndPoint? EndPoint { get; set; }
      public string? NodeId { get; set; }

      public byte[] NodePubKey { get; set; } = new byte[0];

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
         catch
         {
            result = null!;
            return false;
         }
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
            NodeId = nodeId, // todo: add validation on this
            NodePubKey = nodeId.ToByteArray(), 
            EndPoint = IPEndPoint.Parse(span.Slice(span.IndexOf("@") + 1))
         };
      }
   }
}
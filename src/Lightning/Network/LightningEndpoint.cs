using System;
using System.Net;
using MithrilShards.Core.DataTypes;

namespace Network
{
   public class LightningEndpoint
   {
      public EndPoint EndPoint { get; set; }
      public string NodeId { get; set; }

      public override string ToString()
      {
         return $@"{this.NodeId}@{this.EndPoint}";
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
         return new LightningEndpoint
         {
            NodeId = span.Slice(0, span.IndexOf("@")).ToString(), // todo: do validation on this
            EndPoint = IPEndPoint.Parse(span.Slice(span.IndexOf("@") + 1))
         };
      }
   }
}
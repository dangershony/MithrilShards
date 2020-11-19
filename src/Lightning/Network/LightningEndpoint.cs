using System;
using System.Buffers;
using System.Net;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Example.Protocol.Messages
{
   public class LightningEndpoint
   {
      public EndPoint EndPoint { get; set; }
      public UInt256 NodePubkey { get; set; }

      public override string ToString()
      {
         return $@"{this.NodePubkey}@{this.EndPoint}";
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
            NodePubkey = UInt256.Parse(span.Slice(0, 32).ToString()),
            EndPoint = IPEndPoint.Parse(span.Slice(33))
         };
      }
   }
}
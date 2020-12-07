using System;
using System.Linq;
using System.Security.Cryptography;

namespace NoiseProtocol
{
   public class HashFunction : IHashFunction
   {
      public void Hash(Span<byte> span, Span<byte> output)
      {
         using var sha256 = SHA256.Create();
         sha256.ComputeHash(span.ToArray());
         sha256.Hash.AsSpan()
            .CopyTo(output);
      }

      public void Hash(Span<byte> first, Span<byte> second, Span<byte> output)
      {
         var array = new byte[first.Length + second.Length];
         
         first.CopyTo(array.AsSpan(0,first.Length));
         second.CopyTo(array.AsSpan(first.Length,second.Length));

         using var sha256 = SHA256.Create();
         sha256.ComputeHash(array);
         sha256.Hash.AsSpan()
            .CopyTo(output);
      }
      
      public void Hash(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, ReadOnlySpan<byte> third, Span<byte> output)
      {
         var array = new byte[first.Length + second.Length + third.Length];
         
         first.CopyTo(array.AsSpan(0,first.Length));
         second.CopyTo(array.AsSpan(first.Length, second.Length));
         third.CopyTo(array.AsSpan(second.Length, third.Length));

         using var sha256 = SHA256.Create();
         sha256.ComputeHash(array);
         sha256.Hash.AsSpan()
            .CopyTo(output);
      }
   }
}
﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Protocol.Hashing
{
   public static partial class HashGenerator
   {
      public static ReadOnlySpan<byte> Sha256(ReadOnlySpan<byte> data)
      {
         using var sha = new SHA256Managed();
         Span<byte> result = new byte[32];

         if (!sha.TryComputeHash(data, result, out _)) ThrowHashGeneratorException($"Failed to perform {nameof(Sha256)}");

         return result;
      }

      public static ReadOnlySpan<byte> DoubleSha256(ReadOnlySpan<byte> data)
      {
         using var sha = new SHA256Managed();
         Span<byte> result = new byte[32];

         if (!sha.TryComputeHash(data, result, out _) || !sha.TryComputeHash(result, result, out _))
         {
            ThrowHashGeneratorException($"Failed to perform {nameof(DoubleSha256)}");
         }

         return result;
      }

      public static ReadOnlySpan<byte> DoubleSha512(ReadOnlySpan<byte> data)
      {
         using var sha = new SHA512Managed();
         Span<byte> result = new byte[64];
         sha.TryComputeHash(data, result, out _);
         sha.TryComputeHash(result, result, out _);
         return result.Slice(0, 32);
      }

      [DoesNotReturn]
      public static void ThrowHashGeneratorException(string message)
      {
         throw new HashGeneratorException(message);
      }
   }
}
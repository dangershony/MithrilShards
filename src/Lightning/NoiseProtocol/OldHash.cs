using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace NoiseProtocol
{
   public class OldHash
   {
      private readonly byte[] _state = new byte[104];
      private int _currentStateLength = 0;
      private bool _disposed;

      public OldHash() => Reset();

      public int HashLen => 32;
      public int BlockLen => 64;

      public void AppendData(ReadOnlySpan<byte> data)
      {
         if (data.IsEmpty) return;

         data.CopyTo(_state.AsSpan(_currentStateLength, data.Length));

         _currentStateLength += data.Length;
      }

      public void GetHashAndReset(Span<byte> hash)
      {
         Debug.Assert(hash.Length == HashLen);

         using (var sha256 = SHA256.Create())
         {
            sha256.ComputeHash(_state.AsSpan(0, _currentStateLength)
               .ToArray());
            sha256.Hash.AsSpan()
               .CopyTo(hash);
         }

         Reset();
      }

      private void Reset()
      {
         _currentStateLength = 0;
      }

      public void Dispose()
      {
         if (!_disposed)
         {
            _disposed = true;
         }
      }
   }
}
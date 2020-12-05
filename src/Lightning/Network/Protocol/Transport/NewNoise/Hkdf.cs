using System;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport.NewNoise
{
   public class Hkdf : IHkdf, IDisposable
   {
      private Hkdf<Sha256> _hkdf = new Hkdf<Sha256>();
      public void ExtractAndExpand(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> inputKeyMaterial,
         Span<byte> output)
      {
         _hkdf.ExtractAndExpand2(chainingKey,inputKeyMaterial,output);
      }

      public void Dispose() => _hkdf.Dispose();
   }
}
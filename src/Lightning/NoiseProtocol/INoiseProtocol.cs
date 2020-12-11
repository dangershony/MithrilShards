using System;
using System.Buffers;

namespace NoiseProtocol
{
   public interface INoiseProtocol
   {
      void InitHandShake();

      void StartNewHandshake(byte[] remotePublicKey, IBufferWriter<byte> output);

      void ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest, IBufferWriter<byte> output);

      void CompleteHandshake(ReadOnlySpan<byte> handshakeRequest);
   }
}
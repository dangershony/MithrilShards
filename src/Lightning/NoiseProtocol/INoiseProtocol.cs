using System;
using System.Buffers;

namespace NoiseProtocol
{
   public interface INoiseProtocol
   {
      void InitHandShake();

      void StartNewInitiatorHandshake(byte[] remotePublicKey, IBufferWriter<byte> output);

      void ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest, IBufferWriter<byte> output);

      void CompleteResponderHandshake(ReadOnlySpan<byte> handshakeRequest);

      INoiseMessageTransformer GetMessageTransformer();
   }
}
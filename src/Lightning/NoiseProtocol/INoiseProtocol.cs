using System;
using System.Buffers;

namespace NoiseProtocol
{
   public interface INoiseProtocol
   {
      void InitHandShake(byte[] privateKey);

      void StartNewInitiatorHandshake(byte[] remotePublicKey, IBufferWriter<byte> output);

      void ProcessHandshakeRequest(ReadOnlySequence<byte> handshakeRequest, IBufferWriter<byte> output);

      void CompleteResponderHandshake(ReadOnlySequence<byte> handshakeRequest);

      INoiseMessageTransformer GetMessageTransformer();
   }
}
using System;

namespace Network.Protocol.Transport.NewNoise
{
   public interface INoiseProtocol
   {
      void SetPrivateKey(byte[] privateKey);
      
      void InitHandShake();

      ReadOnlySpan<byte> StartNewHandshake(byte[] remotePublicKey);

      ReadOnlySpan<byte> ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest);

      void CompleteHandshake(ReadOnlySpan<byte> handshakeRequest);
   }
}
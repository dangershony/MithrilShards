namespace Network.Protocol.Transport.Noise
{
   public interface IHandshakeStateFactory
   {
      IHandshakeState CreateLightningNetworkHandshakeState(byte[] privateKey, byte[]? remotePublicKey);
   }
}
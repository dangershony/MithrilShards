namespace Network.Protocol.Transport.Noise
{
   public class HandshakeStateFactory : IHandshakeStateFactory
   {
      //TODO David add unit tests
      public IHandshakeState CreateLightningNetworkHandshakeState(byte[] privateKey, byte[]? remotePublicKey)
      {
         Protocol protocol = Protocol.Parse(LightningNetworkConfig.PROTOCOL_NAME);

         protocol.VersionPrefix = LightningNetworkConfig.NoiseProtocolVersionPrefix;

         return protocol.Create(remotePublicKey != null,
            LightningNetworkConfig.ProlugeByteArray(),
            privateKey,
            remotePublicKey!);
      }
   }
}
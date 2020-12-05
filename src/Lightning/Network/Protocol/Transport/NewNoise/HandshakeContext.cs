namespace Network.Protocol.Transport.NewNoise
{
   public class HandshakeContext
   {
      public HandshakeContext()
      {
         _h = new byte[32];
         _ck = new byte[32];
         _sk = new byte[32];
         _rk = new byte[32];
         _ephemeralPrivateKey = new byte[0];
      }

      public byte[] _h;
      public byte[] _ck;
      public byte[] _ephemeralPrivateKey, _sk,_rk;

      byte[]? _privateKey;
      
      byte[] _remotePublicKey = new byte[0];
   }
}
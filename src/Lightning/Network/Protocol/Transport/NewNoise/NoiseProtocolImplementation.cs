using System;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport.NewNoise
{
   public class NoiseProtocolImplementation : INoiseProtocol
   {
      readonly IEllipticCurveActions _curveActions;
      readonly IHkdf _hkdf;
      readonly IAeadConstruction _aeadConstruction;
      readonly IHash _hash;
      readonly IHasher _hasher;
      readonly IKeyGenerator _keyGenerator;

      public byte[] _h;
      public byte[] _ck;
      public byte[] _ephemeralPrivateKey, _sk,_rk;

      byte[]? _privateKey;
      
      byte[] _remotePublicKey = new byte[0];
      
      public NoiseProtocolImplementation()
      {
         _curveActions = new EllipticCurveActions();
         _hkdf = new Hkdf();
         _aeadConstruction = new AeadConstruction();
         _hash = new Sha256();
         _keyGenerator = new KeyGenerator();
         _hasher = new Hasher();

         _h = new byte[32];
         _ck = new byte[32];
         _sk = new byte[32];
         _rk = new byte[32];
         _ephemeralPrivateKey = new byte[0];
      }

      public NoiseProtocolImplementation(IEllipticCurveActions curveActions, IHkdf hkdf, IAeadConstruction aeadConstruction, IHash hash, IKeyGenerator keyGenerator, IHasher hasher)
      {
         _curveActions = curveActions;
         _hkdf = hkdf;
         _aeadConstruction = aeadConstruction;
         _hash = hash;
         _keyGenerator = keyGenerator;
         _hasher = hasher;
      }

      public void SetPrivateKey(byte[] privateKey)
      {
         _privateKey = privateKey;
      }

      public void InitHandShake()
      {
         _hasher.Hash(LightningNetworkConfig.ProtocolNameByteArray(), _ck);

         _hasher.Hash(_ck, LightningNetworkConfig.ProlugeByteArray(), _h);
      }

      public ReadOnlySpan<byte> StartNewHandshake(byte[] remotePublicKey)
      {
         _hasher.Hash(_h, remotePublicKey, _h);

         _remotePublicKey = new byte[33];
         
         remotePublicKey.CopyTo(_remotePublicKey.AsSpan());
         
         return GenerateLocalEphemeralAndProcessRemotePublicKey(remotePublicKey);
      }
      
      public ReadOnlySpan<byte> ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest)
      {
         if (_remotePublicKey.Length == 0)
         {
            //responder act one
            _hasher.Hash(_h,_keyGenerator.GetPublicKey(_privateKey).ToArray(),_h);

            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(_privateKey, handshakeRequest);

            //responder act two

            return GenerateLocalEphemeralAndProcessRemotePublicKey(re.ToArray());
         }
         else
         {
            //act two initiator
            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(_ephemeralPrivateKey, handshakeRequest);

            return CompleteInitiatorHandshake(re);
         }
      }

      public void CompleteHandshake(ReadOnlySpan<byte> handshakeRequest)
      {
         if (handshakeRequest[0] != 0)
            throw new ArgumentException(); // version byte
         
         var cipher = handshakeRequest.Slice(1, 49);

         _remotePublicKey = new byte[33];
         
         _aeadConstruction.DecryptWithAd(_h, cipher, _remotePublicKey);
         
         _hasher.Hash(_h,cipher.ToArray(),_h);
         
         var se = _curveActions.Multiply(_ephemeralPrivateKey, _remotePublicKey);
         
         var ckAndTempKey = ExtractNextKeys(se);
         
         var plainText = new byte[16];
         _aeadConstruction.DecryptWithAd(_h, handshakeRequest.Slice(50), plainText);
         
         _hkdf.ExtractAndExpand(_ck, new byte[0].AsSpan(), ckAndTempKey);
            
         ckAndTempKey.AsSpan(0, 32).CopyTo(_sk);
         ckAndTempKey.AsSpan(32).CopyTo(_rk);
      }
      
      private ReadOnlySpan<byte> CompleteInitiatorHandshake(ReadOnlySpan<byte> re)
      {
         //act three initiator
         var outputText = new byte[66];
         
         var cipher = outputText.AsSpan(1, 49);

         _aeadConstruction.EncryptWithAd(_h, _keyGenerator.GetPublicKey(_privateKey).ToArray(), cipher);

         _hasher.Hash(_h, cipher, _h);

         var se = _curveActions.Multiply(_privateKey, re);

         var ckAndTempKey = ExtractNextKeys(se);

         _aeadConstruction.EncryptWithAd(_h, new byte[0], outputText.AsSpan(50));

         _hkdf.ExtractAndExpand(_ck, new byte[0].AsSpan(), ckAndTempKey);

         ckAndTempKey.AsSpan(0, 32).CopyTo(_sk);
         ckAndTempKey.AsSpan(32).CopyTo(_rk);

         return outputText;
      }

      private ReadOnlySpan<byte> HandleReceivedHandshakeRequest(ReadOnlySpan<byte> privateKey,ReadOnlySpan<byte> handshakeRequest)
      {
         if (handshakeRequest[0] != 0)
            throw new ArgumentException(); // version byte

         var re = handshakeRequest.Slice(1, 33);

         _hasher.Hash(_h, re.ToArray(), _h);

         var secret = _curveActions.Multiply(privateKey.ToArray(), re); //es and ee

         ExtractNextKeys(secret);
         
         var c = handshakeRequest.Slice(34);
         var plainText = new byte[16];
         _aeadConstruction.DecryptWithAd(_h, c, plainText);

         _hasher.Hash(_h, c.ToArray(), _h);
         return re;
      }

      private ReadOnlySpan<byte> GenerateLocalEphemeralAndProcessRemotePublicKey(byte[] remotePublicKey)
      {
         var outputText = new byte[50];
         
         var e = _keyGenerator.GenerateKey();

         _ephemeralPrivateKey = e.ToArray();

         var ephemeralPublicKey = outputText.AsSpan(1, 33);

         _keyGenerator.GetPublicKey(_ephemeralPrivateKey)
            .CopyTo(ephemeralPublicKey);

         _hasher.Hash(_h, ephemeralPublicKey, _h);

         var es = _curveActions.Multiply(_ephemeralPrivateKey, remotePublicKey);

         ExtractNextKeys(es);
         
         _aeadConstruction.EncryptWithAd(_h, null, outputText.AsSpan(34));

         _hasher.Hash(_h, outputText.AsSpan(34), _h);

         return outputText;
      }
      
      private byte[] ExtractNextKeys(ReadOnlySpan<byte> se)
      {
         var ckAndTempKey = new byte[64];
      
         _hkdf.ExtractAndExpand(_ck, se, ckAndTempKey);
      
         ckAndTempKey.AsSpan(0, 32).CopyTo(_ck);
      
         _aeadConstruction.SetKey(ckAndTempKey.AsSpan(32)); //temp key 3
               
         return ckAndTempKey;
      }
   }
}
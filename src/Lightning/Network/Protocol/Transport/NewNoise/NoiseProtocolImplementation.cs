using System;
using Network.Protocol.Transport.Noise;

namespace Network.Protocol.Transport.NewNoise
{
   public class NoiseProtocolImplementation : INoiseProtocol, IDisposable
   {
      readonly IEllipticCurveActions _curveActions;
      readonly IHkdf _hkdf;
      readonly IAeadConstruction _aeadConstruction;
      readonly IHasher _hasher;
      readonly IKeyGenerator _keyGenerator;

      HandshakeContext _handshakeContext;
      
      public NoiseProtocolImplementation(byte[] privateKey)
      {
         _handshakeContext = new HandshakeContext(privateKey);
         _curveActions = new EllipticCurveActions();
         _hkdf = new Hkdf();
         _aeadConstruction = new AeadConstruction();
         _keyGenerator = new KeyGenerator();
         _hasher = new Hasher();
      }

      public NoiseProtocolImplementation(IEllipticCurveActions curveActions, IHkdf hkdf, 
         IAeadConstruction aeadConstruction, IHash hash, IKeyGenerator keyGenerator, IHasher hasher,
         byte[] privateKey)
      {
         _curveActions = curveActions;
         _hkdf = hkdf;
         _aeadConstruction = aeadConstruction;
         _keyGenerator = keyGenerator;
         _hasher = hasher;
         _handshakeContext = new HandshakeContext(privateKey);
      }

      public void InitHandShake()
      {
         _hasher.Hash(LightningNetworkConfig.ProtocolNameByteArray(), _handshakeContext.ChainingKey);

         _hasher.Hash(_handshakeContext.ChainingKey, LightningNetworkConfig.ProlugeByteArray(), _handshakeContext.Hash);
      }

      public ReadOnlySpan<byte> StartNewHandshake(byte[] remotePublicKey)
      {
         _hasher.Hash(_handshakeContext.Hash, remotePublicKey, _handshakeContext.Hash);

         _handshakeContext.SetRemotePublicKey(remotePublicKey);
         
         return GenerateLocalEphemeralAndProcessRemotePublicKey(remotePublicKey);
      }
      
      public ReadOnlySpan<byte> ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest)
      {
         if (!_handshakeContext.HasRemotePublic)
         {
            //responder act one
            _hasher.Hash(_handshakeContext.Hash,_keyGenerator.GetPublicKey(_handshakeContext.PrivateKey).ToArray(),_handshakeContext.Hash);

            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(_handshakeContext.PrivateKey, handshakeRequest);

            //responder act two

            return GenerateLocalEphemeralAndProcessRemotePublicKey(re.ToArray());
         }
         else
         {
            //act two initiator
            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(_handshakeContext.EphemeralPrivateKey, handshakeRequest);

            return CompleteInitiatorHandshake(re);
         }
      }

      public void CompleteHandshake(ReadOnlySpan<byte> handshakeRequest)
      {
         if (handshakeRequest[0] != 0)
            throw new ArgumentException(); // version byte
         
         var cipher = handshakeRequest.Slice(1, 49);

         _handshakeContext.RemotePublicKey = new byte[33];
         
         _aeadConstruction.DecryptWithAd(_handshakeContext.Hash, cipher, _handshakeContext.RemotePublicKey);
         
         _hasher.Hash(_handshakeContext.Hash,cipher.ToArray(),_handshakeContext.Hash);
         
         var se = _curveActions.Multiply(_handshakeContext.EphemeralPrivateKey, _handshakeContext.RemotePublicKey);
         
         ExtractNextKeys(se);
         
         var plainText = new byte[16];
         _aeadConstruction.DecryptWithAd(_handshakeContext.Hash, handshakeRequest.Slice(50), plainText);
         
         ExtractFinalChannelKeys();
      }

      void ExtractFinalChannelKeys()
      {
         var ckAndTempKey = new byte[64];
         
         _hkdf.ExtractAndExpand(_handshakeContext.ChainingKey, new byte[0].AsSpan(), ckAndTempKey);

         ckAndTempKey.AsSpan(0, 32).CopyTo(_handshakeContext.Sk);
         ckAndTempKey.AsSpan(32).CopyTo(_handshakeContext.Rk);
      }

      private ReadOnlySpan<byte> CompleteInitiatorHandshake(ReadOnlySpan<byte> re)
      {
         //act three initiator
         var outputText = new byte[66];
         
         var cipher = outputText.AsSpan(1, 49);

         _aeadConstruction.EncryptWithAd(_handshakeContext.Hash, _keyGenerator.GetPublicKey(_handshakeContext.PrivateKey).ToArray(), cipher);

         _hasher.Hash(_handshakeContext.Hash, cipher, _handshakeContext.Hash);

         var se = _curveActions.Multiply(_handshakeContext.PrivateKey, re);

         ExtractNextKeys(se);

         _aeadConstruction.EncryptWithAd(_handshakeContext.Hash, new byte[0], outputText.AsSpan(50));

         ExtractFinalChannelKeys();

         return outputText;
      }

      private ReadOnlySpan<byte> HandleReceivedHandshakeRequest(ReadOnlySpan<byte> privateKey,ReadOnlySpan<byte> handshakeRequest)
      {
         if (handshakeRequest[0] != 0)
            throw new ArgumentException(); // version byte

         var re = handshakeRequest.Slice(1, 33);

         _hasher.Hash(_handshakeContext.Hash, re.ToArray(), _handshakeContext.Hash);

         var secret = _curveActions.Multiply(privateKey.ToArray(), re); //es and ee

         ExtractNextKeys(secret);
         
         var c = handshakeRequest.Slice(34);
         var plainText = new byte[16];
         _aeadConstruction.DecryptWithAd(_handshakeContext.Hash, c, plainText);

         _hasher.Hash(_handshakeContext.Hash, c.ToArray(), _handshakeContext.Hash);
         return re;
      }

      private ReadOnlySpan<byte> GenerateLocalEphemeralAndProcessRemotePublicKey(byte[] publicKey)
      {
         var outputText = new byte[50];
         
         _handshakeContext.EphemeralPrivateKey = _keyGenerator.GenerateKey();

         var ephemeralPublicKey = outputText.AsSpan(1, 33);

         _keyGenerator.GetPublicKey(_handshakeContext.EphemeralPrivateKey)
            .CopyTo(ephemeralPublicKey);

         _hasher.Hash(_handshakeContext.Hash, ephemeralPublicKey, _handshakeContext.Hash);

         var es = _curveActions.Multiply(_handshakeContext.EphemeralPrivateKey, publicKey);

         ExtractNextKeys(es);
         
         _aeadConstruction.EncryptWithAd(_handshakeContext.Hash, null, outputText.AsSpan(34));

         _hasher.Hash(_handshakeContext.Hash, outputText.AsSpan(34), _handshakeContext.Hash);

         return outputText;
      }
      
      private void ExtractNextKeys(ReadOnlySpan<byte> se)
      {
         var ckAndTempKey = new byte[64];
      
         _hkdf.ExtractAndExpand(_handshakeContext.ChainingKey, se, ckAndTempKey);
      
         ckAndTempKey.AsSpan(0, 32)
            .CopyTo(_handshakeContext.ChainingKey);
      
         _aeadConstruction.SetKey(ckAndTempKey.AsSpan(32));
      }
      
      public void Dispose()
      {
         var dispose = _hkdf as IDisposable;
         dispose?.Dispose();
      }
   }
}
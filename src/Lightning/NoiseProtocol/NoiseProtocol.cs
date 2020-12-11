using System;
using System.Buffers;

namespace NoiseProtocol
{
   public class NoiseProtocol : INoiseProtocol
   {
      readonly IEllipticCurveActions _curveActions;
      readonly IHkdf _hkdf;
      readonly ICipherFunction _aeadConstruction;
      readonly IHashFunction _hasher;
      readonly IKeyGenerator _keyGenerator;
      
      readonly HandshakeContext _handshakeContext;

      public NoiseProtocol(IEllipticCurveActions curveActions, IHkdf hkdf, 
         ICipherFunction aeadConstruction, IKeyGenerator keyGenerator, IHashFunction hasher,
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

      public void StartNewHandshake(byte[] remotePublicKey, IBufferWriter<byte> output)
      {
         _hasher.Hash(_handshakeContext.Hash, remotePublicKey, _handshakeContext.Hash);

         _handshakeContext.SetRemotePublicKey(remotePublicKey);
         
         GenerateLocalEphemeralAndProcessRemotePublicKey(remotePublicKey, output);
      }
      
      public void ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest, IBufferWriter<byte> output)
      {
         if (!_handshakeContext.HasRemotePublic)
         {
            //responder act one
            _hasher.Hash(_handshakeContext.Hash,_keyGenerator.GetPublicKey(_handshakeContext.PrivateKey).ToArray(),_handshakeContext.Hash);

            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(_handshakeContext.PrivateKey, handshakeRequest);

            //responder act two

            GenerateLocalEphemeralAndProcessRemotePublicKey(re.ToArray(), output);
         }
         else
         {
            //act two initiator
            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(_handshakeContext.EphemeralPrivateKey, handshakeRequest);

            CompleteInitiatorHandshake(re, output);
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

      private void CompleteInitiatorHandshake(ReadOnlySpan<byte> re, IBufferWriter<byte> output)
      {
         //act three initiator
         Span<byte> outputText = output.GetSpan(66);
         
         Span<byte> cipher = outputText.Slice(1, 49);

         _aeadConstruction.EncryptWithAd(_handshakeContext.Hash, 
            _keyGenerator.GetPublicKey(_handshakeContext.PrivateKey).ToArray(), cipher);

         _hasher.Hash(_handshakeContext.Hash, cipher, _handshakeContext.Hash);

         var se = _curveActions.Multiply(_handshakeContext.PrivateKey, re);

         ExtractNextKeys(se);

         _aeadConstruction.EncryptWithAd(_handshakeContext.Hash, new byte[0], 
            outputText.Slice(50, 16));

         ExtractFinalChannelKeys();

         output.Advance(66);
      }

      private ReadOnlySpan<byte> HandleReceivedHandshakeRequest(ReadOnlySpan<byte> privateKey,
         ReadOnlySpan<byte> handshakeRequest)
      {
         if (!handshakeRequest.StartsWith(LightningNetworkConfig.NoiseProtocolVersionPrefix))
            throw new AggregateException("Unsupported version in request");

         var re = handshakeRequest.Slice(1, 33);

         _hasher.Hash(_handshakeContext.Hash, re.ToArray(), _handshakeContext.Hash);

         var secret = _curveActions.Multiply(privateKey.ToArray(), re); //es and ee

         ExtractNextKeys(secret);

         var c = handshakeRequest.Slice(34, 16);
         var plainText = new byte[16];//TODO David move to cache on class
         _aeadConstruction.DecryptWithAd(_handshakeContext.Hash, c, plainText);

         _hasher.Hash(_handshakeContext.Hash, c.ToArray(), _handshakeContext.Hash);
         return re;
      }

      private void GenerateLocalEphemeralAndProcessRemotePublicKey(byte[] publicKey, IBufferWriter<byte> output)
      {
         Span<byte> outputText = output.GetSpan(50);
         
         Span<byte> ephemeralPublicKey = outputText.Slice(1, 33);
         Span<byte> cipher = outputText.Slice(34, 16);
         
         _handshakeContext.EphemeralPrivateKey = _keyGenerator.GenerateKey();

         _keyGenerator.GetPublicKey(_handshakeContext.EphemeralPrivateKey)
            .CopyTo(ephemeralPublicKey);

         _hasher.Hash(_handshakeContext.Hash, ephemeralPublicKey, _handshakeContext.Hash);

         var es = _curveActions.Multiply(_handshakeContext.EphemeralPrivateKey, publicKey);

         ExtractNextKeys(es);
         
         _aeadConstruction.EncryptWithAd(_handshakeContext.Hash, null, cipher);

         _hasher.Hash(_handshakeContext.Hash, cipher, _handshakeContext.Hash);
         
         output.Advance(50);
      }
      
      private void ExtractNextKeys(ReadOnlySpan<byte> se)
      {
         var ckAndTempKey = new byte[64]; //TODO David move to cache on class
      
         _hkdf.ExtractAndExpand(_handshakeContext.ChainingKey, se, ckAndTempKey);
      
         ckAndTempKey.AsSpan(0, 32)
            .CopyTo(_handshakeContext.ChainingKey);
      
         _aeadConstruction.SetKey(ckAndTempKey.AsSpan(32));
      }
   }
}
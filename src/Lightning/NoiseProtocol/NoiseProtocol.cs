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
      readonly INoiseMessageTransformer _messageTransformer;
      readonly IKeyGenerator _keyGenerator;

      public HandshakeContext HandshakeContext { get; }

      public NoiseProtocol(IEllipticCurveActions curveActions, IHkdf hkdf, 
         ICipherFunction aeadConstruction, IKeyGenerator keyGenerator, IHashFunction hasher,
         INoiseMessageTransformer messageTransformer, byte[] privateKey)
      {
         _curveActions = curveActions;
         _hkdf = hkdf;
         _aeadConstruction = aeadConstruction;
         _keyGenerator = keyGenerator;
         _hasher = hasher;
         _messageTransformer = messageTransformer;
         HandshakeContext = new HandshakeContext(privateKey);
      }

      public void InitHandShake()
      {
         _hasher.Hash(LightningNetworkConfig.ProtocolNameByteArray(), HandshakeContext.ChainingKey);

         _hasher.Hash(HandshakeContext.ChainingKey, LightningNetworkConfig.PrologueByteArray(), HandshakeContext.Hash);
      }

      // initiator act one
      public void StartNewInitiatorHandshake(byte[] remotePublicKey, IBufferWriter<byte> output)
      {
         _hasher.Hash(HandshakeContext.Hash, remotePublicKey, HandshakeContext.Hash);

         HandshakeContext.SetRemotePublicKey(remotePublicKey);
         
         GenerateLocalEphemeralAndProcessRemotePublicKey(remotePublicKey, output);
      }
      
      public void ProcessHandshakeRequest(ReadOnlySpan<byte> handshakeRequest, IBufferWriter<byte> output)
      {
         if (!HandshakeContext.HasRemotePublic)
         {
            //responder act one
            _hasher.Hash(HandshakeContext.Hash,_keyGenerator.GetPublicKey(HandshakeContext.PrivateKey).ToArray(),HandshakeContext.Hash);

            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(HandshakeContext.PrivateKey, handshakeRequest);

            //responder act two

            GenerateLocalEphemeralAndProcessRemotePublicKey(re.ToArray(), output);
         }
         else
         {
            //act two initiator
            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(HandshakeContext.EphemeralPrivateKey, handshakeRequest);

            //act three initiator
            CompleteInitiatorHandshake(re, output);
         }
      }

      // responder act three
      public void CompleteResponderHandshake(ReadOnlySpan<byte> handshakeRequest)
      {
         if (handshakeRequest[0] != 0)
            throw new ArgumentException(); // version byte
         
         var cipher = handshakeRequest.Slice(1, 49);

         HandshakeContext.RemotePublicKey = new byte[33];
         
         _aeadConstruction.DecryptWithAd(HandshakeContext.Hash, cipher, HandshakeContext.RemotePublicKey);
         
         _hasher.Hash(HandshakeContext.Hash,cipher.ToArray(),HandshakeContext.Hash);
         
         var se = _curveActions.Multiply(HandshakeContext.EphemeralPrivateKey, HandshakeContext.RemotePublicKey);
         
         ExtractNextKeys(se);
         
         var plainText = new byte[16];
         _aeadConstruction.DecryptWithAd(HandshakeContext.Hash, handshakeRequest.Slice(50), plainText);
         
         ExtractFinalChannelKeysForResponder();
      }

      public INoiseMessageTransformer GetMessageTransformer()
      {
         if(!_messageTransformer.CanProcessMessages())
            throw new InvalidOperationException("The handshake did not complete");

         return _messageTransformer;
      }

      void ExtractFinalChannelKeysForInitiator()
      {
         var ckAndTempKey = new byte[64];
         
         _hkdf.ExtractAndExpand(HandshakeContext.ChainingKey, new byte[0].AsSpan(), ckAndTempKey);

         _messageTransformer.SetKeys(HandshakeContext.ChainingKey,ckAndTempKey.AsSpan(0, 32),
            ckAndTempKey.AsSpan(32));
      }
      
      void ExtractFinalChannelKeysForResponder()
      {
         var ckAndTempKey = new byte[64];
         
         _hkdf.ExtractAndExpand(HandshakeContext.ChainingKey, new byte[0].AsSpan(), ckAndTempKey);

         _messageTransformer.SetKeys(HandshakeContext.ChainingKey,ckAndTempKey.AsSpan(32),
            ckAndTempKey.AsSpan(0, 32));
      }

      private void CompleteInitiatorHandshake(ReadOnlySpan<byte> re, IBufferWriter<byte> output)
      {
         //act three initiator
         Span<byte> outputText = output.GetSpan(66);
         
         Span<byte> cipher = outputText.Slice(1, 49);

         _aeadConstruction.EncryptWithAd(HandshakeContext.Hash, 
            _keyGenerator.GetPublicKey(HandshakeContext.PrivateKey).ToArray(), cipher);

         _hasher.Hash(HandshakeContext.Hash, cipher, HandshakeContext.Hash);

         var se = _curveActions.Multiply(HandshakeContext.PrivateKey, re);

         ExtractNextKeys(se);

         _aeadConstruction.EncryptWithAd(HandshakeContext.Hash, new byte[0], 
            outputText.Slice(50, 16));

         ExtractFinalChannelKeysForInitiator();

         output.Advance(66);
      }

      private ReadOnlySpan<byte> HandleReceivedHandshakeRequest(ReadOnlySpan<byte> privateKey,
         ReadOnlySpan<byte> handshakeRequest)
      {
         if (!handshakeRequest.StartsWith(LightningNetworkConfig.NoiseProtocolVersionPrefix))
            throw new AggregateException("Unsupported version in request");

         var re = handshakeRequest.Slice(1, 33);

         _hasher.Hash(HandshakeContext.Hash, re.ToArray(), HandshakeContext.Hash);

         var secret = _curveActions.Multiply(privateKey.ToArray(), re); //es and ee

         ExtractNextKeys(secret);

         var c = handshakeRequest.Slice(34, 16);
         var plainText = new byte[16];//TODO David move to cache on class
         _aeadConstruction.DecryptWithAd(HandshakeContext.Hash, c, plainText);

         _hasher.Hash(HandshakeContext.Hash, c.ToArray(), HandshakeContext.Hash);
         return re;
      }

      private void GenerateLocalEphemeralAndProcessRemotePublicKey(byte[] publicKey, IBufferWriter<byte> output)
      {
         Span<byte> outputText = output.GetSpan(50);
         
         Span<byte> ephemeralPublicKey = outputText.Slice(1, 33);
         Span<byte> cipher = outputText.Slice(34, 16);
         
         HandshakeContext.EphemeralPrivateKey = _keyGenerator.GenerateKey();

         _keyGenerator.GetPublicKey(HandshakeContext.EphemeralPrivateKey)
            .CopyTo(ephemeralPublicKey);

         _hasher.Hash(HandshakeContext.Hash, ephemeralPublicKey, HandshakeContext.Hash);

         var es = _curveActions.Multiply(HandshakeContext.EphemeralPrivateKey, publicKey);

         ExtractNextKeys(es);
         
         _aeadConstruction.EncryptWithAd(HandshakeContext.Hash, null, cipher);

         _hasher.Hash(HandshakeContext.Hash, cipher, HandshakeContext.Hash);
         
         output.Advance(50);
      }
      
      private void ExtractNextKeys(ReadOnlySpan<byte> se)
      {
         var ckAndTempKey = new byte[64]; //TODO David move to cache on class
      
         _hkdf.ExtractAndExpand(HandshakeContext.ChainingKey, se, ckAndTempKey);
      
         ckAndTempKey.AsSpan(0, 32)
            .CopyTo(HandshakeContext.ChainingKey);
      
         _aeadConstruction.SetKey(ckAndTempKey.AsSpan(32));
      }
   }
}
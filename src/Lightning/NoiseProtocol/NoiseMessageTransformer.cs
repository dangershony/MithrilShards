using System;
using System.Buffers;

namespace NoiseProtocol
{
   public class NoiseMessageTransformer : INoiseMessageTransformer
   {
      readonly IHkdf _hkdf;
      readonly ICipherFunction _writer, _reader;
      readonly byte[] _chainingKey;
      bool _keysSet;

      public NoiseMessageTransformer(IHkdf hkdf, ICipherFunction writer, ICipherFunction reader)
      {
         _hkdf = hkdf;
         _writer = writer;
         _reader = reader;
         _chainingKey = new byte[32];
      }

      public void SetKeys(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> senderKey, ReadOnlySpan<byte> receiverKey)
      {
         chainingKey.CopyTo(_chainingKey.AsSpan());
         _writer.SetKey(senderKey);
         _reader.SetKey(receiverKey);
         _keysSet = true;
      }

      public bool CanProcessMessages() => _keysSet;

      public int WriteMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output)
      {
         if (message.Length + Aead.TAG_SIZE > LightningNetworkConfig.MAX_MESSAGE_LENGTH)
            throw new ArgumentException($"Noise message must be less than or equal to {LightningNetworkConfig.MAX_MESSAGE_LENGTH} bytes in length.");
         
         int numOfBytesRead =  _writer.EncryptWithAd(null, message.ToArray(), // TODO David here we call to array should be replaced
            output.GetSpan((int)message.Length));
         
         output.Advance(numOfBytesRead);
         
         KeyRecycle(_writer);
         
         return numOfBytesRead;
      }

      public int ReadMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output)
      {
         int numOfBytesRead = _reader.DecryptWithAd(null, message.ToArray(), // TODO David here we call to array should be replaced 
            output.GetSpan((int)message.Length + Aead.TAG_SIZE));

         output.Advance(numOfBytesRead);
         
         KeyRecycle(_reader);

         return numOfBytesRead;
      }
      
      private void KeyRecycle(ICipherFunction cipherFunction)
      {
         if (cipherFunction.GetNonce() < LightningNetworkConfig.NumberOfNonceBeforeKeyRecycle)
            return;
         
         Span<byte> keys = stackalloc byte[Aead.KEY_SIZE * 2];
         _hkdf.ExtractAndExpand(_chainingKey, cipherFunction.GetKey(), keys);

         // set new chaining key
         keys.Slice(0, Aead.KEY_SIZE)
            .CopyTo(_chainingKey);

         // set new key
         cipherFunction.SetKey(keys.Slice(Aead.KEY_SIZE));
      }
   }
}
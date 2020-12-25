using System;
using System.Buffers;
using Bitcoin.Primitives.Fundamental;
using NBitcoin;
using NBitcoin.Crypto;
using Network.Protocol.Messages;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Validators.Gossip
{
   public class GossipValidationBase <TMessageType>
   {
      readonly ITlvStreamSerializer _tlvStreamSerializer;

      public GossipValidationBase(ITlvStreamSerializer tlvStreamSerializer)
      {
         _tlvStreamSerializer = tlvStreamSerializer;
      }

      internal static bool VerifySignature(PublicKey publicKey, CompressedSignature signature, byte[] doubleHash)
      {
         var keyVerifier = new PubKey(publicKey);

         return !ECDSASignature.TryParseFromCompact(signature, out var ecdsaSignature) || 
                keyVerifier.Verify(new uint256(doubleHash), ecdsaSignature);
      }

      internal static bool VerifyPublicKey(PublicKey publicKey)
      {
         return PubKey.Check(publicKey, true);
      }
      
      internal ReadOnlySpan<byte> GetMessageByteArray<TSerializer,TMessage>(TSerializer serializer, TMessage networkMessage,
         ushort signaturePosition)
      where TSerializer : BaseMessageSerializer<TMessage>
      where TMessage : BaseMessage,new()
      {
         ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();

         serializer.SerializeMessage(networkMessage,0,null!, output);

         return output.WrittenSpan.Slice(signaturePosition);
      }
   }
}
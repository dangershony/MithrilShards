using System;
using System.Buffers;
using NBitcoin;
using NBitcoin.Crypto;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.Serialization.Serializers.Messages.Gossip;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Validators.Gossip
{
   public class NodeAnnouncementValidator : IMessageValidator<NodeAnnouncement>
   {
      readonly ITlvStreamSerializer _tlvStreamSerializer;

      public NodeAnnouncementValidator(ITlvStreamSerializer tlvStreamSerializer)
      {
         _tlvStreamSerializer = tlvStreamSerializer;
      }

      public (bool, ErrorMessage?) ValidateMessage(NodeAnnouncement networkMessage)
      {
         if (!PubKey.Check(networkMessage.NodeId, true))
            return (false, null);

         var output = GetMessageByteArray(networkMessage);

         byte[]? doubleHash = Hashes.DoubleSHA256RawBytes(output.ToArray(), 0, output.Length);

         var publicKey = new PubKey(networkMessage.NodeId);

         if (ECDSASignature.TryParseFromCompact(networkMessage.Signature, out var ecdsaSignature) &&
             !publicKey.Verify(new uint256(doubleHash), ecdsaSignature))
            return (false, null);

         //TODO David validate features including addrlen

         return (true, null);
      }

      private ReadOnlySpan<byte> GetMessageByteArray(NodeAnnouncement networkMessage)
      {
         NodeAnnouncementSerializer serializer = new NodeAnnouncementSerializer(_tlvStreamSerializer); 
         
         ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();

         serializer.SerializeMessage(networkMessage,0,null!, output);

         return output.WrittenSpan.Slice(CompressedSignature.SIGNATURE_LENGTH);
      }
   }
}
using Bitcoin.Primitives.Fundamental;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using NBitcoin.Crypto;
using Network.Protocol.Serialization.Serializers.Messages.Gossip;
using Network.Protocol.TlvStreams;
using Network.Settings;
using Network.Storage.Gossip;

namespace Network.Protocol.Validators.Gossip
{
   public class ChannelAnnouncementValidator : GossipValidationBase ,IMessageValidator<ChannelAnnouncement>
   {
      readonly ITlvStreamSerializer _tlvStreamSerializer;
      readonly IGossipRepository _gossipRepository;
 
      public ChannelAnnouncementValidator(ITlvStreamSerializer tlvStreamSerializer, IGossipRepository gossipRepository) 
         : base(tlvStreamSerializer)
      {
         _tlvStreamSerializer = tlvStreamSerializer;
         _gossipRepository = gossipRepository;
      }

      public (bool, ErrorMessage?) ValidateMessage(ChannelAnnouncement networkMessage)
      {
         if (!VerifyPublicKey(networkMessage.NodeId1) || 
             !VerifyPublicKey(networkMessage.NodeId2) ||
             !VerifyPublicKey(networkMessage.BitcoinKey1) ||
             !VerifyPublicKey(networkMessage.BitcoinKey2))
            return (false, null);

         var messageByteArrayWithoutSignatures = GetMessageByteArray(
            new ChannelAnnouncementSerializer(_tlvStreamSerializer),
            networkMessage, CompressedSignature.SIGNATURE_LENGTH * 4);

         byte[]? doubleHash = Hashes.DoubleSHA256RawBytes(messageByteArrayWithoutSignatures.ToArray(), 
            0, messageByteArrayWithoutSignatures.Length);

         if (!VerifySignature(networkMessage.NodeId1, networkMessage.NodeSignature1, doubleHash) ||
             !VerifySignature(networkMessage.NodeId2, networkMessage.NodeSignature2, doubleHash) ||
             !VerifySignature(networkMessage.BitcoinKey1, networkMessage.BitcoinSignature1, doubleHash) ||
             !VerifySignature(networkMessage.BitcoinKey2, networkMessage.BitcoinSignature2, doubleHash))
            return (false, null);
         
         // TODO David add features validation
         // (from lightning rfc) if there is an unknown even bit in the features field:
         // MUST NOT attempt to route messages through the channel.


         if (_gossipRepository.IsNodeInBlacklistedList(networkMessage.NodeId1) ||
             _gossipRepository.IsNodeInBlacklistedList(networkMessage.NodeId2))
            return (false, null);
         
         if (!ChainHashes.SupportedChainHashes.ContainsValue(networkMessage.ChainHash))
            return (false, null);
         
         return (true, null);
      }
   }
}
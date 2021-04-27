using Bitcoin.Primitives.Fundamental;
using Network.Protocol.Messages.Gossip;

namespace Network.Protocol.Processors.Gossip
{
   public class AnnouncementSignatureValidationWrapper : AnnouncementSignatures
   {
      public AnnouncementSignatureValidationWrapper(AnnouncementSignatures announcementSignatures, PublicKey nodeId, PublicKey bitcoinAddress)
      {
         _announcementSignatures = announcementSignatures;
         ChannelId = announcementSignatures.ChannelId;
         ShortChannelId = announcementSignatures.ShortChannelId;
         NodeSignature = announcementSignatures.NodeSignature;
         BitcoinSignature = announcementSignatures.BitcoinSignature;
         NodeId = nodeId;
         BitcoinAddress = bitcoinAddress;
      }

      private readonly AnnouncementSignatures _announcementSignatures;

      public PublicKey NodeId { get; set; }

      public PublicKey BitcoinAddress { get; set; }
      
   }
}
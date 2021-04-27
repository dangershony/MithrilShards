using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Processors.Gossip;
using Network.Protocol.Serialization.Serializers.Messages.Gossip;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Validators.Gossip
{
   public class AnnouncementSignaturesValidator : GossipValidationBase,
      IMessageValidator<AnnouncementSignatureValidationWrapper>
   {
      public AnnouncementSignaturesValidator(ITlvStreamSerializer tlvStreamSerializer) 
         : base(tlvStreamSerializer)
      { }

      public (bool, ErrorMessage?) ValidateMessage(AnnouncementSignatureValidationWrapper networkMessage)
      {
         var messageByeArray = GetMessageByteArray(new AnnouncementSignaturesSerializer(TlvStreamSerializer)
            , (AnnouncementSignatures)networkMessage, 40);

         byte[]? hash = NBitcoin.Crypto.Hashes.DoubleSHA256RawBytes(messageByeArray.ToArray(), 0, messageByeArray.Length);

         if (!VerifySignature(networkMessage.NodeId, networkMessage.NodeSignature, hash) ||
             !VerifySignature(networkMessage.BitcoinAddress, networkMessage.BitcoinSignature, hash))
            return (false, null);

         return (true, null);
      }
   }
}
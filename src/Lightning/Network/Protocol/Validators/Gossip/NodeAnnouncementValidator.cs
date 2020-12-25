using Bitcoin.Primitives.Fundamental;
using NBitcoin.Crypto;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Serialization.Serializers.Messages.Gossip;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Validators.Gossip
{
   public class NodeAnnouncementValidator : GossipValidationBase<NodeAnnouncement>,IMessageValidator<NodeAnnouncement>
   {
      readonly ITlvStreamSerializer _tlvStreamSerializer;

      public NodeAnnouncementValidator(ITlvStreamSerializer tlvStreamSerializer) 
         : base(tlvStreamSerializer)
      {
         _tlvStreamSerializer = tlvStreamSerializer;
      }

      public (bool, ErrorMessage?) ValidateMessage(NodeAnnouncement networkMessage)
      {
         if (VerifyPublicKey(networkMessage.NodeId))
            return (false, null);

         var output = GetMessageByteArray(new NodeAnnouncementSerializer(_tlvStreamSerializer),
            networkMessage,CompressedSignature.SIGNATURE_LENGTH);

         byte[]? doubleHash = Hashes.DoubleSHA256RawBytes(output.ToArray(), 0, output.Length);

         if (!VerifySignature(networkMessage.NodeId,networkMessage.Signature,doubleHash)) 
            return (false, null);

         //TODO David validate features including addrlen

         return (true, null);
      }
   }
}
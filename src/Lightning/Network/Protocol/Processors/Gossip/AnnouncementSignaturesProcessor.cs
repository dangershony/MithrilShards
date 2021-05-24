using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Bitcoin.Primitives;
using Bitcoin.Primitives.Fundamental;
using Bitcoin.Primitives.Serialization;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Utils;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.Serialization.Serializers.Messages.Gossip;
using Network.Protocol.TlvStreams;
using Network.Protocol.Validators;
using Network.Settings;

namespace Network.Protocol.Processors.Gossip
{
   public class AnnouncementSignaturesProcessor : BaseProcessor,
      INetworkMessageHandler<AnnouncementSignatures>
   {
      readonly IMessageValidator<AnnouncementSignatureValidationWrapper> _messageValidator;
      readonly NodeContext _nodeContext;
      readonly NetworkPeerContext _peerContext;
      readonly IProtocolTypeSerializer<ChannelAnnouncement> _serializer;
      
      readonly ISignatureGenerator _signatureGenerator;
      const bool IS_HANDSHAKE_AWARE = true;

      bool announcementSignatureReceived = false;
      
      public AnnouncementSignaturesProcessor(ILogger<AnnouncementSignaturesProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, ISignatureGenerator signatureGenerator, 
         IMessageValidator<AnnouncementSignatureValidationWrapper> messageValidator, NodeContext nodeContext, NetworkPeerContext peerContext) 
         : base(logger, eventBus, peerBehaviorManager, IS_HANDSHAKE_AWARE)
      {
         _signatureGenerator = signatureGenerator;
         _messageValidator = messageValidator;
         _nodeContext = nodeContext;
         _peerContext = peerContext;
      }

      public async ValueTask<bool> ProcessMessageAsync(AnnouncementSignatures message, CancellationToken cancellation)
      {
         (bool isValid, ErrorMessage? errorMessage) = _messageValidator.ValidateMessage(new AnnouncementSignatureValidationWrapper
            (message,PeerContext.NodeId,PeerContext.BitcoinAddress));//TODO send in the bitcoin address to be validated
         
         if (!isValid)
         {
            if (errorMessage == null)
               throw new ArgumentException(nameof(message));
         
            await SendMessageAsync(errorMessage, cancellation)
               .ConfigureAwait(false);
         }
         //TODO David - need to verify the short channel id with the funding transaction  
         
         //TODO David - add check for funding transaction announce channel bit, and received funding locked message with 6 confirmations before sending a response

         announcementSignatureReceived = true;
         
         byte[] hashedChannelAnnouncement = GetHashedChannelAnnouncement(message);

         var reply = new AnnouncementSignatures(message.ChannelId,
            message.ShortChannelId,
            _signatureGenerator.Sign(_nodeContext.PrivateKey, hashedChannelAnnouncement),
            _signatureGenerator.Sign(
               PeerContext.BitcoinAddressKey ?? throw new ArgumentNullException(nameof(PeerContext.BitcoinAddressKey)),
               hashedChannelAnnouncement));

         await SendMessageAsync(reply, cancellation).ConfigureAwait(false);

         //TODO David - add gossip message broadcasting to all connected nodes
         
         return true;
      }

      byte[] GetHashedChannelAnnouncement(AnnouncementSignatures message)
      {
         // TODO return a hashed constructed announcement message
         var announcementChannel = new ChannelAnnouncement
         {
            Features = new byte[0],
            ChainHash = ChainHashes.Bitcoin,
            ShortChannelId = new ShortChannelId(null), //TODO add the logic when implemented 
            NodeId1 = (PublicKey) _nodeContext.LocalPubKey.ToByteArray(), 
            NodeId2 = new PublicKey (message.ChannelId),
            BitcoinKey1 = _peerContext.BitcoinAddress, 
            BitcoinKey2 = new PublicKey() //TODO David get the logic when implemented
         };
         
         ArrayBufferWriter<byte> output = new ArrayBufferWriter<byte>();
         
         _serializer.Serialize(announcementChannel, 0, output);
         
         byte[]? hash = NBitcoin.Crypto.Hashes.DoubleSHA256RawBytes(output.WrittenMemory.ToArray(),
            0, 40);
         
         throw new NotImplementedException();
      }
   }
}
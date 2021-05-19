using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Validators;

namespace Network.Protocol.Processors.Gossip
{
   public class AnnouncementSignaturesProcessor : BaseProcessor,
      INetworkMessageHandler<AnnouncementSignatures>
   {
      readonly IMessageValidator<AnnouncementSignatureValidationWrapper> _messageValidator;
      readonly NodeContext _nodeContext;
      
      readonly ISignatureGenerator _signatureGenerator;
      const bool IS_HANDSHAKE_AWARE = true;

      bool announcementSignatureReceived = false;
      
      public AnnouncementSignaturesProcessor(ILogger<AnnouncementSignaturesProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, ISignatureGenerator signatureGenerator, 
         IMessageValidator<AnnouncementSignatureValidationWrapper> messageValidator, NodeContext nodeContext) 
         : base(logger, eventBus, peerBehaviorManager, IS_HANDSHAKE_AWARE)
      {
         _signatureGenerator = signatureGenerator;
         _messageValidator = messageValidator;
         _nodeContext = nodeContext;
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
         
         byte[] hashedChannelAnnouncement = ParseMessageToByteArray(message);

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

      static byte[] ParseMessageToByteArray(AnnouncementSignatures message)
      {
         // TODO return a hashed constructed announcement message
         throw new NotImplementedException();
      }
   }
}
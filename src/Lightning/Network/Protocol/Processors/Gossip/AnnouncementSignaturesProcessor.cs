using System;
using System.Threading;
using System.Threading.Tasks;
using Bitcoin.Primitives.Fundamental;
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
      readonly ISignatureGenerator _signatureGenerator;
      const bool IS_HANDSHAKE_AWARE = true;

      public AnnouncementSignaturesProcessor(ILogger<AnnouncementSignaturesProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, ISignatureGenerator signatureGenerator, 
         IMessageValidator<AnnouncementSignatures> messageValidator) 
         : base(logger, eventBus, peerBehaviorManager, IS_HANDSHAKE_AWARE)
      {
         _signatureGenerator = signatureGenerator;
         _messageValidator = messageValidator;
      }

      public async ValueTask<bool> ProcessMessageAsync(AnnouncementSignatures message, CancellationToken cancellation)
      {
         (bool isValid, ErrorMessage? errorMessage) = _messageValidator.ValidateMessage(new AnnouncementSignatureValidationWrapper
            (message,PeerContext.NodeId,null!));//TODO send in the bitcoin address to be validated
         
         if (!isValid)
         {
            if (errorMessage == null)
               throw new ArgumentException(nameof(message));
         
            await SendMessageAsync(errorMessage, cancellation)
               .ConfigureAwait(false);
         }
         
         byte[] messageByteArray = ParseMessageToByteArray(message);

         var reply = new AnnouncementSignatures
         {
            ChannelId = message.ChannelId,
            ShortChannelId = message.ShortChannelId,
            NodeSignature = _signatureGenerator.Sign(PeerContext.NodeId, messageByteArray),
            BitcoinSignature = _signatureGenerator.Sign(PeerContext.NodeId, messageByteArray)
         };

         await SendMessageAsync(reply, cancellation).ConfigureAwait(false);

         return true;
      }

      static byte[] ParseMessageToByteArray(AnnouncementSignatures message)
      {
         byte[] messageByteArray = new byte[32 + 8];
         ((byte[]) message.ChannelId).CopyTo(messageByteArray.AsSpan(0, 32));
         ((byte[]) message.ShortChannelId).CopyTo(messageByteArray.AsSpan(32));
         return messageByteArray;
      }
   }

   public interface ISignatureGenerator
   {
      CompressedSignature Sign(byte[] secret, byte[] message);
   }
}
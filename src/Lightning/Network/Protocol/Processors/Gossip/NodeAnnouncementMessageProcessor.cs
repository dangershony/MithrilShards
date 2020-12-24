using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Validators;
using Network.Storage.Gossip;

namespace Network.Protocol.Processors.Gossip
{
   public class NodeAnnouncementMessageProcessor : BaseProcessor,
      INetworkMessageHandler<NodeAnnouncement>
   {
      readonly IMessageValidator<NodeAnnouncement> _messageValidator;

      readonly IGossipRepository _gossipRepository;

      public NodeAnnouncementMessageProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, IMessageValidator<NodeAnnouncement> messageValidator, IGossipRepository gossipRepository) 
         : base(logger, eventBus, peerBehaviorManager, true)
      {
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
      }

      public async ValueTask<bool> ProcessMessageAsync(NodeAnnouncement message, CancellationToken cancellation)
      {
         var (isValid, errorMessage) = _messageValidator.ValidateMessage(message);
         
         if (!isValid)
         {
            if (errorMessage == null)
               throw new ArgumentException(nameof(message));

            await SendMessageAsync(errorMessage, cancellation)
               .ConfigureAwait(false);
         }

         var node = new GossipNode
         {
            Addresses = message.Addresses,
            Addrlen = message.Addrlen,
            Alias = message.Alias,
            Features = message.Features,
            Timestamp = message.Timestamp,
            NodeId = message.NodeId,
            RgbColor = message.RgbColor
         };
         
         _gossipRepository.AddNode(node);
 
         eventBus.Publish(node);
         
         return await new ValueTask<bool>(false)
            .ConfigureAwait(false);;
      }
   }
}
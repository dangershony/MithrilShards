using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.Validators;
using Network.Storage.Gossip;

namespace Network.Protocol.Processors.Gossip
{
   public class NodeAnnouncementMessageProcessor : BaseProcessor,
      INetworkMessageHandler<NodeAnnouncement>,
      INetworkMessageHandler<GossipTimestampFilter>
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

         var node = new GossipNode(message.NodeId,message.Features,message.RgbColor,message.Alias,message.Addresses)
         {
            Addrlen = message.Addrlen,
            Timestamp = message.Timestamp
         };
         
         _gossipRepository.AddNode(node);
 
         eventBus.Publish(node);
         
         return await new ValueTask<bool>(false)
            .ConfigureAwait(false);;
      }

      public ValueTask<bool> ProcessMessageAsync(GossipTimestampFilter message, CancellationToken cancellation)
      {
         if (message.ChainHash == null)
            throw new ArgumentNullException(nameof(ChainHash));

         var node = _gossipRepository.GetNode(PeerContext.NodeId);

         var existingFilter = node.BlockchainTimeFilters
                                 .SingleOrDefault(_ => _.ChainHash.Equals(message.ChainHash))
                              ?? new GossipNodeTimestampFilter(message.ChainHash);

         existingFilter.FirstTimestamp = message.FirstTimestamp;
         existingFilter.TimestampRange = message.TimestampRange;

         _gossipRepository.AddNode(node);

         return new ValueTask<bool>(true);
      }
   }
}
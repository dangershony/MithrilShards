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
using Network.Storage.Gossip;

namespace Network.Protocol.Processors.Gossip
{
   public class GossipTimestampFilterProcessor : BaseProcessor,
      INetworkMessageHandler<GossipTimestampFilter>
   {
      readonly IGossipRepository _gossipRepository;
      
      public GossipTimestampFilterProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, IGossipRepository gossipRepository) 
         : base(logger, eventBus, peerBehaviorManager, true)
      {
         _gossipRepository = gossipRepository;
      }

      public ValueTask<bool> ProcessMessageAsync(GossipTimestampFilter message, CancellationToken cancellation)
      {
         if (message.ChainHash == null)
            throw new ArgumentNullException(nameof(ChainHash));

         var node = _gossipRepository.GetNode(PeerContext.NodeId);

         if (node == null)
            return new ValueTask<bool>(false); //TODO David should we support this as getting a node filter means we had a full handshake, perhaps should throw an exception

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
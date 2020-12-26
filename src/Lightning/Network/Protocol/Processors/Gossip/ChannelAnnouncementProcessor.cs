using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Validators;
using Network.Storage.Gossip;

namespace Network.Protocol.Processors.Gossip
{
   public class ChannelAnnouncementProcessor : BaseProcessor,
    INetworkMessageHandler<ChannelAnnouncement>
   {
      readonly IMessageValidator<ChannelAnnouncement> _messageValidator;
      readonly IGossipRepository _gossipRepository;
      
      public ChannelAnnouncementProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, IMessageValidator<ChannelAnnouncement> messageValidator, IGossipRepository gossipRepository) 
         : base(logger, eventBus, peerBehaviorManager, true)
      {
         _messageValidator = messageValidator;
         _gossipRepository = gossipRepository;
      }

      public async ValueTask<bool> ProcessMessageAsync(ChannelAnnouncement message, CancellationToken cancellation)
      {
         (bool isValid, ErrorMessage? errorMessage) = _messageValidator.ValidateMessage(message);
         if (!isValid)
         {
            if (errorMessage == null)
               throw new ArgumentException(nameof(message));
         
            await SendMessageAsync(errorMessage, cancellation)
               .ConfigureAwait(false);
         }

         var existingChannel = _gossipRepository.GetGossipChannel(message.ShortChannelId);

         if (existingChannel != null)
         {
            if (existingChannel.NodeId1 != message.NodeId1 || existingChannel.NodeId2 != message.NodeId2)
            {
               var nodes = _gossipRepository.GetNodes(existingChannel.NodeId1, existingChannel.NodeId2,
                  message.NodeId1, message.NodeId2);

               var nodeIds = nodes.Select(_ => _.NodeId).ToArray()
                  .Union(new[] {message.NodeId1, message.NodeId2, existingChannel.NodeId1, existingChannel.NodeId2})
                  .ToArray();

               _gossipRepository.AddNodeToBlacklist(nodeIds);

               var channelsToForget = nodes.SelectMany(_ => _.Channels);

               _gossipRepository.RemoveGossipChannels(channelsToForget
                  .Select(_ => _.ShortChannelId).ToArray());

            }
         }
            
         
         //TODO David add logic to verify P2WSH for bitcoin keys
         
         
         
       eventBus.Publish(new ChannelAnnouncementEvent
       {
          ChannelAnnouncement = message
       });

       return true;
      }
   }

   class ChannelAnnouncementEvent : EventBase
   {
      public ChannelAnnouncement? ChannelAnnouncement { get; set; }
   }
}
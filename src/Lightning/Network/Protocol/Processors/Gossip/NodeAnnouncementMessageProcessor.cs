using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using NBitcoin;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.Validators;

namespace Network.Protocol.Processors.Gossip
{
   public class NodeAnnouncementMessageProcessor : BaseProcessor,
      INetworkMessageHandler<NodeAnnouncement>
   {
      readonly IMessageValidator<NodeAnnouncement> _messageValidator;

      readonly IDictionary<PublicKey, NodeAnnouncement> _dictionary;
      
      public NodeAnnouncementMessageProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager, IMessageValidator<NodeAnnouncement> messageValidator) 
         : base(logger, eventBus, peerBehaviorManager, true)
      {
         _messageValidator = messageValidator;
         _dictionary = new Dictionary<PublicKey, NodeAnnouncement>();
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

         //TODO David extend this after defined storage and operations
         _dictionary.AddOrReplace(message.NodeId,message);
         
         return await new ValueTask<bool>(false)
            .ConfigureAwait(false);;
      }
   }
}
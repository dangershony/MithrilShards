using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Protocol.Messages;

namespace Network.Protocol.Processors
{
   public class ErrorMessageProcessor : BaseProcessor,INetworkMessageHandler<ErrorMessage>
   {
      public ErrorMessageProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, 
         IPeerBehaviorManager peerBehaviorManager) 
         : base(logger, eventBus, peerBehaviorManager, true)
      { }

      public ValueTask<bool> ProcessMessageAsync(ErrorMessage message, CancellationToken cancellation)
      {
         Logger.LogDebug($"Received error message from {PeerContext.PeerId}");
         if (message.Data != null) Logger.LogDebug($"{Encoding.ASCII.GetString(message.Data)}");

         return new ValueTask<bool>(false);
      }
   }
}
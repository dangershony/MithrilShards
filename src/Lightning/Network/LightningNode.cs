using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.MithrilShards;
using Network.Settings;

namespace Network
{
   public class LightningNode : IMithrilShard
   {
      private readonly ILogger<LightningNode> _logger;
      private readonly LightningNodeSettings _settings;

      public LightningNode(ILogger<LightningNode> logger, IOptions<LightningNodeSettings> settings)
      {
         _logger = logger;
         _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
      }

      public ValueTask InitializeAsync(CancellationToken cancellationToken)
      {
         return default;
      }

      public ValueTask StartAsync(CancellationToken cancellationToken)
      {
         return default;
      }

      public ValueTask StopAsync(CancellationToken cancellationToken)
      {
         return default;
      }
   }
}
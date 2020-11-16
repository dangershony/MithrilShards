using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;

namespace MithrilShards.Network.Bedrock
{
   public static class TransportBuilder
   {
      public static IHostBuilder AddServer(this IHostBuilder hostBuilder)
      {
         if (hostBuilder is null)
         {
            throw new System.ArgumentNullException(nameof(hostBuilder));
         }

         hostBuilder.ConfigureServices((hostBuildContext, services) =>
         {
            services
               .AddHostedService<TransportStartup>()
               .AddSingleton<IConnectivityPeerStats, ConnectivityPeerStats>()
               .AddSingleton<ConnectionClientHandler>()
               .Configure<TransportSettings>(hostBuildContext.Configuration.GetSection(nameof(TransportSettings)));
         });

         return hostBuilder;
      }
   }
}
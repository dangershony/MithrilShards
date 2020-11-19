using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;

namespace MithrilShards.Network.Bedrock
{
   public static class TransportBuilderExtensions
   {
      public static IForgeBuilder UseBedrockTransport<TNetworkProtocolMessageSerializer>(this IForgeBuilder forgeBuilder) where TNetworkProtocolMessageSerializer : class, INetworkProtocolMessageSerializer
      {
         if (forgeBuilder is null)
         {
            throw new System.ArgumentNullException(nameof(forgeBuilder));
         }

         forgeBuilder.AddShard<TransportConnectivity, ForgeConnectivitySettings>(
            (hostBuildContext, services) =>
            {
               services
                  .Replace(ServiceDescriptor.Singleton<ITransportConnectivity, TransportConnectivity>())
                  .AddSingleton<IConnectivityPeerStats, ConnectivityPeerStats>()
                  .AddSingleton<MithrilForgeClientConnectionHandler>()
                  .AddTransient<INetworkProtocolMessageSerializer, TNetworkProtocolMessageSerializer>()
                  ;
            });

         return forgeBuilder;
      }
   }
}
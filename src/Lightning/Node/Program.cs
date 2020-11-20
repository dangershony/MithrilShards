using System;
using System.Threading.Tasks;
using MithrilShards.Core.Forge;
using MithrilShards.Dev.Controller;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using Network;
using Network.Protocol.Transport;

namespace Node
{
   internal class Program
   {
      private static async Task Main(string[] args)
      {
         //await new ForgeBuilder()
         //   .UseForge<DefaultForge>(args)
         //   .UseSerilog("log-settings-with-seq.json")
         //   .UseBedrockForgeServer<TransportMessageSerializer>()
         //   .UseDevController(assemblyScaffoldEnabler => assemblyScaffoldEnabler.LoadAssemblyFromType<LightningNode>())
         //   .UseLightningNetwork()
         //   .RunConsoleAsync().ConfigureAwait(false);

         Task node1 = new ForgeBuilder()
            .UseForge<DefaultForge>(args, configurationFile: "forge-settings-node1.json")
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockForgeServer<TransportMessageSerializer>()
            .UseDevController(assemblyScaffoldEnabler => assemblyScaffoldEnabler.LoadAssemblyFromType<LightningNode>())
            .UseLightningNetwork()
            .RunConsoleAsync();

         Task node2 = new ForgeBuilder()
            .UseForge<DefaultForge>(args, configurationFile: "forge-settings-node2.json")
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockForgeServer<TransportMessageSerializer>()
            .UseDevController(assemblyScaffoldEnabler => assemblyScaffoldEnabler.LoadAssemblyFromType<LightningNode>())
            .UseLightningNetwork()
            .RunConsoleAsync();

         await Task.WhenAll(node1, node2).ConfigureAwait(false);
      }
   }
}
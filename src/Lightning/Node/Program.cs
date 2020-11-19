using System;
using System.Threading.Tasks;
using MithrilShards.Core.Forge;
using MithrilShards.Dev.Controller;
using MithrilShards.Example;
using MithrilShards.Example.Dev;
using MithrilShards.Example.Protocol;
using MithrilShards.Logging.Serilog;
using MithrilShards.Network.Bedrock;
using Network;
using Network.Peer.Transport;

namespace Node
{
   internal class Program
   {
      private static async Task Main(string[] args)
      {
         await new ForgeBuilder()
            .UseForge<DefaultForge>(args)
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockForgeServer<TransportMessageSerializer>()
            .UseDevController(assemblyScaffoldEnabler => assemblyScaffoldEnabler.LoadAssemblyFromType<LightningNode>())
            .UseLightningNetwork()
            .RunConsoleAsync().ConfigureAwait(false);
      }
   }
}
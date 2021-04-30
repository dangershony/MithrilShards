using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
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
         if (args.Contains("test")) { await TestNodes(args).ConfigureAwait(false); return; }

         await new ForgeBuilder()
            .UseForge<DefaultForge>(args, configurationFile: "lightning-settings.json")
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockNetwork<TransportMessageSerializer>()
            .UseApi(options => options.ControllersSeeker = (seeker) => seeker.LoadAssemblyFromType<LightningNode>())
            .UseDevController()
            .UseLightningNetwork()
            .RunConsoleAsync().ConfigureAwait(false);
      }

      private static async Task TestNodes(string[] args)
      {
         Task node1 = new ForgeBuilder()
            .UseForge<DefaultForge>(args, configurationFile: "lightning-settings.json")
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockNetwork<TransportMessageSerializer>()
            .UseApi(options => options.ControllersSeeker = (seeker) => seeker.LoadAssemblyFromType<LightningNode>())
            .UseDevController()
            .UseLightningNetwork()
            .RunConsoleAsync();

         string[] args1 = args.Append("--ForgeConnectivity:Listeners:0:Endpoint=127.0.0.1:9736")
                              .Append("--DevController:EndPoint=127.0.0.1:5001")
                              .Append("--WebApi:EndPoint=127.0.0.1:45021")
                              .ToArray();

         Task node2 = new ForgeBuilder()
            .UseForge<DefaultForge>(args1, configurationFile: "lightning-settings.json")
            .UseSerilog("log-settings-with-seq.json")
            .UseBedrockNetwork<TransportMessageSerializer>()
            .UseApi(options => options.ControllersSeeker = (seeker) => seeker.LoadAssemblyFromType<LightningNode>())
            .UseDevController()
            .UseLightningNetwork()
            .RunConsoleAsync();

         await Task.WhenAll(node1, node2).ConfigureAwait(false);
      }
   }
}
using System;
using System.Threading.Tasks;
using MithrilShards.Core.Forge;
using MithrilShards.Logging.Serilog;

namespace Node
{
   internal class Program
   {
      private static async Task Main(string[] args)
      {
         await StartBedrockForgeServerAsync(args).ConfigureAwait(false);
      }

      private static async Task StartBedrockForgeServerAsync(string[] args)
      {
         await new ForgeBuilder()
            .UseForge<DefaultForge>(args)
            .ExtendInnerHostBuilder(host =>
            {
               host..ConfigureServices((context, service) =>
               {
                  service.
               })
            })
            //  .UseSerilog("log-settings-with-seq.json")
            //.UseBedrockForgeServer()

            .RunConsoleAsync()
            .ConfigureAwait(false);
      }
   }
}
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MithrilShards.Core.MithrilShards;
using MithrilShards.Example.Protocol.Messages;

namespace MithrilShards.Core.Forge
{
   public interface ITransportConnectivity : IMithrilShard
   {
      ValueTask AttemptConnectionAsync(LightningEndpoint remoteEndPoint, CancellationToken cancellation);
   }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Threading;

namespace Network.Settings
{
   /// <summary>
   /// Tries to connect to peers configured to be connected.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.Client.IConnector" />
   public class NetworkRequiredConnection : ConnectorBase
   {
      private LightningNodeSettings settings;

      public List<OutgoingConnectionEndPoint> connectionsToAttempt = new List<OutgoingConnectionEndPoint>();

      public NetworkRequiredConnection(ILogger<NetworkRequiredConnection> logger,
                                IEventBus eventBus,
                                IOptions<LightningNodeSettings> options,
                                IConnectivityPeerStats serverPeerStats,
                                IForgeConnectivity forgeConnectivity,
                                IPeriodicWork connectionLoop) : base(logger, eventBus, serverPeerStats, forgeConnectivity, connectionLoop)
      {
         this.settings = options.Value!;
      }

      protected override async ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation)
      {
         foreach (OutgoingConnectionEndPoint remoteEndPoint in this.connectionsToAttempt)
         {
            if (cancellation.IsCancellationRequested) break;

            if (connectionManager.CanConnectTo(remoteEndPoint.EndPoint))
            {
               // note that AttemptConnection is not blocking because it returns when the peer fails to connect or when one of the parties disconnect
               _ = this.forgeConnectivity.AttemptConnectionAsync(remoteEndPoint, cancellation).ConfigureAwait(false);

               // apply a delay between attempts to prevent too many connection attempt in a row
               await Task.Delay(500, cancellation).ConfigureAwait(false);
            }
         }
      }

      /// <summary>
      /// Tries the add end point.
      ///
      /// </summary>
      /// <param name="endPoint">The end point.</param>
      /// <returns><see langword="true"/> if the endpoint has been added, <see langword="false"/> if the endpoint was already listed.</returns>
      public bool TryAddLightningEndPoint(LightningEndpoint endPoint)
      {
         endPoint.EndPoint = endPoint.EndPoint.AsIPEndPoint().EnsureIPv6();
         if (this.connectionsToAttempt.Exists(ip => ip.Equals(endPoint)))
         {
            this.logger.LogDebug("EndPoint {RemoteEndPoint} already in the list of connections attempt.", endPoint);
            return false;
         }
         else
         {
            var outgoingConnectionEndPoint = new OutgoingConnectionEndPoint(endPoint.EndPoint.AsIPEndPoint());
            outgoingConnectionEndPoint.Items[nameof(LightningEndpoint)] = endPoint;
            this.connectionsToAttempt.Add(outgoingConnectionEndPoint);
            this.logger.LogDebug("EndPoint {RemoteEndPoint} added to the list of connections attempt.", endPoint);
            return true;
         }
      }
   }
}
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Threading;
using Network.Settings;

namespace Network.Protocol.Transport
{
   /// <summary>
   /// Tries to connect to peers configured to be connected.
   /// </summary>
   /// <seealso cref="MithrilShards.Core.Network.Client.IConnector" />
   public class NetworkRequiredConnection : ConnectorBase
   {
      private LightningNodeSettings _settings;

      public List<OutgoingConnectionEndPoint> connectionsToAttempt = new List<OutgoingConnectionEndPoint>();

      public NetworkRequiredConnection(ILogger<NetworkRequiredConnection> logger,
                                IEventBus eventBus,
                                IOptions<LightningNodeSettings> options,
                                IConnectivityPeerStats serverPeerStats,
                                IForgeConnectivity forgeConnectivity,
                                IPeriodicWork connectionLoop) : base(logger, eventBus, serverPeerStats, forgeConnectivity, connectionLoop)
      {
         _settings = options.Value!;
      }

      protected override async ValueTask AttemptConnectionsAsync(IConnectionManager connectionManager, CancellationToken cancellation)
      {
         foreach (OutgoingConnectionEndPoint remoteEndPoint in connectionsToAttempt)
         {
            if (cancellation.IsCancellationRequested) break;

            if (connectionManager.CanConnectTo(remoteEndPoint.EndPoint))
            {
               // note that AttemptConnection is not blocking because it returns when the peer fails to connect or when one of the parties disconnect
               _ = forgeConnectivity.AttemptConnectionAsync(remoteEndPoint, cancellation).ConfigureAwait(false);

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
         if (connectionsToAttempt.Exists(ip => ip.Equals(endPoint)))
         {
            logger.LogDebug("EndPoint {RemoteEndPoint} already in the list of connections attempt.", endPoint);
            return false;
         }
         else
         {
            var outgoingConnectionEndPoint = new OutgoingConnectionEndPoint(endPoint.EndPoint.AsIPEndPoint());
            outgoingConnectionEndPoint.Items[nameof(LightningEndpoint)] = endPoint;
            connectionsToAttempt.Add(outgoingConnectionEndPoint);
            logger.LogDebug("EndPoint {RemoteEndPoint} added to the list of connections attempt.", endPoint);
            return true;
         }
      }

      /// <summary>
      /// Tries the remove end point from the list of connection attempts.
      /// </summary>
      /// <param name="endPoint">The end point to remove.</param>
      /// <returns><see langword="true"/> if the endpoint has been removed, <see langword="false"/> if the endpoint has not been found.</returns>
      public bool TryRemoveEndPoint(IPEndPoint endPoint)
      {
         endPoint = endPoint.AsIPEndPoint().EnsureIPv6();
         return connectionsToAttempt.RemoveAll(remoteEndPoint => remoteEndPoint.EndPoint.Equals(endPoint)) > 0;
      }
   }
}
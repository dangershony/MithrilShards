using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Events;
using MithrilShards.Dev.Controller.Models.Requests;
using Network.Protocol.Transport;
using Network.Settings;

namespace Network.Api.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class PeerControllerDev : ControllerBase
   {
      private readonly ILogger<PeerControllerDev> _logger;
      private readonly IEventBus _eventBus;
      private readonly NetworkRequiredConnection? _requiredConnection;

      public PeerControllerDev(ILogger<PeerControllerDev> logger, IEventBus eventBus, IEnumerable<IConnector>? connectors)
      {
         _logger = logger;
         _eventBus = eventBus;
         _requiredConnection = connectors?.OfType<NetworkRequiredConnection>().FirstOrDefault();
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Connect")]
      public ActionResult<bool> Connect(PeerConnectRequest request)
      {
         if (_requiredConnection == null)
         {
            return NotFound($"Cannot produce output because {nameof(NetworkRequiredConnection)} is not available");
         }

         if (!LightningEndpoint.TryParse(request.EndPoint, out LightningEndpoint ipEndPoint))
         {
            return BadRequest("Incorrect endpoint");
         }

         return Ok(_requiredConnection.TryAddLightningEndPoint(ipEndPoint));
      }

      [HttpPost]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Disconnect")]
      public ActionResult<bool> Disconnect(PeerDisconnectRequest request)
      {
         if (!IPEndPoint.TryParse(request.EndPoint, out IPEndPoint ipEndPoint))
         {
            return BadRequest("Incorrect endpoint");
         }

         _requiredConnection.TryRemoveEndPoint(ipEndPoint);
         _eventBus.Publish(new PeerDisconnectionRequired(ipEndPoint, request.Reason));

         return true;
      }
   }
}
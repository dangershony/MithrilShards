using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Dev.Controller.Models.Requests;
using Network.Api.Models.Responses;

namespace Network.Api.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class NodeControllerDev : ControllerBase
   {
      private readonly ILogger<PeerControllerDev> _logger;
      private readonly IEventBus _eventBus;
      private readonly NodeContext _nodeContext;

      public NodeControllerDev(ILogger<PeerControllerDev> logger, IEventBus eventBus, NodeContext nodeContext)
      {
         _logger = logger;
         _eventBus = eventBus;
         _nodeContext = nodeContext;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      [Route("Info")]
      public ActionResult<NodeInfoResponse> Info()
      {
         return Ok(new NodeInfoResponse { NodeId = new NBitcoin.Key(_nodeContext.PrivateKey).PubKey.ToHex() });
      }
   }
}
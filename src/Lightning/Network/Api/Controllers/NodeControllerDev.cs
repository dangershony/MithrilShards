using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.WebApi;
using Network.Api.Models.Responses;

namespace Network.Api.Controllers
{
   [Area(WebApiArea.AREA_DEV)]
   public class NodeController : MithrilControllerBase
   {
      private readonly ILogger<NodeController> _logger;
      private readonly IEventBus _eventBus;
      private readonly NodeContext _nodeContext;

      public NodeController(ILogger<NodeController> logger, IEventBus eventBus, NodeContext nodeContext)
      {
         _logger = logger;
         _eventBus = eventBus;
         _nodeContext = nodeContext;
      }

      [HttpGet("Info")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public ActionResult<NodeInfoResponse> Info()
      {
         return Ok(new NodeInfoResponse { NodeId = new NBitcoin.Key(_nodeContext.PrivateKey).PubKey.ToHex() });
      }
   }
}
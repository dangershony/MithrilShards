﻿using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.EventBus;
using MithrilShards.Dev.Controller;

namespace MithrilShards.Chain.Bitcoin.Dev
{
   [ApiController]
   [DevController]
   [Route("[controller]")]
   public class ConsensusControllerDev : ControllerBase
   {
      private readonly ILogger<ConsensusControllerDev> _logger;
      readonly IEventBus _eventBus;
      readonly IChainState _chainState;

      public ConsensusControllerDev(ILogger<ConsensusControllerDev> logger, IEventBus eventBus, IChainState chainState)
      {
         this._logger = logger;
         this._eventBus = eventBus;
         this._chainState = chainState;
      }

      [HttpGet]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [Route("ShowBestHeaderTree")]
      public ActionResult<string> ShowBestHeaderTree()
      {
         return this.Ok(DumpKnownTree(this._chainState.BestHeader));
      }


      public static string DumpKnownTree(HeaderNode tip)
      {
         var stringBuilder = new StringBuilder();

         var nodes = new List<HeaderNode>(tip.Height + 1);
         HeaderNode? current = tip;

         do
         {
            nodes.Add(current);
            current = current.Previous;
         } while (current != null);

         nodes.Reverse();

         for (int i = 0; i < nodes.Count - 1; i++)
         {
            PrintNode(stringBuilder, nodes[i], indent: "", isLast: false);
         }

         PrintNode(stringBuilder, nodes[^1], indent: "", isLast: true);

         return stringBuilder.ToString();
      }

      private static void PrintNode(StringBuilder stringBuilder, HeaderNode node, string indent, bool isLast)
      {
         const string _cross = " ├─";
         const string _corner = " └─";
         const string _vertical = " │ ";
         const string _space = "   ";

         // Print the provided pipes/spaces indent
         stringBuilder.Append(indent);

         // Depending if this node is a last child, print the
         // corner or cross, and calculate the indent that will
         // be passed to its children
         if (isLast)
         {
            stringBuilder.Append(_corner);
            indent += _space;
         }
         else
         {
            stringBuilder.Append(_cross);
            indent += _vertical;
         }

         if (node.Height == 0)
         {
            stringBuilder.AppendLine($" GENESIS - {node.Hash}");
         }
         else
         {
            stringBuilder.AppendLine($" {node.Height:0000000} - {node.Hash}");
         }

         //// Loop through the children recursively, passing in the
         //// indent, and the isLast parameter
         //var numberOfChildren = node.Previous.Count;
         //for (var i = 0; i < numberOfChildren; i++)
         //{
         //   var child = node.Children[i];
         //   var isLast = (i == (numberOfChildren - 1));
         //   PrintNode(child, indent, isLast);
         //}
      }
   }
}

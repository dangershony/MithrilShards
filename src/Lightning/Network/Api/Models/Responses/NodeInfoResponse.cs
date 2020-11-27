using System.ComponentModel.DataAnnotations;

namespace MithrilShards.Dev.Controller.Models.Requests
{
   public class NodeInfoResponse
   {
      [Required]
      public string NodeId { get; set; } = string.Empty;
   }
}
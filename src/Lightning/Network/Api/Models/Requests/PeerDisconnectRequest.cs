using System.ComponentModel.DataAnnotations;

namespace MithrilShards.Dev.Controller.Models.Requests
{
   public class PeerDisconnectRequest
   {
      [Required]
      public string EndPoint { get; set; } = string.Empty;

      public string Reason { get; set; } = string.Empty;
   }
}
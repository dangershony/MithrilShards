using System.ComponentModel.DataAnnotations;

namespace MithrilShards.Dev.Controller.Models.Requests
{
   public class PeerConnectRequest
   {
      [Required]
      public string EndPoint { get; set; } = string.Empty;
   }
}
using System.ComponentModel.DataAnnotations;

namespace Network.Api.Models.Requests
{
   public class PeerDisconnectRequest
   {
      [Required]
      public string EndPoint { get; set; } = string.Empty;

      public string Reason { get; set; } = string.Empty;
   }
}
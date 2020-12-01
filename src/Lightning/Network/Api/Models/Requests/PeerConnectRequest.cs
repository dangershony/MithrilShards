using System.ComponentModel.DataAnnotations;

namespace Network.Api.Models.Requests
{
   public class PeerConnectRequest
   {
      [Required]
      public string EndPoint { get; set; } = string.Empty;
   }
}
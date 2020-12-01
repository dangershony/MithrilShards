using System.ComponentModel.DataAnnotations;

namespace Network.Api.Models.Responses
{
   public class NodeInfoResponse
   {
      [Required]
      public string NodeId { get; set; } = string.Empty;
   }
}
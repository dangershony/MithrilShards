using System.Diagnostics.CodeAnalysis;

namespace Network.Settings
{
   /// <summary>
   /// Client Peer endpoint the node would like to be connected to.
   /// </summary>
   public class LightningNodeBinding
   {
      /// <summary>IP address and port number of the peer we wants to connect to.</summary>
      public string? LightningEndPoint { get; set; }

      public bool TryGetEndPoint([MaybeNullWhen(false)] out LightningEndpoint endPoint)
      {
         endPoint = null;

         if (!LightningEndpoint.TryParse(this.LightningEndPoint, out endPoint))
         {
            return false;
         }

         return true;
      }
   }
}
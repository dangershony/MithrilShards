using MithrilShards.Core.Network;
using Network.Protocol.Transport;

namespace Network.Protocol
{
   public interface INetworkPeerContext : IPeerContext
   {
      bool HandshakeComplete { get; set; }

      bool InitComplete { get; set; }

      IHandshakeProtocol? HandshakeProtocol { get; set; }

      void SetHandshakeProtocol(IHandshakeProtocol handshakeProtocol);

      void OnHandshakeCompleted();

      void OnInitMessageCompleted();
   }
}
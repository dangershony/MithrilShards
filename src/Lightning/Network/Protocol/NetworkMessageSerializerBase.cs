using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace Network.Protocol
{
   public abstract class NetworkMessageSerializerBase<TMessageType> : NetworkMessageSerializerBase<TMessageType, NetworkPeerContext> where TMessageType : INetworkMessage, new()
   {
      protected void MethodOurSerializersMayNeed()
      {
      }
   }
}
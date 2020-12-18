using MithrilShards.Core.Network.Protocol;
using Network.Protocol.Messages;

namespace Network.Protocol.Validators
{
   public interface IMessageValidator<in T> where T : INetworkMessage
   {
      (bool, ErrorMessage?) ValidateMessage(T networkMessage);
   }
}
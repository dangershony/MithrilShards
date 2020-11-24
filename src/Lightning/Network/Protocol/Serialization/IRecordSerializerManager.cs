using System.Diagnostics.CodeAnalysis;

namespace Network.Protocol.Serialization
{
   public interface IRecordSerializerManager
   {
      bool TryGetType(ulong recordType, [MaybeNullWhen(false)] out IRecordSerializer recordSerializer);
   }
}

using System;
using FASTER.core;
using Newtonsoft.Json;

namespace Repository
{
   internal class UlongStringPersistenceStore : IPersistenceSession, IDisposable
   {
      readonly ClientSession<ulong, string, string, string, Empty, SimpleFunctions<ulong, string>> _session;
      
      public UlongStringPersistenceStore( ClientSession<ulong, string, string, string, Empty, 
         SimpleFunctions<ulong, string>> session)
      {
         _session = session;
      }

      public T GetById<T>(ulong id)
      {
         (Status status, string data) = _session.Read(id);

         if (status != Status.OK)
         {
            throw new InvalidOperationException("Failed to read from FASTER key value store");
         }
         
         return JsonConvert.DeserializeObject<T>(data);
      }

      public void Add<T>(ulong id, T item)
      {
         string data = JsonConvert.SerializeObject(item);

         _session.RMW(id, data);
      }

      public void SaveChanges() => _session.WaitForCommitAsync();

      public void Dispose() => _session?.Dispose();
   }
}
using System;
using System.Threading.Tasks;
using FASTER.core;
using Newtonsoft.Json;

namespace Repository
{
   internal class UlongStringPersistenceStore : IPersistenceStore<ulong>, IDisposable
   {
      readonly IFasterKV<ulong,string> _fasterKv;
      readonly ClientSession<ulong, string, string, string, Empty, 
         IFunctions<ulong, string, string, string, Empty>> _session;
      
      public UlongStringPersistenceStore(IFasterKV<ulong,string> fasterKv)
      {
         _session = fasterKv.NewSession(new SimpleFunctions<ulong,string>());
         _fasterKv = fasterKv;
      }

      public T GetById<T>(ulong id)
      {
         (Status status, string data) = _session.Read(id);

         if (status != Status.OK)
         {
            _session.CompletePending();
         }
         
         return JsonConvert.DeserializeObject<T>(data);
      }

      public void Add<T>(ulong id, T item) where T : class
      {
         string data = JsonConvert.SerializeObject(item);

         _session.RMW(id, data);
      }
      
      public ValueTask SaveChangesAsync()
      {
         _fasterKv.TakeFullCheckpoint(out Guid token, CheckpointType.FoldOver);
         return _fasterKv.CompleteCheckpointAsync();
      }
      public void Dispose() => _session?.Dispose();
   }
}
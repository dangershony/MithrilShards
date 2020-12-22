using System;
using System.Threading.Tasks;
using FASTER.core;
using Newtonsoft.Json;

namespace Repository
{
   internal class SpanByteStringPersistenceStore : IPersistenceStore, IDisposable
   {
      readonly IFasterKV<SpanByte,string> _fasterKv;
      readonly ClientSession<SpanByte, string, string, string, Empty, SimpleFunctions<SpanByte, string>> _session;
      
      public SpanByteStringPersistenceStore(FasterKV<SpanByte,string> fasterKv)
      {
         _session = fasterKv.For(new SimpleFunctions<SpanByte,string>())
            .NewSession<SimpleFunctions<SpanByte,string>>();
         _fasterKv = fasterKv;
      }

      public T GetById<T>(byte[] id)
      {
         (Status status, string data) = _session.Read(ToSpanByte(id));

         if (status != Status.OK)
         {
            _session.CompletePending();
         }
         
         return JsonConvert.DeserializeObject<T>(data);
      }

      public void Add<T>(byte[] id, T item) where T : class
      {
         string data = JsonConvert.SerializeObject(item);

         _session.RMW(ToSpanByte(id), data);
      }
      
      private static SpanByte ToSpanByte(byte[] key)
      {
         return SpanByte.FromFixedSpan(key);
      }
      
      public ValueTask SaveChangesAsync()
      {
         _fasterKv.TakeFullCheckpoint(out Guid token, CheckpointType.FoldOver);
         return _fasterKv.CompleteCheckpointAsync();
      }

      public void Dispose()
      {
         _fasterKv?.Dispose();
         _session?.Dispose();
      } 
   }
}
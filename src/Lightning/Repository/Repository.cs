using System;
using System.Threading.Tasks;

namespace Repository
{
   internal class Repository : IRepository<ulong> , IDisposable
   {
      private readonly IPersistenceStore<ulong> _store;

      public Repository(IPersistenceStoreFactory<ulong> storeFactory)
      {
         _store = storeFactory.CreateKeyStore();
      }

      public void Add<T>(ulong key, T item) where T : class => _store.Add(key, item);

      public T Get<T>(ulong key) where T : class => _store.GetById<T>(key);
      public ValueTask SaveChangesAsync() => _store.SaveChangesAsync();

      public void Dispose()
      {
         var disposable = _store as IDisposable;
         
         disposable?.Dispose();
      }
   }
}
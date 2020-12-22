using System;
using System.Threading.Tasks;

namespace Repository
{
   internal class Repository<T> : IRepository<T> , IDisposable where T : class
   {
      private readonly IPersistenceStore _store;

      public Repository(IPersistenceStoreFactory storeFactory)
      {
         _store = storeFactory.CreateKeyStore();
      }

      public void Add(byte[] key, T item) => _store.Add(key, item);

      public T Get(byte[] key) => _store.GetById<T>(key);
      public ValueTask SaveChangesAsync() => _store.SaveChangesAsync();

      public void Dispose()
      {
         var disposable = _store as IDisposable;
         
         disposable?.Dispose();
      }
   }
}
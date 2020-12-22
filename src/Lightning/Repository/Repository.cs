using System;

namespace Repository
{
   internal class Repository<T> : IRepository<T> , IDisposable
      where T : class
   {
      readonly IPersistenceStore _store;

      public Repository(IPersistenceStoreFactory storeFactory)
      {
         _store = storeFactory.CreateUlongKeyStore();
      }

      public void Add(ulong key, T item) => _store.Add(key, item);

      public T Get(ulong key) => _store.GetById<T>(key);

      public void Dispose()
      {
         var disposable = _store as IDisposable;
         
         disposable?.Dispose();
      }
   }
}
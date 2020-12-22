using System;

namespace Repository
{
   internal class Repository<T> : IRepository<T> , IDisposable
      where T : class
   {
      readonly IPersistenceSession _session;

      public Repository(IPersistenceStoreFactory storeFactory)
      {
         _session = storeFactory.CreateUlongKeyStore();
      }

      public void Add(ulong key, T item) => _session.Add(key, item);

      public T Get(ulong key) => _session.GetById<T>(key);
      public void SaveChanges() => _session.SaveChanges();

      public void Dispose()
      {
         var disposable = _session as IDisposable;
         
         disposable?.Dispose();
      }
   }
}
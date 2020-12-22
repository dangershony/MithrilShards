using System;
using System.Collections.Generic;
using FASTER.core;

namespace Repository
{
   internal class PersistenceStoreFactory : IPersistenceStoreFactory, IDisposable
   {
      readonly IStorageConfiguration _configuration;

      IList<IDisposable> stores = new List<IDisposable>();

      public PersistenceStoreFactory(IStorageConfiguration configuration)
      {
         _configuration = configuration;
      }

      public IPersistenceStore CreateUlongKeyStore()
      {
         var log = Devices.CreateLogDevice( _configuration.StoragePath, recoverDevice: true);

         var store = new FasterKV<ulong, string>(1L << 20, new LogSettings
         {
            LogDevice = log
         });

         stores.Add(store);
         
         return new UlongStringPersistenceStore(store.For(new SimpleFunctions<ulong, string>())
            .NewSession<SimpleFunctions<ulong, string>>());
      }

      public void Dispose()
      {
         if (stores.Count <= 0) 
            return;
         
         foreach (IDisposable disposable in stores)
         {
            disposable.Dispose();
         }
      }
   }
}
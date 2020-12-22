using System;
using System.Collections.Generic;
using FASTER.core;

namespace Repository
{
   internal class PersistenceStoreFactory : IPersistenceStoreFactory, IDisposable
   {
      readonly IStorageConfiguration _configuration;

      readonly IList<IDisposable> _stores = new List<IDisposable>();

      public PersistenceStoreFactory(IStorageConfiguration configuration)
      {
         _configuration = configuration;
      }

      public IPersistenceSession CreateUlongKeyStore()
      {
         var log = Devices.CreateLogDevice( _configuration.LogStoragePath, recoverDevice: true);
         
         var objectLog = Devices.CreateLogDevice( _configuration.ObjectLogStoragePath, recoverDevice: true);

         var store = new FasterKV<ulong, string>(1L << 20,
            new LogSettings {LogDevice = log, ObjectLogDevice = objectLog});

         _stores.Add(store);
         
         return new UlongStringPersistenceStore(store.For(new SimpleFunctions<ulong, string>())
            .NewSession<SimpleFunctions<ulong, string>>());
      }

      public void Dispose()
      {
         if (_stores.Count <= 0) 
            return;
         
         foreach (IDisposable disposable in _stores)
         {
            disposable.Dispose();
         }
      }
   }
}
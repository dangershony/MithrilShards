using FASTER.core;

namespace Repository
{
   internal class PersistenceStoreFactory : IPersistenceStoreFactory<ulong>
   {
      readonly IStorageConfiguration _configuration;

      public PersistenceStoreFactory(IStorageConfiguration configuration)
      {
         _configuration = configuration;
      }

      public IPersistenceStore<ulong> CreateKeyStore() 
      {
         var log = Devices.CreateLogDevice( _configuration.LogStoragePath, recoverDevice: true);
         
         var objectLog = Devices.CreateLogDevice( _configuration.ObjectLogStoragePath, recoverDevice: true);

         var store = new FasterKV<ulong, string>(1L << 20,
            new LogSettings {LogDevice = log, ObjectLogDevice = objectLog});

         return new UlongStringPersistenceStore(store);
      }
   }
}
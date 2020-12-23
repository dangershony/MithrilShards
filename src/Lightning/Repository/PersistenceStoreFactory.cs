using FASTER.core;

namespace Repository
{
   internal class PersistenceStoreFactory : IPersistenceStoreFactory
   {
      readonly IStorageConfiguration _configuration;

      public PersistenceStoreFactory(IStorageConfiguration configuration)
      {
         _configuration = configuration;
      }

      public IPersistenceStore CreateKeyStore() 
      {
         var log = Devices.CreateLogDevice( _configuration.LogStoragePath, recoverDevice: true);
         
         var objectLog = Devices.CreateLogDevice( _configuration.ObjectLogStoragePath, recoverDevice: true);

         var store = new FasterKV<SpanByte, string>(1L << 5, 
            new LogSettings
            {
               LogDevice = log, 
               ObjectLogDevice = objectLog
            });

         return new SpanByteStringPersistenceStore(store);
      }
   }
}
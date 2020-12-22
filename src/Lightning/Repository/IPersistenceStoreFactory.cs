namespace Repository
{
   internal interface IPersistenceStoreFactory
   {
      IPersistenceStore CreateUlongKeyStore();
   }
}
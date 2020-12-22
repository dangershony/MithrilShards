namespace Repository
{
   internal interface IPersistenceStoreFactory
   {
      IPersistenceStore CreateKeyStore();
   }
}
namespace Repository
{
   internal interface IPersistenceStoreFactory
   {
      IPersistenceStore CreatePersistenceStore();
   }
}
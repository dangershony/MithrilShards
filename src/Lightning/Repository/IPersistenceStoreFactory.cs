namespace Repository
{
   internal interface IPersistenceStoreFactory
   {
      IPersistenceSession CreateUlongKeyStore();
   }
}
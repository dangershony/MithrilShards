namespace Repository
{
   internal interface IPersistenceStoreFactory <in TKey> where TKey : struct
   {
      IPersistenceStore<TKey> CreateKeyStore();
   }
}
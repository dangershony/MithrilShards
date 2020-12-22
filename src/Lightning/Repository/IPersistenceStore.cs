namespace Repository
{
   internal interface IPersistenceStore
   {
      T GetById<T>(ulong id);

      void Add<T>(ulong id, T item);
   }
}
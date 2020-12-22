namespace Repository
{
   internal interface IPersistenceSession
   {
      T GetById<T>(ulong id);

      void Add<T>(ulong id, T item);
   }
}
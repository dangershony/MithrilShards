namespace Repository
{
   public interface IRepository<T> where T : class
   {
      void Add(ulong key, T item);

      T Get(ulong key);
   }
}
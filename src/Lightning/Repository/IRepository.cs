using System.Threading.Tasks;

namespace Repository
{
   public interface IRepository<in TKey> 
      where TKey : struct
   {
      void Add<T>(TKey key, T item) where T : class;

      T Get<T>(TKey key) where T : class;

      ValueTask SaveChangesAsync();
   }
}
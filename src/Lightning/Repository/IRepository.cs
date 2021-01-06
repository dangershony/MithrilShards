using System.Threading.Tasks;

namespace Repository
{
   public interface IRepository<T> where T : class
   {
      void Add(byte[] key, T item);

      T Get(byte[] key);

      ValueTask SaveChangesAsync();
   }
}
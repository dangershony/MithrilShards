using System.Threading.Tasks;

namespace Repository
{
   internal interface IPersistenceStore
   {
      T GetById<T>(byte[] id) ;

      void Add<T>(byte[] id, T item)
         where T : class;
      
      ValueTask SaveChangesAsync();
   }
}
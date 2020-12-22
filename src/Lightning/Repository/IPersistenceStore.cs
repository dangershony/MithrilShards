using System;
using System.Threading.Tasks;

namespace Repository
{
   internal interface IPersistenceStore<in TKey> where TKey : struct
   {
      T GetById<T>(TKey id) ;

      void Add<T>(TKey id, T item)
         where T : class;
      
      ValueTask SaveChangesAsync();
   }
}
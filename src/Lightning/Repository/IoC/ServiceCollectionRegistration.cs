using Microsoft.Extensions.DependencyInjection;

namespace Repository.IoC
{
   public static  class ServiceCollectionRegistration
   {
      public static IServiceCollection AddRepositoryRegistrations(this IServiceCollection serviceCollection)
      {
         //serviceCollection.AddTransient<IPersistenceStore, UlongStringPersistenceStore>();
         serviceCollection.AddSingleton<IPersistenceStoreFactory<ulong>, PersistenceStoreFactory>();
         serviceCollection.AddSingleton<IStorageConfiguration, NodeListStorageConfiguration>();
         serviceCollection.AddScoped<IRepository<ulong>,Repository>();
         return serviceCollection;
      }
   }
}
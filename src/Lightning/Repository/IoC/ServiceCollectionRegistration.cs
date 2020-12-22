using Microsoft.Extensions.DependencyInjection;

namespace Repository.IoC
{
   public static  class ServiceCollectionRegistration
   {
      public static IServiceCollection AddRepositoryRegistrations(this IServiceCollection serviceCollection)
      {
         //serviceCollection.AddTransient<IPersistenceStore, UlongStringPersistenceStore>();
         serviceCollection.AddSingleton<IPersistenceStoreFactory, PersistenceStoreFactory>();
         serviceCollection.AddSingleton<IStorageConfiguration, NodeListStorageConfiguration>();
         serviceCollection.AddScoped(typeof(IRepository<>),typeof(Repository<>));
         return serviceCollection;
      }
   }
}
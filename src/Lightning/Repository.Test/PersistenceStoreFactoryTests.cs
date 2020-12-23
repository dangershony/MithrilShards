using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Repository.IoC;
using Xunit;

namespace Repository.Test
{
   public class PersistenceStoreFactoryTests
   {
      private PersistenceStoreFactory _factory;

      [Fact]
      public void FactoryCanCreateIPersistenceStoreWithDefaultStorageConfiguration()
      {
         var collection = new ServiceCollection()
            .AddRepositoryRegistrations();

         var builder = collection.BuildServiceProvider();

         _factory = builder.GetService(typeof(IPersistenceStoreFactory)) as PersistenceStoreFactory;

         var store = _factory?.CreateKeyStore();
         
         store.Should()
            .NotBeNull();
         
         (store as IDisposable)?.Dispose();
      }
      
      
   }
}
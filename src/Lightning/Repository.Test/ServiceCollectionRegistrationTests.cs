using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Repository.IoC;
using Xunit;

namespace Repository.Test
{
   public class ServiceCollectionRegistrationTests
   {
      ServiceCollection _collection = new ServiceCollection();

      [Fact]
      public void RepositoryIsGeneratedSuccessfully()
      {
         _collection.AddRepositoryRegistrations();

         var repository = _collection.BuildServiceProvider()
            .GetService(typeof(IRepository<Test>)) as IRepository<Test>;

         repository.Should().NotBeNull();
      }
      
      private class Test {}
   }
}
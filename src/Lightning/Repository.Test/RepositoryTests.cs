using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Repository.Test
{
   public class RepositoryTests
   {
      Repository<TestItem> _repository;

      public class TestItem
      {
         public int Number;
         public byte[] Bytes;
         public string String;
      }

      public static TestItem RandomNewTestItem()
      {
         var random = new Random();

         var testitem = new TestItem
         {
            Number = random.Next(int.MaxValue),
            Bytes = new byte[random.Next(ushort.MaxValue)],
            String = Guid.NewGuid().ToString()
         };
         
         random.NextBytes(testitem.Bytes);

         return testitem;
      }
         
      [Fact]
      public void RepositoryCanAddAndGetAnItem()
      {
         var testItem = RandomNewTestItem();

         _repository = new Repository<TestItem>(new PersistenceStoreFactory(new NodeListStorageConfiguration()));

         _repository.Add(testItem.Bytes, testItem);

         var lookupItem = _repository.Get(testItem.Bytes);

         lookupItem.Should()
            .BeEquivalentTo(testItem);
      }

      [Fact]
      public async Task RepositoryShouldSaveToFileOnSaveChanges()
      {
         var testItem = RandomNewTestItem();

         var testConfiguration = new TestConfiguration();
         
         ClearTestLogFiles();
         
         _repository = new Repository<TestItem>(new PersistenceStoreFactory(testConfiguration));

         _repository.Add(testItem.Bytes, testItem);

         await _repository.SaveChangesAsync().ConfigureAwait(false);

         _repository.Dispose();
            
         string[] fileList = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(),
            TestConfiguration.TEST_FILE_DIRECTORY_NAME));
         
         fileList.Contains(testConfiguration.LogStoragePath + ".0")
            .Should()
            .BeTrue();

         fileList.Contains(testConfiguration.ObjectLogStoragePath + ".0")
            .Should()
            .BeTrue();
      }

      static void ClearTestLogFiles()
      {
         string testFilesPath = Path.Combine(Directory.GetCurrentDirectory(), TestConfiguration.TEST_FILE_DIRECTORY_NAME);

         if (!Directory.Exists(testFilesPath))
            return;

         Directory.Delete(testFilesPath,true);
      }

      private class TestConfiguration : IStorageConfiguration
      {
         public const string  TEST_FILE_DIRECTORY_NAME = "TestRepo";
         public string LogStoragePath => Path.Combine(Directory.GetCurrentDirectory(), $"{TEST_FILE_DIRECTORY_NAME}\\TestFile.log"); 
         public string ObjectLogStoragePath => Path.Combine(Directory.GetCurrentDirectory(), $"{TEST_FILE_DIRECTORY_NAME}\\TestFileObj.log");
      }
   }
}
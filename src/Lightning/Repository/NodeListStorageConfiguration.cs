using System.IO;

namespace Repository
{
   internal class NodeListStorageConfiguration : IStorageConfiguration
   {
      public string StoragePath => Directory.GetCurrentDirectory() + nameof(NodeListStorageConfiguration) + ".log";
   }
}
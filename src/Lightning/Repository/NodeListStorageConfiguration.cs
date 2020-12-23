using System.IO;

namespace Repository
{
   internal class NodeListStorageConfiguration : IStorageConfiguration
   {
      const string _logSuffix = ".log";
      
      const string _objLogSuffix = "obj.log";
      public string LogStoragePath => Path.Combine(Directory.GetCurrentDirectory(),nameof(NodeListStorageConfiguration) + _logSuffix);
      public string ObjectLogStoragePath => Path.Combine(Directory.GetCurrentDirectory(),nameof(NodeListStorageConfiguration) + _objLogSuffix);
   }
}
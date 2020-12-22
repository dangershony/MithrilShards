using System.IO;

namespace Repository
{
   internal class NodeListStorageConfiguration : IStorageConfiguration
   {
      public string LogStoragePath => "C:\\Temp\\" + nameof(NodeListStorageConfiguration) + ".log";
      public string ObjectLogStoragePath => "C:\\Temp\\" + nameof(NodeListStorageConfiguration) + "Obj.log";
   }
}
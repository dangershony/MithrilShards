namespace Repository
{
   public interface IStorageConfiguration
   {
      string LogStoragePath { get; }
      
      string ObjectLogStoragePath { get; }
   }
}
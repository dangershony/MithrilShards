using System;
using Network.Protocol.Messages.Types;

namespace Network.Test.Utilities
{
   public static class RandomData
   {
      public static byte[] GetRandomByteArray(int length)
      {
         var random = new Random();
         byte[] arr = new byte[length];
         
         random.NextBytes(arr);

         return arr;
      }

      public static ChannelId RandomChannelId()
      {
         return (ChannelId)GetRandomByteArray(32);
      }
   }
}
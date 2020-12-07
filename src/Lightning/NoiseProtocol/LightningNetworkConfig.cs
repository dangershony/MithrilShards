using System.Text;

namespace NoiseProtocol
{
   public static class LightningNetworkConfig
   {
      public const string PROTOCOL_NAME = "Noise_XK_secp256k1_ChaChaPoly_SHA256";
      private const string PROLUGE = "lightning";

      public static byte[] ProtocolNameByteArray()
      {
         var byteArray = new byte[PROTOCOL_NAME.Length];
         Encoding.ASCII.GetBytes(PROTOCOL_NAME, 0, PROTOCOL_NAME.Length, byteArray, 0);
         return byteArray;
      }
      
      /// <summary>
      ///
      /// </summary>
      /// <returns></returns>
      public static byte[] ProlugeByteArray()
      {
         var byteArray = new byte[PROLUGE.Length];
         Encoding.ASCII.GetBytes(PROLUGE, 0, PROLUGE.Length, byteArray, 0);
         return byteArray;
      }

      public static readonly byte[] NoiseProtocolVersionPrefix = {0x00};

      public static readonly ulong NumberOfNonceBeforeKeyRecycle = 1000;
   }
}
using System.Text;

namespace Network.Protocol.Transport.Noise
{
    /// <summary>
    /// 
    /// </summary>
    public static class LightningNetworkConfig
    {
        public const string ProtocolName = "Noise_XK_secp256k1_ChaChaPoly_SHA256";
        private const string Proluge = "lightning";
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static byte[] ProlugeByteArray()
        {
            var byteArray = new byte[Proluge.Length];
            Encoding.ASCII.GetBytes(Proluge, 0, Proluge.Length, byteArray, 0);
            return byteArray;
        }
            
    }
}
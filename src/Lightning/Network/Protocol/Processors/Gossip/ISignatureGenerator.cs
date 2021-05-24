using Bitcoin.Primitives.Fundamental;

namespace Network.Protocol.Processors.Gossip
{
   public interface ISignatureGenerator
   {
      CompressedSignature Sign(PrivateKey key, byte[] hash);
   }

   public class SignatureGenerator : ISignatureGenerator
   {
      public CompressedSignature Sign(PrivateKey key, byte[] hash) => throw new System.NotImplementedException();
      //(CompressedSignature) k.SignCompact(new uint256(hash));
   }
}
using Bitcoin.Primitives.Types;

namespace Protocol
{
   public interface ITransactionHashCalculator
   {
      UInt256 ComputeHash(Transaction transaction, int protocolVersion);

      UInt256 ComputeWitnessHash(Transaction transaction, int protocolVersion);
   }
}
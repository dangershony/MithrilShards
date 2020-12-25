namespace Bitcoin.Primitives.Types
{
   /// <summary>
   /// transaction output (tx_out).
   /// </summary>
   public class TransactionWitness
   {
      /// <summary>
      /// The witness raw data
      /// </summary>
      public TransactionWitnessComponent[]? Components { get; set; }
   }
}
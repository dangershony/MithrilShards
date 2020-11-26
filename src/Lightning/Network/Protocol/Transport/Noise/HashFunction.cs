using System;

namespace Network.Protocol.Transport.Noise
{
   /// <summary>
   /// Constants representing the available hash functions.
   /// </summary>
   public sealed class HashFunction
   {
      /// <summary>
      /// SHA-256 from <see href="https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf">FIPS 180-4</see>.
      /// </summary>
      public static readonly HashFunction Sha256 = new HashFunction("SHA256");

      private readonly string _name;

      private HashFunction(string name) => _name = name;

      /// <summary>
      /// Returns a string that represents the current object.
      /// </summary>
      /// <returns>The name of the current hash function.</returns>
      public override string ToString() => _name;

      internal static HashFunction Parse(ReadOnlySpan<char> s)
      {
         switch (s)
         {
            case var _ when s.SequenceEqual(Sha256._name.AsSpan()): return Sha256;
            default: throw new ArgumentException("Unknown hash function.", nameof(s));
         }
      }
   }
}
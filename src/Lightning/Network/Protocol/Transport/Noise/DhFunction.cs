using System;

namespace Network.Protocol.Transport.Noise
{
   /// <summary>
   /// Constants representing the available DH functions.
   /// </summary>
   public sealed class DhFunction
   {
      /// <summary>
      /// Bitcoin elliptic curve
      /// </summary>
      public static readonly DhFunction CurveSecp256K1 = new DhFunction("secp256k1");

      private readonly string _name;

      private DhFunction(string name) => _name = name;

      /// <summary>
      /// Returns a string that represents the current object.
      /// </summary>
      /// <returns>The name of the current DH function.</returns>
      public override string ToString() => _name;

      internal static DhFunction Parse(ReadOnlySpan<char> s)
      {
         switch (s)
         {
            case var _ when s.SequenceEqual(CurveSecp256K1._name.AsSpan()): return CurveSecp256K1;
            default: throw new ArgumentException("Unknown DH function.", nameof(s));
         }
      }
   }
}
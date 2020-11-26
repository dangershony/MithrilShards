using System;

namespace Network.Protocol.Transport.Noise
{
   /// <summary>
   /// Constants representing the available cipher functions.
   /// </summary>
   public sealed class CipherFunction
   {
      /// <summary>
      /// AES256 with GCM from NIST Special Publication
      /// <see href="https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38d.pdf">800-38D</see>.
      /// </summary>
      public static readonly CipherFunction AesGcm = new CipherFunction("AESGCM");

      /// <summary>
      /// AEAD_CHACHA20_POLY1305 from <see href="https://tools.ietf.org/html/rfc7539">RFC 7539</see>.
      /// </summary>
      public static readonly CipherFunction ChaChaPoly = new CipherFunction("ChaChaPoly");

      private readonly string _name;

      private CipherFunction(string name) => _name = name;

      /// <summary>
      /// Returns a string that represents the current object.
      /// </summary>
      /// <returns>The name of the current cipher function.</returns>
      public override string ToString() => _name;

      internal static CipherFunction Parse(ReadOnlySpan<char> s)
      {
         switch (s)
         {
            case var _ when s.SequenceEqual(AesGcm._name.AsSpan()): return AesGcm;
            case var _ when s.SequenceEqual(ChaChaPoly._name.AsSpan()): return ChaChaPoly;
            default: throw new ArgumentException("Unknown cipher function.", nameof(s));
         }
      }
   }
}
using System;

namespace Network.Protocol.Transport.Noise
{
   /// <summary>
   /// A Diffie-Hellman private/public key pair.
   /// </summary>
   public sealed class KeyPair : IDisposable
   {
      private static readonly CurveSecp256K1 _dh = new CurveSecp256K1();
      private readonly byte[] _privateKey;
      private readonly byte[] _publicKey;
      private bool _disposed;

      /// <summary>
      /// Initializes a new instance of the <see cref="KeyPair"/> class.
      /// </summary>
      /// <param name="privateKey">The private key.</param>
      /// <param name="publicKey">The public key.</param>
      /// <exception cref="ArgumentNullException">
      /// Thrown if the <paramref name="privateKey"/> or the <paramref name="publicKey"/> is null.
      /// </exception>
      /// <exception cref="ArgumentException">
      /// Thrown if the lengths of the <paramref name="privateKey"/> or the <paramref name="publicKey"/> are invalid.
      /// </exception>
      internal KeyPair(byte[] privateKey, byte[] publicKey)
      {
         Exceptions.ThrowIfNull(privateKey, nameof(privateKey));
         Exceptions.ThrowIfNull(publicKey, nameof(publicKey));

         if (privateKey.Length != 32)
         {
            throw new ArgumentException("Private key must have length of 32 bytes.", nameof(privateKey));
         }

         if (publicKey.Length != 33)
         {
            throw new ArgumentException("Public key must have length of 33 bytes.", nameof(publicKey));
         }

         _privateKey = privateKey;
         _publicKey = publicKey;
      }

      /// <summary>
      /// Generates a new Diffie-Hellman key pair.
      /// </summary>
      /// <returns>A randomly generated private key and its corresponding public key.</returns>
      public static KeyPair Generate()
      {
         return _dh.GenerateKeyPair();
      }

      /// <summary>
      /// Gets the private key.
      /// </summary>
      /// <exception cref="ObjectDisposedException">
      /// Thrown if the current instance has already been disposed.
      /// </exception>
      public byte[] PrivateKey
      {
         get
         {
            Exceptions.ThrowIfDisposed(_disposed, nameof(KeyPair));
            return _privateKey;
         }
      }

      /// <summary>
      /// Gets the public key.
      /// </summary>
      /// <exception cref="ObjectDisposedException">
      /// Thrown if the current instance has already been disposed.
      /// </exception>
      public byte[] PublicKey
      {
         get
         {
            Exceptions.ThrowIfDisposed(_disposed, nameof(KeyPair));
            return _publicKey;
         }
      }

      /// <summary>
      /// Erases the key pair from the memory.
      /// </summary>
      public void Dispose()
      {
         if (!_disposed)
         {
            Utilities.ZeroMemory(_privateKey);
            _disposed = true;
         }
      }
   }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bitcoin.Primitives.Fundamental;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Secp256k1;

namespace Protocol.Channels
{
   public class KeyDerivation
   {
      private readonly ILogger<KeyDerivation> _logger;

      public KeyDerivation(ILogger<KeyDerivation> logger)
      {
         _logger = logger;
      }

      public PublicKey PublicKeyFromPrivateKey(PrivateKey privateKey)
      {
         if (ECPrivKey.TryCreate(privateKey, Context.Instance, out ECPrivKey? ecprvkey))
         {
            if (ecprvkey != null)
            {
               ECPubKey ecpubkey = ecprvkey.CreatePubKey();
               Span<byte> pub = stackalloc byte[33];
               ecpubkey.WriteToSpan(true, pub, out _);
               return new PublicKey(pub.ToArray());
            }
         }

         return null;
      }

      public PublicKey DerivePublickey(PublicKey basepoint, PublicKey perCommitmentPoint)
      {
         // TODO: pubkey = basepoint + SHA256(per_commitment_point || basepoint) * G

         Span<byte> toHash = stackalloc byte[PublicKey.LENGTH * 2];
         perCommitmentPoint.GetSpan().CopyTo(toHash);
         basepoint.GetSpan().CopyTo(toHash.Slice(PublicKey.LENGTH));
         byte[] hashed = NBitcoin.Crypto.Hashes.SHA256(toHash);

         if (ECPubKey.TryCreate(basepoint, Context.Instance, out _, out ECPubKey? ecpubkey))
         {
            if (ecpubkey.TryAddTweak(hashed.AsSpan(), out ECPubKey? ecpubkeytweaked))
            {
               if (ecpubkeytweaked != null)
               {
                  Span<byte> pub = stackalloc byte[33];
                  ecpubkeytweaked.WriteToSpan(true, pub, out _);
                  return new PublicKey(pub.ToArray());
               }
            }
         }

         return null;
      }

      public PrivateKey DerivePrivatekey(PublicKey basepoint, PrivateKey basepointSecret, PublicKey perCommitmentPoint)
      {
         // TODO: privkey = basepoint_secret + SHA256(per_commitment_point || basepoint)

         Span<byte> toHash = stackalloc byte[PublicKey.LENGTH * 2];
         perCommitmentPoint.GetSpan().CopyTo(toHash);
         basepoint.GetSpan().CopyTo(toHash.Slice(PublicKey.LENGTH));
         byte[] hashed = NBitcoin.Crypto.Hashes.SHA256(toHash);

         if (ECPrivKey.TryCreate(basepointSecret, Context.Instance, out ECPrivKey? ecprvkey))
         {
            if (ecprvkey.TryTweakAdd(hashed.AsSpan(), out ECPrivKey? ecprvkeytweaked))
            {
               if (ecprvkeytweaked != null)
               {
                  Span<byte> prv = stackalloc byte[32];
                  ecprvkeytweaked.WriteToSpan(prv);
                  return new PrivateKey(prv.ToArray());
               }
            }
         }

         return null;
      }

      public PublicKey DeriveRevocationPublicKey(PublicKey basepoint, PublicKey perCommitmentPoint)
      {
         // TODO: revocationpubkey = revocation_basepoint * SHA256(revocation_basepoint || per_commitment_point) + per_commitment_point * SHA256(per_commitment_point || revocation_basepoint)

         Span<byte> toHash1 = stackalloc byte[PublicKey.LENGTH * 2];
         basepoint.GetSpan().CopyTo(toHash1);
         perCommitmentPoint.GetSpan().CopyTo(toHash1.Slice(PublicKey.LENGTH));
         byte[] hashed1 = NBitcoin.Crypto.Hashes.SHA256(toHash1);

         ECPubKey? revocationBasepointTweaked = null;
         if (ECPubKey.TryCreate(basepoint, Context.Instance, out _, out ECPubKey? ecbasepoint))
         {
            if (ecbasepoint.TryMultTweak(hashed1.AsSpan(), out ECPubKey? ecpubkeytweaked))
            {
               if (ecpubkeytweaked != null)
               {
                  revocationBasepointTweaked = ecpubkeytweaked;
               }
            }
         }

         Span<byte> toHash2 = stackalloc byte[PublicKey.LENGTH * 2];
         perCommitmentPoint.GetSpan().CopyTo(toHash2);
         basepoint.GetSpan().CopyTo(toHash2.Slice(PublicKey.LENGTH));
         byte[] hashed2 = NBitcoin.Crypto.Hashes.SHA256(toHash2);

         ECPubKey? perCommitmentPointTweaked = null;
         if (ECPubKey.TryCreate(perCommitmentPoint, Context.Instance, out _, out ECPubKey? ecperCommitmentPoint))
         {
            if (ecperCommitmentPoint.TryMultTweak(hashed2.AsSpan(), out ECPubKey? ecperCommitmentPointtweaked))
            {
               if (ecperCommitmentPointtweaked != null)
               {
                  perCommitmentPointTweaked = ecperCommitmentPointtweaked;
               }
            }
         }

         if (revocationBasepointTweaked != null && perCommitmentPointTweaked != null)
         {
            var keys = new ECPubKey[] { revocationBasepointTweaked, perCommitmentPointTweaked };

            if (ECPubKey.TryCombine(Context.Instance, keys, out ECPubKey? revocationpubkey))
            {
               if (revocationpubkey != null)
               {
                  Span<byte> pub = stackalloc byte[33];
                  revocationpubkey.WriteToSpan(true, pub, out _);
                  return new PublicKey(pub.ToArray());
               }
            }
         }

         return null;
      }

      public PrivateKey DeriveRevocationPrivatekey(PublicKey basepoint, PrivateKey basepointSecret, PrivateKey perCommitmentSecret, PublicKey perCommitmentPoint)
      {
         // TODO: revocationpubkey = revocation_basepoint * SHA256(revocation_basepoint || per_commitment_point) + per_commitment_point * SHA256(per_commitment_point || revocation_basepoint)

         Span<byte> toHash1 = stackalloc byte[PublicKey.LENGTH * 2];
         basepoint.GetSpan().CopyTo(toHash1);
         perCommitmentPoint.GetSpan().CopyTo(toHash1.Slice(PublicKey.LENGTH));
         byte[] hashed1 = NBitcoin.Crypto.Hashes.SHA256(toHash1);

         ECPrivKey? revocationBasepointSecretTweaked = null;
         if (ECPrivKey.TryCreate(basepointSecret, Context.Instance, out ECPrivKey? ecbasepointsecret))
         {
            if (ecbasepointsecret.TryTweakMul(hashed1.AsSpan(), out ECPrivKey? ecprivtweaked))
            {
               if (ecprivtweaked != null)
               {
                  revocationBasepointSecretTweaked = ecprivtweaked;
               }
            }
         }

         Span<byte> toHash2 = stackalloc byte[PublicKey.LENGTH * 2];
         perCommitmentPoint.GetSpan().CopyTo(toHash2);
         basepoint.GetSpan().CopyTo(toHash2.Slice(PublicKey.LENGTH));
         byte[] hashed2 = NBitcoin.Crypto.Hashes.SHA256(toHash2);

         ECPrivKey? perCommitmentSecretTweaked = null;
         if (ECPrivKey.TryCreate(perCommitmentSecret, Context.Instance, out ECPrivKey? ecpercommitmentsecret))
         {
            if (ecpercommitmentsecret.TryTweakMul(hashed2.AsSpan(), out ECPrivKey? ecprivtweaked))
            {
               if (ecprivtweaked != null)
               {
                  perCommitmentSecretTweaked = ecprivtweaked;
               }
            }
         }

         if (revocationBasepointSecretTweaked != null && perCommitmentSecretTweaked != null)
         {
            Span<byte> prvtpadd = stackalloc byte[32];
            perCommitmentSecretTweaked.WriteToSpan(prvtpadd);

            if (revocationBasepointSecretTweaked.TryTweakAdd(prvtpadd, out ECPrivKey? revocationprvkey))
            {
               if (revocationprvkey != null)
               {
                  Span<byte> prv = stackalloc byte[32];
                  revocationprvkey.WriteToSpan(prv);
                  return new PrivateKey(prv.ToArray());
               }
            }
         }

         return null;
      }
   }
}
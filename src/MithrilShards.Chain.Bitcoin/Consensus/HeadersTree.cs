﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Threading;

namespace MithrilShards.Chain.Bitcoin.Consensus
{
   /// <summary>
   /// A thread safe headers tree that tracks current best chain and known headers on forks.
   /// A List holds the best chain headers sorted by height starting from genesis and a dictionary holds relationships between known block
   /// hashes and their previous hash and the height where that hash was/is on the best chain.
   /// </summary>
   /// <remarks>
   /// Internally it uses <see cref="ReaderWriterLockSlim"/> to ensure thread safety on every get and set method.
   /// </remarks>
   public class HeadersTree
   {
      private const int INITIAL_ITEMS_ALLOCATED = 16 ^ 2; //this parameter may go into settings, better to be multiple of 2

      private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

      private readonly ILogger<HeadersTree> logger;
      private readonly IChainDefinition chainDefinition;

      /// <summary>
      /// Known set of hashes, both on forks and on best chains.
      /// Those who are on the best chain, can be found in the <see cref="bestChain"/> list.
      /// </summary>
      private readonly Dictionary<UInt256, HeaderNode> knownHeaders = new Dictionary<UInt256, HeaderNode>(INITIAL_ITEMS_ALLOCATED);

      /// <summary>
      /// The best chain of hashes sorted by height.
      /// If a block hash is in this list, it means it's in the main chain and can be sent to other peers.
      /// </summary>
      private readonly List<UInt256> bestChain = new List<UInt256>(INITIAL_ITEMS_ALLOCATED);

      /// <summary>
      /// The genesis node.
      /// </summary>
      private readonly HeaderNode genesisNode;

      public UInt256 Genesis => this.chainDefinition.Genesis;

      private int height;
      public int Height => this.height;

      public UInt256 Tip
      {
         get
         {
            using (new ReadLock(this.@lock)) return this.bestChain[this.height];
         }
      }

      public HeadersTree(ILogger<HeadersTree> logger, IChainDefinition chainDefinition)
      {
         this.logger = logger;
         this.chainDefinition = chainDefinition ?? throw new ArgumentNullException(nameof(chainDefinition));

         this.genesisNode = new HeaderNode(0, this.chainDefinition.Genesis, null);

         this.ResetToGenesis();
      }

      private void ResetToGenesis()
      {
         using (new WriteLock(this.@lock))
         {
            this.height = 0;
            this.bestChain.Clear();
            this.knownHeaders.Clear();

            this.bestChain.Add(this.chainDefinition.Genesis);
            this.knownHeaders.Add(this.chainDefinition.Genesis, this.genesisNode);
         }
      }

      public bool Contains(UInt256 blockHash)
      {
         using (new ReadLock(this.@lock))
         {
            return this.knownHeaders.ContainsKey(blockHash);
         }
      }

      /// <summary>
      /// Tries to get the height of an hash.
      /// </summary>
      /// <param name="blockHash">The block hash.</param>
      /// <param name="onlyBestChain">if set to <c>true</c> check only headers that belong to the best chain.</param>
      /// <param name="height">The height if found, -1 otherwise.</param>
      /// <returns><c>true</c> if the result has been found, <see langword="false"/> otherwise.</returns>
      public bool TryGetNode(UInt256 blockHash, bool onlyBestChain, [MaybeNullWhen(false)] out HeaderNode node)
      {
         using (new ReadLock(this.@lock))
         {
            return onlyBestChain ? this.knownHeaders.TryGetValue(blockHash, out node!) : this.TryGetNodeOnBestChainNoLock(blockHash, out node!);
         }
      }

      /// <summary>
      /// Tries to get the node on best chain at a specified height.
      /// </summary>
      /// <param name="height">The height.</param>
      /// <returns></returns>
      public bool TryGetNodeOnBestChain(int height, [MaybeNullWhen(false)] out HeaderNode node)
      {
         using (new ReadLock(this.@lock))
         {
            if (height > this.height)
            {
               node = null!;
               return false;
            }

            node = this.GetHeaderNodeNoLock(height);
            return true;
         }
      }

      /// <summary>
      /// Tries the get hash of a block at the specified height.
      /// </summary>
      /// <param name="height">The height.</param>
      /// <param name="blockHash">The block hash.</param>
      /// <returns></returns>
      public bool TryGetHash(int height, [MaybeNullWhen(false)]out UInt256 blockHash)
      {
         using (new ReadLock(this.@lock))
         {
            if (height > this.height || height < 0)
            {
               blockHash = null!;
               return false;
            }

            blockHash = this.bestChain[height];
         }

         return true;
      }

      /// <summary>
      /// Set a new tip in the chain
      /// </summary>
      /// <param name="newTip">The new tip</param>
      /// <param name="newTipPreviousHash">The block hash before the new tip</param>
      public ConnectHeaderResult TrySetTip(in UInt256 newTip, in UInt256? newTipPreviousHash)
      {
         using (new WriteLock(this.@lock))
         {
            return this.TrySetTipNoLock(newTip, newTipPreviousHash);
         }
      }

      public BlockLocator GetTipLocator()
      {
         using (new ReadLock(this.@lock))
         {
            return this.GetLocatorNoLock(this.height);
         }
      }

      public BlockLocator? GetLocator(int height)
      {
         using (new ReadLock(this.@lock))
         {
            return (height > this.height || height < 0) ? null : this.GetLocatorNoLock(height);
         }
      }

      public BlockLocator? GetLocator(UInt256 blockHash)
      {
         using (new ReadLock(this.@lock))
         {
            return (!this.knownHeaders.TryGetValue(blockHash, out HeaderNode? node)) ? null : this.GetLocatorNoLock(node.Height);
         }
      }

      /// <summary>
      /// Performing code to generate a <see cref="BlockLocator"/>.
      /// </summary>
      /// <param name="height">The height block locator starts from.</param>
      /// <returns></returns>
      private BlockLocator GetLocatorNoLock(int height)
      {
         int itemsToAdd = height <= 10 ? (height + 1) : (10 + (int)Math.Ceiling(Math.Log2(height)));
         UInt256[] hashes = new UInt256[itemsToAdd];

         int index = 0;
         while (index < 10 && height > 0)
         {
            hashes[index++] = this.bestChain[height--];
         }

         int step = 1;
         while (height > 0)
         {
            hashes[index++] = this.bestChain[height];
            step *= 2;
            height -= step;
         }
         hashes[itemsToAdd - 1] = this.Genesis;

         return new BlockLocator { BlockLocatorHashes = hashes };
      }

      /// <summary>
      /// Returns the first common block between our known best chain and the block locator.
      /// </summary>
      /// <param name="hashes">Hash to search for</param>
      /// <returns>First found block or genesis</returns>
      public HeaderNode GetHighestNodeInBestChainFromBlockLocator(BlockLocator blockLocator)
      {
         if (blockLocator == null) throw new ArgumentNullException(nameof(blockLocator));

         using (new ReadLock(this.@lock))
         {
            foreach (UInt256 hash in blockLocator.BlockLocatorHashes)
            {
               // ensure that any header we have in common belong to the main chain.
               if (this.TryGetNodeOnBestChainNoLock(hash, out HeaderNode? node))
               {
                  return node;
               }
            }
         }

         return this.genesisNode;
      }

      /// <summary>
      /// Gets the current tip header node.
      /// </summary>
      /// <returns></returns>
      public HeaderNode GetTipHeaderNode()
      {
         using (new ReadLock(this.@lock))
         {
            return this.GetHeaderNodeNoLock(this.height);
         }
      }

      /// <summary>
      /// Determines whether the specified hash is a known hash.
      /// May be present on best chain or on a fork.
      /// </summary>
      /// <param name="hash">The hash.</param>
      /// <returns>
      ///   <c>true</c> if the specified hash is known; otherwise, <c>false</c>.
      /// </returns>
      public bool IsKnown(UInt256? hash)
      {
         if (hash == null) return false;

         using (new ReadLock(this.@lock))
         {
            return this.knownHeaders.ContainsKey(hash);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private HeaderNode GetHeaderNodeNoLock(int height)
      {
         return new HeaderNode(height, this.bestChain[height], height == 0 ? null : this.bestChain[height - 1]);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private HeaderNode GetHeaderNodeNoLock(int height, UInt256 currentHeader)
      {
         return new HeaderNode(height, currentHeader, height == 0 ? null : this.bestChain[height - 1]);
      }

      /// <summary>
      /// Tries to get the height of an hash on the best chain (no lock).
      /// </summary>
      /// <param name="blockHash">The block hash.</param>
      /// <param name="height">The height.</param>
      /// <returns></returns>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private bool TryGetNodeOnBestChainNoLock(UInt256 blockHash, [MaybeNullWhen(false)] out HeaderNode node)
      {
         if (this.knownHeaders.TryGetValue(blockHash, out node!) && this.height > node.Height && this.bestChain[node.Height] == blockHash)
         {
            return true;
         }
         else
         {
            node = null!;
            return false;
         }
      }

      private ConnectHeaderResult TrySetTipNoLock(in UInt256 newTip, in UInt256? newTipPreviousHash)
      {
         if (newTip == this.Genesis)
         {
            if (newTipPreviousHash != null)
            {
               throw new ArgumentException("Genesis block should not have previous block", nameof(newTipPreviousHash));
            }

            this.ResetToGenesis();

            return ConnectHeaderResult.ResettedToGenesis;
         }
         else
         {
            if (newTipPreviousHash == null)
            {
               throw new ArgumentNullException(nameof(newTipPreviousHash), "Previous hash null allowed only on genesis block.");
            }
         }

         // check if the tip we want to set is already into our chain
         if (this.knownHeaders.TryGetValue(newTip, out HeaderNode? tipNode))
         {
            if (this.bestChain[tipNode.Height - 1] != newTipPreviousHash)
            {
               throw new ArgumentException("The new tip is already inserted with a different previous block.");
            }

            this.logger.LogDebug("The tip we want to set is already in our headers chain.");
         }

         // ensures tip previous header is present.
         if (!this.knownHeaders.TryGetValue(newTipPreviousHash, out HeaderNode? newTipPreviousHeader))
         {
            //previous tip header not found, abort.
            this.logger.LogDebug("New Tip previous header not found, can't connect headers.");
            return ConnectHeaderResult.MissingPreviousHeader;
         }

         // if newTipPreviousHash isn't current tip, means we need to rollback
         bool needRewind = this.height != newTipPreviousHeader.Height;
         if (needRewind)
         {
            int rollingBackHeight = this.height;
            while (rollingBackHeight > newTipPreviousHeader.Height)
            {
               this.knownHeaders.Remove(this.bestChain[rollingBackHeight]);
               this.bestChain.RemoveAt(rollingBackHeight);
               rollingBackHeight--;
            }
            this.height = rollingBackHeight;
         }

         // now we can put the tip on top of our chain.
         this.height++;
         this.bestChain.Add(newTip); //[this.height] = newTip;
         this.knownHeaders.Add(newTip, new HeaderNode(this.height, newTip, newTipPreviousHash));

         return needRewind ? ConnectHeaderResult.Rewinded : ConnectHeaderResult.Connected;
      }
   }
}
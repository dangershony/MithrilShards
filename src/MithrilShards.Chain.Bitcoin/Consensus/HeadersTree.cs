﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.ChainDefinitions;
using MithrilShards.Chain.Bitcoin.Consensus.Validation;
using MithrilShards.Chain.Bitcoin.Consensus.Validation.Header;
using MithrilShards.Chain.Bitcoin.DataTypes;
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

      private readonly ReaderWriterLockSlim theLock = new ReaderWriterLockSlim();

      private readonly ILogger<HeadersTree> logger;
      private readonly IConsensusParameters consensusParameters;
      readonly IBlockHeaderRepository blockHeaderRepository;

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

      public UInt256 Genesis => this.consensusParameters.Genesis;

      private int height;
      public int Height => this.height;

      public HeadersTree(ILogger<HeadersTree> logger,
                         IConsensusParameters consensusParameters,
                         IBlockHeaderRepository blockHeaderRepository)
      {
         this.logger = logger;
         this.consensusParameters = consensusParameters ?? throw new ArgumentNullException(nameof(consensusParameters));
         this.blockHeaderRepository = blockHeaderRepository;

         this.genesisNode = new HeaderNode(this.consensusParameters.GenesisHeader);

         this.ResetToGenesis();
      }

      private void ResetToGenesis()
      {
         using (new WriteLock(this.theLock))
         {
            this.height = 0;
            this.bestChain.Clear();
            this.knownHeaders.Clear();

            this.bestChain.Add(this.Genesis);
            this.knownHeaders.Add(this.Genesis, this.genesisNode);
         }
      }

      public bool Contains(UInt256 blockHash)
      {
         using (new ReadLock(this.theLock))
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
         using (new ReadLock(this.theLock))
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
         using (new ReadLock(this.theLock))
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
      public bool TryGetHash(int height, [MaybeNullWhen(false)] out UInt256 blockHash)
      {
         using (new ReadLock(this.theLock))
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


      public HeaderNode Add(in BlockHeader newBlockHeader)
      {
         UInt256 newHash = newBlockHeader.Hash!;
         UInt256? previousHash = newBlockHeader.PreviousBlockHash;
         HeaderNode? previousHeader;

         using (new WriteLock(this.theLock))
         {
            // check if the header we want to add has been already added
            if (this.knownHeaders.TryGetValue(newHash, out HeaderNode? headerAlredyAdded))
            {
               return headerAlredyAdded;
            }

            if (previousHash == null)
            {
               previousHeader = this.genesisNode;
            }
            else if (!this.knownHeaders.TryGetValue(previousHash, out previousHeader))
            {
               throw new Exception("Cannot add an header without a known previous header");
            }

            var newHeader = new HeaderNode(newBlockHeader, previousHeader);
            this.knownHeaders.Add(newHash, newHeader);

            //check if we are extending the tip
            if (this.bestChain[this.height] == previousHeader.Hash)
            {
               this.height++;
               this.bestChain.Add(newHash);
            }
            else
            {
               // TODO
               // need to rewind
            }

            return newHeader;
         }
      }

      ///// <summary>
      ///// Set a new tip in the chain
      ///// </summary>
      ///// <param name="newTip">The new tip</param>
      ///// <param name="newTipPreviousHash">The block hash before the new tip</param>
      //public ConnectHeaderResult TrySetTip(in BlockHeader newTip, ref BlockValidationState validationState)
      //{
      //   UInt256 newTipHash = newTip.Hash!;
      //   UInt256? newTipPreviousHash = newTip.PreviousBlockHash;

      //   using (new WriteLock(this.theLock))
      //   {
      //      // check if the tip we want to set is already into our chain
      //      if (this.knownHeaders.TryGetValue(newTipHash, out HeaderNode? tipNode))
      //      {
      //         if (tipNode.Validity.HasFlag(HeaderValidityStatuses.FailedMask))
      //         {
      //            validationState.Invalid(BlockValidationFailureContext.BlockCachedInvalid, "duplicate", "block marked as invalid");
      //            return this.ValidationFailure(validationState);
      //         }
      //      }


      //      continue L3612 validation.cpp

      //      // if newTipPreviousHash isn't current tip, means we need to rollback
      //      bool needRewind = this.height != newTipPreviousHeader.Height;
      //      if (needRewind)
      //      {
      //         int rollingBackHeight = this.height;
      //         while (rollingBackHeight > newTipPreviousHeader.Height)
      //         {
      //            this.knownHeaders.Remove(this.bestChain[rollingBackHeight]);
      //            this.bestChain.RemoveAt(rollingBackHeight);
      //            rollingBackHeight--;
      //         }
      //         this.height = rollingBackHeight;
      //      }

      //      // now we can put the tip on top of our chain.
      //      this.height++;
      //      this.bestChain.Add(newTipHash);
      //      this.knownHeaders.Add(newTipHash, new HeaderNode(this.height, newTipHash, newTipPreviousHash));
      //      this.blockHeaderRepository.TryAdd(newTip);

      //      return needRewind ? ConnectHeaderResult.Rewinded : ConnectHeaderResult.Connected;
      //   }
      //}

      public BlockLocator GetTipLocator()
      {
         using (new ReadLock(this.theLock))
         {
            return this.GetLocatorNoLock(this.height);
         }
      }


      /// <summary>
      /// Gets the full block header tip.
      /// </summary>
      /// <returns></returns>
      public BlockHeader GetTipAsBlockHeader()
      {
         using (new ReadLock(this.theLock))
         {
            if (!this.blockHeaderRepository.TryGet(this.bestChain[this.height], out BlockHeader? header))
            {
               ThrowHelper.ThrowBlockHeaderRepositoryException($"Unexpected error, cannot fetch the tip at height {this.height}.");
            }

            return header!;
         }
      }

      /// <summary>
      /// Gets the <see cref="BlockHeader" /> referenced by the <paramref name="headerNode" />.
      /// </summary>
      /// <param name="headerNode">The header node.</param>
      /// <returns></returns>
      public bool TryGetBlockHeader(HeaderNode? headerNode, [MaybeNullWhen(false)] out BlockHeader blockHeader)
      {
         if (headerNode == null)
         {
            blockHeader = null!;
            return false;
         }

         using (new ReadLock(this.theLock))
         {
            return this.blockHeaderRepository.TryGet(headerNode.Hash, out blockHeader!);
         }
      }

      public BlockLocator? GetLocator(int height)
      {
         using (new ReadLock(this.theLock))
         {
            return (height > this.height || height < 0) ? null : this.GetLocatorNoLock(height);
         }
      }

      public BlockLocator? GetLocator(UInt256 blockHash)
      {
         using (new ReadLock(this.theLock))
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

         using (new ReadLock(this.theLock))
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
      public HeaderNode GetTip()
      {
         using (new ReadLock(this.theLock))
         {
            return this.GetHeaderNodeNoLock(this.height);
         }
      }

      public bool IsInBestChain(HeaderNode? headerNode)
      {
         if (headerNode == null) return false;

         using (new ReadLock(this.theLock))
         {
            int headerHeight = headerNode.Height;
            return this.bestChain.Count < headerHeight && this.bestChain[headerHeight] == headerNode.Hash;
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

         using (new ReadLock(this.theLock))
         {
            return this.knownHeaders.ContainsKey(hash);
         }
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      private HeaderNode GetHeaderNodeNoLock(int height)
      {
         return this.knownHeaders[this.bestChain[height]];
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

      /// <summary>
      /// Logs the validation failure reason and return a <see cref="ConnectHeaderResult.Invalid"/>.
      /// </summary>
      /// <param name="validationState">Validation state containing failing reason.</param>
      /// <returns></returns>
      private ConnectHeaderResult ValidationFailure(BlockValidationState validationState)
      {
         this.logger.LogDebug("Header validation failure: {0}", validationState.ToString());
         return ConnectHeaderResult.Invalid;
      }
   }
}

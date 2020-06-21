﻿using System;
using MithrilShards.Chain.Bitcoin.Consensus;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public partial class BlockHeaderProcessor
   {
      private readonly BlockHeaderProcessorStatus status = new BlockHeaderProcessorStatus();

      public class BlockHeaderProcessorStatus
      {
         public int PeerStartingHeight { get; internal set; } = 0;

         /// <summary>
         /// If we've announced NODE_WITNESS to this peer: whether the peer sends witnesses in cmpctblocks/blocktxns,
         /// otherwise: whether this peer sends non-witnesses in cmpctblocks/blocktxns.
         /// </summary>
         public bool SupportsDesiredCompactVersion { get; internal set; } = false;

         /// <summary>
         /// Whether this peer will send us cmpctblocks if we request them (fProvidesHeaderAndIDs).
         /// This is not used to gate request logic, as we really only care about fSupportsDesiredCmpctVersion,
         /// but is used as a flag to "lock in" the version of compact blocks(fWantsCmpctWitness) we send.
         /// </summary>
         public bool ProvidesHeaderAndIDs { get; set; } = false;

         /// <summary>
         /// When true, enable compact messaging using high bandwidth mode.
         /// See BIP 152 for details.
         /// </summary>
         public bool AnnounceUsingCompactBlock { get; internal set; } = false;

         /// <summary>
         /// Whether new block should be announced using send headers, see BIP 130.
         /// </summary>
         public bool AnnounceNewBlockUsingSendHeaders { get; internal set; } = false;

         /// <summary>
         /// The unconnecting headers counter, used to issue a misbehavior penalty when exceed the expected threshold.
         /// It gets reset to 0 when a header connects successfully.
         /// </summary>
         public int UnconnectingHeaderReceived { get; internal set; } = 0;

         /// <summary>
         /// Gets or sets the last unknown block hash.
         /// </summary>
         /// <value>
         /// The last unknown block hash.
         /// </value>
         public UInt256? LastUnknownBlockHash { get; internal set; }

         /// <summary>
         /// Gets or sets the best known block we know this peer has announced.
         /// </summary>
         /// <value>
         /// The best known header.
         /// </value>
         public HeaderNode? BestKnownHeader { get; internal set; }

         /// <summary>
         /// Gets the time of last new block announcement.
         /// </summary>
         /// <value>
         /// The last block announcement time (epoch).
         /// </value>
         public long LastBlockAnnouncement { get; internal set; }

         /// <summary>
         /// Gets the blocks in download.
         /// </summary>
         /// <value>
         /// The blocks in download.
         /// </value>
         public int BlocksInDownload { get; internal set; }

         /// <summary>
         /// Whether this peer can give us witnesses. (fHaveWitness)
         /// </summary>
         /// <value>
         ///   <c>true</c> if the peer can serve witness; otherwise, <c>false</c>.
         /// </value>
         public bool CanServeWitness { get; internal set; }

         /// <summary>
         /// Gets or sets a value indicating whether this peer wants witnesses in cmpctblocks/blocktxns.
         /// </summary>
         public bool WantsCompactWitness { get; set; }

         /// <summary>
         /// Gets the date when the peer started to download blocks.
         /// </summary>
         /// <value>
         /// The date when the peer started to download blocks.
         /// </value>
         public long DownloadingSince { get; internal set; }
      }
   }
}
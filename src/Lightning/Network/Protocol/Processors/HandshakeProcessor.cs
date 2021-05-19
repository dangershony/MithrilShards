﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Core;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol.Processors;
using Network.Protocol.Messages;
using Network.Protocol.TlvStreams;
using Network.Protocol.TlvStreams.TlvRecords;
using Network.Protocol.Transport;
using Network.Storage.Gossip;

namespace Network.Protocol.Processors
{
   public partial class HandshakeProcessor : BaseProcessor,
      INetworkMessageHandler<HandshakeMessage>,
      INetworkMessageHandler<InitMessage>
   {
      private const int HANDSHAKE_TIMEOUT_SECONDS = 500; //5;
      readonly IGossipRepository _gossipRepository;

      private IHandshakeProtocol? _handshakeProtocol;
      private int _handshakeActNumber;

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                                IEventBus eventBus,
                                IPeerBehaviorManager peerBehaviorManager,
                                IGossipRepository gossipRepository) 
         : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         _gossipRepository = gossipRepository;
      }

      public override bool CanReceiveMessages { get { return true; } }

      protected override async ValueTask OnPeerAttachedAsync()
      {
         _handshakeActNumber = 1;

         //add the status to the PeerContext, this way other processors may query the status
         _handshakeProtocol = PeerContext.HandshakeProtocol 
                              ?? throw new ArgumentNullException(nameof(PeerContext.HandshakeProtocol));

         // ensures the handshake is performed timely
         _ = DisconnectIfAsync(() =>
         {
            return new ValueTask<bool>(PeerContext.HandshakeComplete == false);
         }, TimeSpan.FromSeconds(HANDSHAKE_TIMEOUT_SECONDS), "Handshake not performed in time");

         if (PeerContext.Direction == PeerConnectionDirection.Outbound)
         {
            Logger.LogDebug("Handshake ActOne sent.", ++_handshakeActNumber);
            await SendMessageAsync(Handshake(new ReadOnlySequence<byte>())).ConfigureAwait(false);
         }
      }

      public async ValueTask<bool> ProcessMessageAsync(HandshakeMessage noiseMessage, CancellationToken cancellation)
      {
         if (PeerContext.HandshakeComplete)
         {
            Logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         switch (_handshakeActNumber)
         {
            case 1: // ActOne responder
               {
                  Logger.LogDebug("Handshake ActOne received.");
                  await SendMessageAsync(Handshake(noiseMessage.Payload), cancellation).ConfigureAwait(false);
                  _handshakeActNumber += 2; // jump to act3
                  Logger.LogDebug("Handshake ActTwo sent.");
                  break;
               }
            case 2: // ActTwo both
               {
                  Logger.LogDebug("Handshake ActTwo received.");
                  await SendMessageAsync(Handshake(noiseMessage.Payload), cancellation).ConfigureAwait(false);
                  PeerContext.OnHandshakeCompleted();
                  _handshakeActNumber++;
                  Logger.LogDebug("Handshake ActThree sent.");
                  break;
               }
            case 3: // ActThree Responder
               {
                  Logger.LogDebug("Handshake ActThree received.");
                  _ = Handshake(noiseMessage.Payload);
                  _handshakeActNumber++;
                  Logger.LogDebug("Handshake Init sent.");
                  PeerContext.OnHandshakeCompleted();
                  await SendMessageAsync(CreateInitMessage(), cancellation).ConfigureAwait(false);
                  PeerContext.OnInitMessageCompleted();
                  break;
               }
         }

         // will prevent to handle noise messages to other Processors
         return false;
      }

      private HandshakeMessage Handshake(ReadOnlySequence<byte> input)
      {
         if (_handshakeProtocol == null)
            throw new InvalidOperationException();
         
         var output = new ArrayBufferWriter<byte>();
         _handshakeProtocol.Handshake(input, output);
         return new HandshakeMessage { Payload = new ReadOnlySequence<byte>(output.WrittenMemory) };
      }

      public async ValueTask<bool> ProcessMessageAsync(InitMessage message, CancellationToken cancellation)
      {
         Logger.LogDebug("Handshake Init received");

         // validate init message

         PeerContext.OnInitMessageCompleted();
         
         await SendMessageAsync(CreateInitMessage(), cancellation).ConfigureAwait(false);

         _gossipRepository.AddNode(new GossipNode(PeerContext.NodeId, 
            message.Features, new byte[0], new byte[0],new byte[0]));
         
         return true;
      }

      private InitMessage CreateInitMessage()
      {
         return new InitMessage
         {
            GlobalFeatures = new byte[4],
            Features = new byte[4],
            Extension = new TlVStream
            {
               Records = new List<TlvRecord>
               {
                  new NetworksTlvRecord {Type = 1},
               }
            }
         };
      }
   }
}
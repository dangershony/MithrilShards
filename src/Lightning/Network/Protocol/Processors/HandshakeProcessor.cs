using System;
using System.Buffers;
using System.Collections.Generic;
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
using Network.Protocol.Transport;
using Network.Protocol.Types;

namespace Network.Protocol.Processors
{
   public partial class HandshakeProcessor : BaseProcessor,
      INetworkMessageHandler<HandshakeMessage>,
      INetworkMessageHandler<InitMessage>
   {
      private const int HANDSHAKE_TIMEOUT_SECONDS = 500; //5;
      private readonly IDateTimeProvider _dateTimeProvider;
      private readonly IRandomNumberGenerator _randomNumberGenerator;
      private readonly IUserAgentBuilder _userAgentBuilder;

      private IHandshakeProtocol _handshakeProtocol;
      private int _handshakeActNumber;
      private bool _handshakeInitiator;

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                                IEventBus eventBus,
                                IDateTimeProvider dateTimeProvider,
                                IRandomNumberGenerator randomNumberGenerator,
                                IPeerBehaviorManager peerBehaviorManager,
                                IUserAgentBuilder userAgentBuilder
                                ) : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         _dateTimeProvider = dateTimeProvider;
         _randomNumberGenerator = randomNumberGenerator;
         _userAgentBuilder = userAgentBuilder;
      }

      public override bool CanReceiveMessages { get { return true; } }

      protected override async ValueTask OnPeerAttachedAsync()
      {
         _handshakeActNumber = 1;
         _handshakeInitiator = PeerContext.Direction == PeerConnectionDirection.Outbound;

         //add the status to the PeerContext, this way other processors may query the status
         _handshakeProtocol = PeerContext.HandshakeProtocol;

         // ensures the handshake is performed timely
         _ = DisconnectIfAsync(() =>
         {
            return new ValueTask<bool>(PeerContext.HandshakeComplete == false);
         }, TimeSpan.FromSeconds(HANDSHAKE_TIMEOUT_SECONDS), "Handshake not performed in time");

         if (PeerContext.Direction == PeerConnectionDirection.Outbound)
         {
            logger.LogDebug("Handshake ActOne sent.", ++_handshakeActNumber);
            await SendMessageAsync(Handshake(new ReadOnlySequence<byte>())).ConfigureAwait(false);
         }
      }

      public async ValueTask<bool> ProcessMessageAsync(HandshakeMessage noiseMessage, CancellationToken cancellation)
      {
         if (PeerContext.HandshakeComplete)
         {
            logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         switch (_handshakeActNumber)
         {
            case 1: // ActOne
               {
                  logger.LogDebug("Handshake ActOne received.");
                  await SendMessageAsync(Handshake(noiseMessage.Payload), cancellation).ConfigureAwait(false);
                  _handshakeActNumber += 2; // jump to act3
                  logger.LogDebug("Handshake ActTwo sent.");
                  break;
               }
            case 2: // ActTwo
               {
                  logger.LogDebug("Handshake ActTwo received.");
                  await SendMessageAsync(Handshake(noiseMessage.Payload), cancellation).ConfigureAwait(false);
                  PeerContext.OnHandshakeCompleted();
                  _handshakeActNumber++;
                  logger.LogDebug("Handshake ActThree sent.");
                  break;
               }
            case 3: // ActThree
               {
                  logger.LogDebug("Handshake ActThree received.");
                  _ = Handshake(noiseMessage.Payload);
                  _handshakeActNumber++;
                  logger.LogDebug("Handshake Init sent.");
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
         var output = new ArrayBufferWriter<byte>();
         _handshakeProtocol.Handshake(input.FirstSpan, output);
         return new HandshakeMessage { Payload = new ReadOnlySequence<byte>(output.WrittenMemory) };
      }

      public ValueTask<bool> ProcessMessageAsync(InitMessage message, CancellationToken cancellation)
      {
         logger.LogDebug("Handshake Init received.");

         // validate init message

         PeerContext.OnInitMessageCompleted();
         return new ValueTask<bool>(true);
      }

      private InitMessage CreateInitMessage()
      {
         return new InitMessage
         {
            GlobalFeatures = new byte[] { 1, 2, 3, 4 },
            Features = new byte[] { 1, 2, 3, 4, 5 },
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
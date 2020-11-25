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
      private readonly IDateTimeProvider dateTimeProvider;
      private readonly IRandomNumberGenerator randomNumberGenerator;
      private readonly IUserAgentBuilder userAgentBuilder;

      private IHandshakeProtocol handshakeProtocol;
      private int HandshakeActNumber;
      private bool HandshakeInitiiator;

      public HandshakeProcessor(ILogger<HandshakeProcessor> logger,
                                IEventBus eventBus,
                                IDateTimeProvider dateTimeProvider,
                                IRandomNumberGenerator randomNumberGenerator,
                                IPeerBehaviorManager peerBehaviorManager,
                                IUserAgentBuilder userAgentBuilder
                                ) : base(logger, eventBus, peerBehaviorManager, isHandshakeAware: true)
      {
         this.dateTimeProvider = dateTimeProvider;
         this.randomNumberGenerator = randomNumberGenerator;
         this.userAgentBuilder = userAgentBuilder;
      }

      public override bool CanReceiveMessages { get { return true; } }

      protected override async ValueTask OnPeerAttachedAsync()
      {
         this.HandshakeActNumber = 1;
         this.HandshakeInitiiator = this.PeerContext.Direction == PeerConnectionDirection.Outbound;

         //add the status to the PeerContext, this way other processors may query the status
         this.handshakeProtocol = this.PeerContext.HandshakeProtocol;

         // ensures the handshake is performed timely
         _ = this.DisconnectIfAsync(() =>
         {
            return new ValueTask<bool>(this.PeerContext.HandshakeComplete == false);
         }, TimeSpan.FromSeconds(HANDSHAKE_TIMEOUT_SECONDS), "Handshake not performed in time");

         if (this.PeerContext.Direction == PeerConnectionDirection.Outbound)
         {
            this.logger.LogDebug("Handshake ActOne sent.", ++this.HandshakeActNumber);
            await this.SendMessageAsync(this.Handshake(new ReadOnlySequence<byte>())).ConfigureAwait(false);
         }
      }

      public async ValueTask<bool> ProcessMessageAsync(HandshakeMessage noiseMessage, CancellationToken cancellation)
      {
         if (this.PeerContext.HandshakeComplete)
         {
            this.logger.LogDebug("Receiving version while already handshaked, disconnect.");
            throw new ProtocolViolationException("Peer already handshaked, disconnecting because of protocol violation.");
         }

         switch (this.HandshakeActNumber)
         {
            case 1: // ActOne
               {
                  this.logger.LogDebug("Handshake ActOne received.");
                  await this.SendMessageAsync(this.Handshake(noiseMessage.Payload), cancellation).ConfigureAwait(false);
                  this.HandshakeActNumber += 2; // jump to act3
                  this.logger.LogDebug("Handshake ActTwo sent.");
                  break;
               }
            case 2: // ActTwo
               {
                  this.logger.LogDebug("Handshake ActTwo received.");
                  await this.SendMessageAsync(this.Handshake(noiseMessage.Payload), cancellation).ConfigureAwait(false);
                  this.PeerContext.OnHandshakeCompleted();
                  this.HandshakeActNumber++;
                  this.logger.LogDebug("Handshake ActThree sent.");
                  break;
               }
            case 3: // ActThree
               {
                  this.logger.LogDebug("Handshake ActThree received.");
                  _ = this.Handshake(noiseMessage.Payload);
                  this.HandshakeActNumber++;
                  this.logger.LogDebug("Handshake Init sent.");
                  this.PeerContext.OnHandshakeCompleted();
                  await this.SendMessageAsync(this.CreateInitMessage(), cancellation).ConfigureAwait(false);
                  this.PeerContext.OnInitMessageCompleted();
                  break;
               }
         }

         // will prevent to handle noise messages to other Processors
         return false;
      }

      private HandshakeMessage Handshake(ReadOnlySequence<byte> input)
      {
         var output = new ArrayBufferWriter<byte>();
         this.handshakeProtocol.Handshake(input.FirstSpan, output);
         return new HandshakeMessage { Payload = new ReadOnlySequence<byte>(output.WrittenMemory) };
      }

      public ValueTask<bool> ProcessMessageAsync(InitMessage message, CancellationToken cancellation)
      {
         this.logger.LogDebug("Handshake Init received.");

         // validate init message

         this.PeerContext.OnInitMessageCompleted();
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
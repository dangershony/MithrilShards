﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Extensions;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Events;
using MithrilShards.Core.Network.PeerBehaviorManager;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Processors;

namespace MithrilShards.Chain.Bitcoin.Protocol.Processors
{
   public abstract class BaseProcessor : INetworkMessageProcessor
   {
      protected readonly ILogger<BaseProcessor> logger;
      protected readonly IEventBus eventBus;
      private readonly IPeerBehaviorManager peerBehaviorManager;
      private readonly bool isHandshakeAware;
      private INetworkMessageWriter messageWriter;

      /// <summary>
      /// Holds registration of subscribed <see cref="IEventBus"/> event handlers.
      /// </summary>
      private readonly EventSubscriptionManager eventSubscriptionManager = new EventSubscriptionManager();

      public BitcoinPeerContext PeerContext { get; private set; }

      public bool Enabled { get; private set; } = true;

      /// <summary>Initializes a new instance of the <see cref="BaseProcessor"/> class.</summary>
      /// <param name="logger">The logger.</param>
      /// <param name="eventBus">The event bus.</param>
      /// <param name="peerBehaviorManager">The peer behavior manager.</param>
      /// <param name="isHandshakeAware">If set to <c>true</c> register the instance to be handshake aware: when the peer is handshaked, OnPeerHandshaked method will be invoked.</param>
      public BaseProcessor(ILogger<BaseProcessor> logger, IEventBus eventBus, IPeerBehaviorManager peerBehaviorManager, bool isHandshakeAware)
      {
         this.logger = logger;
         this.eventBus = eventBus;
         this.peerBehaviorManager = peerBehaviorManager;
         this.isHandshakeAware = isHandshakeAware;

         this.PeerContext = null!; //hack to not rising null warnings, these are initialized when calling AttachAsync
         this.messageWriter = null!; //hack to not rising null warnings, these are initialized when calling AttachAsync
      }

      public async ValueTask AttachAsync(IPeerContext peerContext)
      {
         this.PeerContext = peerContext as BitcoinPeerContext ?? throw new ArgumentException("Expected BitcoinPeerContext", nameof(peerContext));
         this.messageWriter = this.PeerContext.GetMessageWriter();

         await this.OnPeerAttachedAsync().ConfigureAwait(false);
      }

      /// <summary>
      /// Called when a peer has been attached and <see cref="PeerContext"/> assigned.
      /// </summary>
      /// <returns></returns>
      protected virtual ValueTask OnPeerAttachedAsync()
      {
         if (this.isHandshakeAware)
         {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
            this.RegisterLifeTimeSubscription(this.eventBus.Subscribe<PeerHandshaked>(async (receivedEvent) =>
            {
               await this.OnPeerHandshakedAsync().ConfigureAwait(false);
            }));
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
         }

         return default;
      }

      /// <summary>Method invoked when the peer handshakes and <see cref="isHandshakeAware"/> is set to <see langword="true"/>.</summary>
      /// <param name="event">The event.</param>
      /// <returns></returns>
      protected virtual ValueTask OnPeerHandshakedAsync()
      {
         return default;
      }


      /// <summary>
      /// Registers the component life time subscription to an <see cref="IEventBus"/> event that will be automatically
      /// unregistered once the component gets disposed.
      /// </summary>
      /// <param name="subscription">The subscription.</param>
      protected void RegisterLifeTimeSubscription(SubscriptionToken subscription)
      {
         this.eventSubscriptionManager.RegisterSubscriptions(subscription);
      }

      /// <summary>
      /// Sends the message asynchronously to the other peer.
      /// </summary>
      /// <param name="message">The message to send.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      public async ValueTask SendMessageAsync(INetworkMessage message, CancellationToken cancellationToken = default)
      {
         await this.messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
      }

      /// <summary>
      /// Sends the message asynchronously to the other peer.
      /// </summary>
      /// <param name="message">The message to send.</param>
      /// <param name="minVersion">
      /// The minimum, inclusive, negotiated version required to send this message.
      /// Passing 0 means the message will be sent without version check.
      /// </param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      protected async ValueTask<bool> SendMessageAsync(int minVersion, INetworkMessage message, CancellationToken cancellationToken = default)
      {
         if (minVersion == 0 || this.PeerContext.NegotiatedProtocolVersion.Version >= minVersion)
         {
            await this.messageWriter.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            return true;
         }
         else
         {
            return false;
         }
      }

      /// <summary>
      /// Disconnects if, after timeout expires, the condition is evaluated to true.
      /// </summary>
      /// <param name="condition">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
      /// <param name="timeout">The timeout that will trigger the condition evaluation.</param>
      /// <param name="reason">The disconnection reason to set in case of disconnection.</param>
      /// <param name="cancellation">The cancellation that may interrupt the <paramref name="condition" /> evaluation.</param>
      /// <returns></returns>
      protected Task DisconnectIfAsync(Func<ValueTask<bool>> condition, TimeSpan timeout, string reason, CancellationToken cancellation = default)
      {
         if (cancellation == default)
         {
            cancellation = this.PeerContext.ConnectionCancellationTokenSource.Token;
         }

         return Task.Run(async () =>
         {
            await Task.Delay(timeout).WithCancellationAsync(cancellation).ConfigureAwait(false);
            // if cancellation was requested, return without doing anything
            if (!cancellation.IsCancellationRequested && !this.PeerContext.ConnectionCancellationTokenSource.Token.IsCancellationRequested && await condition().ConfigureAwait(false))
            {
               this.logger.LogDebug("Request peer disconnection because {DisconnectionRequestReason}", reason);
               this.PeerContext.ConnectionCancellationTokenSource.Cancel();
            }
         });
      }

      /// <summary>
      /// Execute an action in case the condition evaluates to true after <paramref name="timeout"/> expires.
      /// </summary>
      /// <param name="condition">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
      /// <param name="timeout">The timeout that will trigger the condition evaluation.</param>
      /// <param name="action">The condition that, when evaluated to true, causes the peer to be disconnected.</param>
      /// <returns></returns>
      protected Task ExecuteIfAsync(Func<ValueTask<bool>> condition, TimeSpan timeout, Func<ValueTask> action, CancellationToken cancellation = default)
      {
         if (cancellation == default)
         {
            cancellation = this.PeerContext.ConnectionCancellationTokenSource.Token;
         }

         return Task.Run(async () =>
         {
            await Task.Delay(timeout).WithCancellationAsync(cancellation).ConfigureAwait(false);
            // if cancellation was requested, return without doing anything
            if (!cancellation.IsCancellationRequested && !this.PeerContext.ConnectionCancellationTokenSource.Token.IsCancellationRequested && await condition().ConfigureAwait(false))
            {
               this.logger.LogDebug("Condition met, trigger action.");
               await action().ConfigureAwait(false);
            }
         });
      }

      /// <summary>
      /// Punish the peer because of his misbehaves.
      /// </summary>
      /// <param name="penalty">The penalty.</param>
      /// <param name="reason">The reason.</param>
      /// <param name="disconnect">if set to <c>true</c> [disconnect].</param>
      protected void Misbehave(uint penalty, string reason, bool disconnect = false)
      {
         this.peerBehaviorManager.Misbehave(this.PeerContext, penalty, reason);
         if (disconnect)
         {
            this.logger.LogDebug("Request peer disconnection because {DisconnectionRequestReason}", reason);
            this.PeerContext.ConnectionCancellationTokenSource.Cancel();
         }
      }

      public virtual void Dispose()
      {
         this.eventSubscriptionManager.Dispose();

         this.PeerContext?.ConnectionCancellationTokenSource.Cancel();
      }

   }
}

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MithrilShards.Core;
using MithrilShards.Core.Forge;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol.Processors;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Core.Network.Server.Guards;
using Network.Protocol;
using Network.Protocol.Serialization;
using Network.Protocol.TlvStreams;
using Network.Protocol.Transport;
using Network.Protocol.Transport.Noise;
using Network.Settings;
using NoiseProtocol;

namespace Network
{
   public static class Builder
   {
      public static IForgeBuilder UseLightningNetwork(this IForgeBuilder forgeBuilder)
      {
         if (forgeBuilder is null) throw new ArgumentNullException(nameof(forgeBuilder));

         forgeBuilder.AddShard<LightningNode, LightningNodeSettings>(
            (hostBuildContext, services) =>
            {
               services
                  .AddSingleton<IDateTimeProvider, DateTimeProvider>()
                  .AddSingleton<NodeContext>()
                  .AddMessageSerializers()
                  .AddProtocolTypeSerializers()
                  .AddMessageProcessors()
                  .AddTlvComponents()
                  .ReplaceServices()
                  .AddNoiseComponents();
            });

         return forgeBuilder;
      }

      private static IServiceCollection AddNoiseComponents(this IServiceCollection services)
      {
         //services.AddSingleton<IHandshakeStateFactory, HandshakeStateFactory>();
         services.AddSingleton<IEllipticCurveActions,EllipticCurveActions>();
         services.AddTransient<IHashWithState,OldHash>();
         services.AddSingleton<NoiseProtocol.IHkdf,OldHkdf>();
         services.AddTransient<ICipherFunction,ChaCha20Poly1305CipherFunction>();
         services.AddSingleton<IHashFunction,NoiseProtocol.Sha256>();
         services.AddTransient<INoiseMessageTransformer,NoiseMessageTransformer>();
         services.AddSingleton<IKeyGenerator,KeyGenerator>();
         services.AddSingleton<IHandshakeProcessor, NoiseProtocol.HandshakeProcessor>();
         return services;
      }

      private static IServiceCollection AddTlvComponents(this IServiceCollection services)
      {
         services.AddSingleton<ITlvStreamSerializer, TlvStreamSerializer>();

         // Discovers and registers all message serializer in this assembly.
         // It is possible to add them manually to have full control of message serializers we are interested into.
         Type serializerInterface = typeof(ITlvRecordSerializer);
         foreach (Type messageSerializerType in typeof(LightningNode).Assembly.GetTypes().Where(t => serializerInterface.IsAssignableFrom(t) && !t.IsAbstract))
         {
            services.AddSingleton(typeof(ITlvRecordSerializer), messageSerializerType);
         }

         return services;
      }

      private static IServiceCollection AddMessageSerializers(this IServiceCollection services)
      {
         // Discovers and registers all message serializer in this assembly.
         // It is possible to add them manually to have full control of message serializers we are interested into.
         Type serializerInterface = typeof(INetworkMessageSerializer);
         foreach (Type messageSerializerType in typeof(LightningNode).Assembly.GetTypes().Where(t => serializerInterface.IsAssignableFrom(t) && !t.IsAbstract))
         {
            services.AddSingleton(typeof(INetworkMessageSerializer), messageSerializerType);
         }

         return services;
      }

      private static IServiceCollection AddProtocolTypeSerializers(this IServiceCollection services)
      {
         // Discovers and registers all message serializer in this assembly.
         // It is possible to add them manually to have full control of protocol serializers we are interested into.
         Type protocolSerializerInterface = typeof(IProtocolTypeSerializer<>);
         var implementations = from type in typeof(LightningNode).Assembly.GetTypes()
                               from typeInterface in type.GetInterfaces()
                               where typeInterface.IsGenericType && protocolSerializerInterface.IsAssignableFrom(typeInterface.GetGenericTypeDefinition())
                               select new { Interface = typeInterface, ImplementationType = type };

         foreach (var implementation in implementations)
         {
            services.AddSingleton(implementation.Interface, implementation.ImplementationType);
         }

         return services;
      }

      private static IServiceCollection AddMessageProcessors(this IServiceCollection services)
      {
         // Discovers and registers all message processors in this assembly.
         // It is possible to add them manually to have full control of message processors we are interested into.
         Type serializerInterface = typeof(INetworkMessageProcessor);
         foreach (Type processorType in typeof(LightningNode).Assembly.GetTypes().Where(t => !t.IsAbstract && serializerInterface.IsAssignableFrom(t)))
         {
            services.AddTransient(typeof(INetworkMessageProcessor), processorType);
         }

         return services;
      }

      private static IServiceCollection ReplaceServices(this IServiceCollection services)
      {
         services
            .Replace(ServiceDescriptor.Singleton<IPeerContextFactory, NetworkPeerContextFactory>())
            ;

         /// Replace extension is useful when we want to replace an implementation that's only used as a single entry when resolved, and not as IEnumerable,
         /// because Replace, as per documentation, replaces the first registered service type with the provided ServiceDescriptor.
         /// when we have different implementation of the same service type, when we resolve a single instance then the last registered implementation is used,
         /// while if we resolve as IEnumerable then all known implementations are returned. This mean that when we are registered something to be resolved as
         /// IEnumerable we can't replace one of them at will so we have to fall back to the Remove method and then add the one implementation we want, back into
         /// the IServiceCollection.

         // in this scenario, we are looking for the ServiceDescriptor that registers our RequiredConnection implementation of IConnector
         var requiredConnectionDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IConnector) && descriptor.ImplementationType == typeof(RequiredConnection));
         // remove it
         services.Remove(requiredConnectionDescriptor);
         // and add back our implementation
         services.AddSingleton<IConnector, NetworkRequiredConnection>();

         return services;
      }
   }
}
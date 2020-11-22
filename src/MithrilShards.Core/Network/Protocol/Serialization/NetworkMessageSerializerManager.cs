﻿using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace MithrilShards.Core.Network.Protocol.Serialization
{
   public class NetworkMessageSerializerManager : INetworkMessageSerializerManager
   {
      readonly ILogger<NetworkMessageSerializerManager> _logger;
      readonly IEnumerable<INetworkMessageSerializer> _messageSerializers;
      private Dictionary<string, INetworkMessageSerializer> _serializers;

      public NetworkMessageSerializerManager(ILogger<NetworkMessageSerializerManager> logger, IEnumerable<INetworkMessageSerializer> messageSerializers)
      {
         this._logger = logger;
         this._messageSerializers = messageSerializers;
         this._serializers = null!; //will be serialized in InitializeMessageSerializers

         this.InitializeMessageSerializers();
      }


      private void InitializeMessageSerializers()
      {
         this._serializers = (
            from serializer in this._messageSerializers
            let managedMessageType = serializer.GetMessageType()
            let networkMessageAttribute = managedMessageType.GetCustomAttribute<NetworkMessageAttribute>()
            where networkMessageAttribute != null
            select new { Command = networkMessageAttribute.Command, Serializer = serializer }
         ).ToDictionary(reg => reg.Command, reg => reg.Serializer);


         this._logger.LogInformation(
                  "Using {NetworkMessageSerializersCount} message network serializers: {NetworkMessageSerializers}.",
                  this._serializers.Count,
                  this._serializers.Keys.ToArray()
                  );
      }

      public bool TrySerialize(INetworkMessage message, int protocolVersion, IPeerContext peerContext, IBufferWriter<byte> output)
      {
         if (this._serializers.TryGetValue(message.Command, out INetworkMessageSerializer? serializer))
         {
            serializer.Serialize(message, protocolVersion, peerContext, output);
            return true;
         }

         return false;
      }

      public bool TryDeserialize(string commandName, ref ReadOnlySequence<byte> data, int protocolVersion, IPeerContext peerContext, [MaybeNullWhen(false)] out INetworkMessage message)
      {
         if (this._serializers.TryGetValue(commandName, out INetworkMessageSerializer? serializer))
         {
            message = serializer.Deserialize(ref data, protocolVersion, peerContext);
            return true;
         }

         message = null!;
         return false;
      }
   }
}

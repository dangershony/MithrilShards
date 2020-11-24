﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages;
using Network.Protocol.Serialization;
using Network.Protocol.Types;

namespace Network.Protocol
{
   public abstract class BaseMessageSerializer<TMessageType> : NetworkMessageSerializerBase<TMessageType, NetworkPeerContext> where TMessageType : BaseMessage, new()
   {
      private readonly ITlvStreamSerializer tlvStreamSerializer;

      protected BaseMessageSerializer(ITlvStreamSerializer tlvStreamSerializer)
      {
         this.tlvStreamSerializer = tlvStreamSerializer;
      }

      public override TMessageType Deserialize(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext)
      {
         TMessageType message = this.DeserializeMessage(ref reader, protocolVersion, peerContext);

         message.Extension = this.tlvStreamSerializer.DeserializeTlvStream(ref reader);

         return message;
      }

      public override void Serialize(TMessageType message, int protocolVersion, NetworkPeerContext peerContext, IBufferWriter<byte> output)
      {
         this.SerializeMessage(message, protocolVersion, peerContext, output);

         this.tlvStreamSerializer.SerializeTlvStream(message.Extension, output);
      }

      public abstract void SerializeMessage(TMessageType message, int protocolVersion, NetworkPeerContext peerContext, IBufferWriter<byte> output);

      public abstract TMessageType DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext);
   }
}
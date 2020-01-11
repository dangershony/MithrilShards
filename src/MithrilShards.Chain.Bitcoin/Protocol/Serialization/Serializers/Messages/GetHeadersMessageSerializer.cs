﻿using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.DataTypes;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class GetHeadersMessageSerializer : NetworkMessageSerializerBase<GetHeadersMessage>
   {
      private readonly IProtocolTypeSerializer<BlockLocator> blockLocatorSerializer;
      private readonly IProtocolTypeSerializer<UInt256> uint256Serializer;

      public GetHeadersMessageSerializer(IChainDefinition chainDefinition,
                                         IProtocolTypeSerializer<BlockLocator> blockLocatorSerializer,
                                         IProtocolTypeSerializer<UInt256> uint256Serializer) : base(chainDefinition)
      {
         this.blockLocatorSerializer = blockLocatorSerializer;
         this.uint256Serializer = uint256Serializer;
      }

      public override void Serialize(GetHeadersMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteUInt(message.Version);
         output.WriteWithSerializer(message.BlockLocator, protocolVersion, this.blockLocatorSerializer);
         output.WriteWithSerializer(message.HashStop, protocolVersion, this.uint256Serializer);
      }

      public override GetHeadersMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         var message = new GetHeadersMessage
         {
            Version = reader.ReadUInt(),
            BlockLocator = reader.ReadWithSerializer(protocolVersion, this.blockLocatorSerializer),
            HashStop = reader.ReadWithSerializer(protocolVersion, this.uint256Serializer)
         };

         return message;
      }
   }
}
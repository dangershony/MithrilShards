﻿using System.Buffers;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers
{
   public class GetBlocksMessageSerializer : NetworkMessageSerializerBase<GetBlocksMessage>
   {
      public GetBlocksMessageSerializer(IChainDefinition chainDefinition) : base(chainDefinition) { }

      public override void Serialize(GetBlocksMessage message, int protocolVersion, IBufferWriter<byte> output)
      {
         output.WriteUInt(message.Version);
         message.BlockLocator.Serialize(output, protocolVersion);
         output.WriteUInt256(message.HashStop);
      }

      public override GetBlocksMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion)
      {
         var message = new GetBlocksMessage
         {
            Version = reader.ReadUInt()
         };

         //TODO add extension to read and write blocklocator
         message.BlockLocator = new Types.BlockLocator();
         message.BlockLocator.Deserialize(ref reader, protocolVersion);

         message.HashStop = reader.ReadUInt256();

         return message;
      }
   }
}
﻿using System.Buffers;
using MithrilShards.Chain.Bitcoin.Network;
using MithrilShards.Chain.Bitcoin.Protocol.Messages;
using MithrilShards.Chain.Bitcoin.Protocol.Types;
using MithrilShards.Core.Network.Protocol.Serialization;

namespace MithrilShards.Chain.Bitcoin.Protocol.Serialization.Serializers.Messages
{
   public class BlockMessageSerializer : BitcoinNetworkMessageSerializerBase<BlockMessage>
   {
      private readonly IProtocolTypeSerializer<Block> blockSerializer;

      public BlockMessageSerializer(IProtocolTypeSerializer<Block> blockSerializer)
      {
         this.blockSerializer = blockSerializer;
      }

      public override void Serialize(BlockMessage message, int protocolVersion, BitcoinPeerContext peerContext, IBufferWriter<byte> output)
      {
         ProtocolTypeSerializerOptions? options = null;
         message.PopulateSerializerOption(ref options);

         output.WriteWithSerializer(message.Block!, protocolVersion, this.blockSerializer, options);
      }

      public override BlockMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, BitcoinPeerContext peerContext)
      {
         ProtocolTypeSerializerOptions options = new ProtocolTypeSerializerOptions((SerializerOptions.SERIALIZE_WITNESS, peerContext.CanServeWitness));

         return new BlockMessage { Block = reader.ReadWithSerializer(protocolVersion, this.blockSerializer, options) };
      }
   }
}
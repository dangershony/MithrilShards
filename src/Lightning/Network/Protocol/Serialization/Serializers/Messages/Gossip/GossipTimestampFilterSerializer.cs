using System;
using System.Buffers;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.TlvStreams;

namespace Network.Protocol.Serialization.Serializers.Messages.Gossip
{
   public class GossipTimestampFilterSerializer : BaseMessageSerializer<GossipTimestampFilter>
   {
      public GossipTimestampFilterSerializer(ITlvStreamSerializer tlvStreamSerializer) : base(tlvStreamSerializer)
      { }

      public override void SerializeMessage(GossipTimestampFilter message, int protocolVersion, NetworkPeerContext peerContext,
         IBufferWriter<byte> output)
      {
         output.WriteBytes(message.ChainHash ?? throw new ArgumentNullException(nameof(message.ChainHash)));
         output.WriteUInt(message.FirstTimestamp);
         output.WriteUInt(message.TimestampRange);
      }


      public override GossipTimestampFilter DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion,
         NetworkPeerContext peerContext)
      {
         return new GossipTimestampFilter
         {
            ChainHash = (ChainHash) reader.ReadBytes(32),
            FirstTimestamp = reader.ReadUInt(),
            TimestampRange = reader.ReadUInt()
         };
      }
   }
}
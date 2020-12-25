using System;
using System.Buffers;
using System.Collections.Generic;
using Bitcoin.Primitives.Fundamental;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Utils;
using Moq;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.Messages.Types;
using Network.Protocol.Serialization.Serializers.Messages.Gossip;
using Network.Protocol.TlvStreams;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages.Gossip
{
   public class NodeAnnouncementSerializerTests : BaseMessageSerializerTests<NodeAnnouncement>
   {
      public NodeAnnouncementSerializerTests() 
         : base(new NodeAnnouncementSerializer(new TlvStreamSerializer(
            new Mock<ILogger<TlvStreamSerializer>>().Object, new List<ITlvRecordSerializer>())))
      { }

      private byte[] GetRandomBytes(Random random, ushort arrayLength)
      {
         var arr = new byte[arrayLength];
         random.NextBytes(arr);
         return arr;
      }
      
      protected override NodeAnnouncement WithRandomMessage(Random random)
      {
         var len = (ushort)random.Next(ushort.MaxValue);
         var addrlen = (ushort)random.Next(ushort.MaxValue);
         
         
         return new NodeAnnouncement
         {
            Signature = (CompressedSignature) GetRandomBytes(random, CompressedSignature.SIGNATURE_LENGTH),
            Len = len,
            Features = GetRandomBytes(random, len),
            Timestamp = (uint) random.Next(int.MaxValue),
            NodeId = (PublicKey) GetRandomBytes(random,PublicKey.LENGTH),
            RgbColor = GetRandomBytes(random,3),
            Alias = GetRandomBytes(random,32),
            Addrlen = addrlen,
            Addresses = GetRandomBytes(random,addrlen)
         };
      }

      protected override void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer,
         NodeAnnouncement message)
      {
         Assert.Equal(outputBuffer.WrittenCount,CompressedSignature.SIGNATURE_LENGTH + 2 + message.Len + 4 +
                                                PublicKey.LENGTH + 3 + 32 + 2 + message.Addrlen);
         
         
      }

      protected override void AssertMessageDeserialized(NodeAnnouncement baseMessage, NodeAnnouncement expectedMessage)
      {
         Assert.Equal(baseMessage.Addresses,expectedMessage.Addresses);
         Assert.Equal(baseMessage.Addrlen,expectedMessage.Addrlen);
         Assert.Equal(baseMessage.Alias,expectedMessage.Alias);
         Assert.Equal(baseMessage.Command,expectedMessage.Command);
         Assert.Equal(baseMessage.Features,expectedMessage.Features);
         Assert.Equal(baseMessage.Len,expectedMessage.Len);
         Assert.Equal((byte[]) baseMessage.Signature,(byte[]) expectedMessage.Signature);
         Assert.Equal(baseMessage.RgbColor,expectedMessage.RgbColor);
      }

      protected override IEnumerable<(string, NodeAnnouncement)> GetData()
      {
         yield return (new byte[140].ToHexString(),
            new NodeAnnouncement
         {
            Addresses = new byte[0], Addrlen = 0, Alias = new byte[32],
            Features = new byte[0], Len = 0, Signature = (CompressedSignature) new byte[64],
            NodeId = (PublicKey) new byte[33],Timestamp = 0, RgbColor = new byte[3]
         });
      }
   }
}
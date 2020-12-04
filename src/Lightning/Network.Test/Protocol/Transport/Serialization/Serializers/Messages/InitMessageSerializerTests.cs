using System;
using System.Buffers;
using System.Collections.Generic;
using Moq;
using Network.Protocol.Messages;
using Network.Protocol.Serialization;
using Network.Protocol.Serialization.Serializers.Messages;
using Network.Protocol.TlvStreams;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public class InitMessageSerializerTests : BaseMessageSerializerTests<InitMessage>
   {
      public InitMessageSerializerTests()
         : base(new InitMessageSerializer(new Mock<ITlvStreamSerializer>().Object))
      { }

      protected override InitMessage WithRandomMessage(Random random)
      {
         return new InitMessage
         {
            Features = new byte[] { 0x01 },
            GlobalFeatures = new byte[] { 0x03 }
         };
      }

      protected override void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer, InitMessage message)
      {
         Assert.Equal(outputBuffer.WrittenCount, 2 + 1 + 2 + 1);
      }

      protected override void AssertMessageDeserialized(InitMessage baseMessage, InitMessage expectedMessage)
      {
         Assert.Equal(baseMessage.Features, expectedMessage.Features);
         Assert.Equal(baseMessage.GlobalFeatures, expectedMessage.GlobalFeatures);
      }

      protected override IEnumerable<(string, InitMessage)> GetData()
      {
         // 0x001000000000 from Bolt 1 without type as it is not sent to the serializer
         yield return ("00000000", new InitMessage());
      }
   }
}
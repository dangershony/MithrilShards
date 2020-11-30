using System;
using System.Buffers;
using Moq;
using Network.Protocol.Messages;
using Network.Protocol.Serialization;
using Network.Protocol.Serialization.Serializers.Messages;
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
            Features = new byte[]{0x01},
            GlobalFeatures = new byte[]{0x03}
         };
      }

      protected override void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer, InitMessage message)
      {
         Assert.Equal(outputBuffer.WrittenCount, 2 + 1 + 2 + 1);
      }

      protected override void AssertMessageDeserialized(InitMessage baseMessage, InitMessage expectedMessage)
      {
         Assert.Equal(baseMessage,expectedMessage);
      }

      protected override (string, InitMessage) GetData()
      {
         // 0x010003010001
         // 0x001000000000
         return ("0x001000000000", new InitMessage());
      }
   }
}
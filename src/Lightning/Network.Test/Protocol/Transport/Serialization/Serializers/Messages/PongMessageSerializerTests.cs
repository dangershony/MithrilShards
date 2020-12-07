using System;
using System.Buffers;
using System.Collections.Generic;
using Moq;
using Network.Protocol.Messages;
using Network.Protocol.Serialization.Serializers.Messages;
using Network.Protocol.TlvStreams;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public class PongMessageSerializerTests : BaseMessageSerializerTests<PongMessage>
   {
      public PongMessageSerializerTests() 
         : base(new PongMessageSerializer(new Mock<ITlvStreamSerializer>().Object))
      { }

      protected override PongMessage WithRandomMessage(Random random)
      {
         ushort len = (ushort)random.Next(PingMessage.MAX_BYTES_LEN);
         return new PongMessage {BytesLen = len,Ignored = new byte[len]};
      }

      protected override void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer, PongMessage message)
      {
         Assert.Equal(outputBuffer.WrittenCount, 2 + message.BytesLen);
         Assert.NotEmpty(outputBuffer.WrittenSpan.ToArray());
      }

      protected override void AssertMessageDeserialized(PongMessage baseMessage, PongMessage expectedMessage)
      {
         Assert.Equal(expectedMessage.BytesLen,baseMessage.BytesLen);
         Assert.Equal(expectedMessage.Ignored,baseMessage.Ignored);
      }

      protected override IEnumerable<(string,PongMessage)> GetData()
      {
         yield return ("0x000a00000000000000000000",new PongMessage
         {
            BytesLen = 10,Ignored = new byte[10]
         });
      }
   }
}
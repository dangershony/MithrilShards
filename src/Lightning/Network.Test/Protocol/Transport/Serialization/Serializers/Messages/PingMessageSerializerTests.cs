using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Network.Protocol.Messages;
using Network.Protocol.Serialization;
using Network.Protocol.Serialization.Serializers.Messages;
using Network.Protocol.TlvStreams;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public class PingMessageSerializerTests : BaseMessageSerializerTests<PingMessage>
   {
      public PingMessageSerializerTests()
         : base(new PingMessageSerializer(new Mock<ITlvStreamSerializer>().Object))
      { }

      protected override PingMessage WithRandomMessage(Random random)
         => new PingMessage((ushort)random.Next(PingMessage.MAX_BYTES_LEN));

      protected override void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer, PingMessage message)
      {
         Assert.Equal(outputBuffer.WrittenCount, 2 + 2 + message.Ignored!.Length);
         Assert.NotEmpty(outputBuffer.WrittenSpan.ToArray());
      }

      protected override void AssertMessageDeserialized(PingMessage message, PingMessage expectedMessage)
      {
         Assert.NotNull(message);
         Assert.Equal(message.BytesLen, expectedMessage.BytesLen);
         Assert.Equal(message.NumPongBytes, expectedMessage.NumPongBytes);
         Assert.Equal(message.Ignored, expectedMessage.Ignored);
      }

      protected override (string, PingMessage) GetData()
      {
         return ("0xfff1000a00000000000000000000",
               new PingMessage { BytesLen = 10, NumPongBytes = 65521, Ignored = new byte[10] });
      }
   }
}
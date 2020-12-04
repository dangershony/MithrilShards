using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Moq;
using Network.Protocol.Messages;
using Network.Protocol.Serialization.Serializers.Messages;
using Network.Protocol.TlvStreams;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public class ErrorMessageSerializerTests : BaseMessageSerializerTests<ErrorMessage>
   {
      public ErrorMessageSerializerTests() 
         : base(new ErrorMessageSerializer(new Mock<ITlvStreamSerializer>().Object))
      { }

      protected override ErrorMessage WithRandomMessage(Random random)
      {
         var randomData = new byte[36];
         random.NextBytes(randomData);
         
         return new ErrorMessage
         {
            ChannelId = randomData.AsSpan(0,32).ToArray(),
            Len = (ushort)randomData.Length,
            Data = randomData
         };
      }
         

      protected override void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer, ErrorMessage message)
      {
         Assert.Equal(32 + 2 + 36, outputBuffer.WrittenCount);
         Assert.NotEmpty(outputBuffer.WrittenSpan.ToArray());
      }

      protected override void AssertMessageDeserialized(ErrorMessage baseMessage, ErrorMessage expectedMessage)
      {
         Assert.Equal(baseMessage.Data,expectedMessage.Data);
         Assert.Equal(baseMessage.Len,expectedMessage.Len);
         Assert.Equal(baseMessage.ChannelId, expectedMessage.ChannelId);
      }

      protected override IEnumerable<(string, ErrorMessage)> GetData()
      {
         yield return("0x000000000000000000000000000000000000000000000000000000000000000000305468652041534349492074657874206d6573736167652073656e742061732070617274206f6620746865206572726f72",
            new ErrorMessage
            {
               ChannelId = new byte[32],
               Len = 48,
               Data = Encoding.ASCII.GetBytes("The ASCII text message sent as part of the error")
            });
         yield return("0x044f355bdcb7cc0af728ef3cceb9615d90684bb5b2ca5f859ab0f0b70407587100305468652041534349492074657874206d6573736167652073656e742061732070617274206f6620746865206572726f72",
            new ErrorMessage
            {
               ChannelId = "0x044f355bdcb7cc0af728ef3cceb9615d90684bb5b2ca5f859ab0f0b704075871".ToByteArray(),
               Len = 48,
               Data = Encoding.ASCII.GetBytes("The ASCII text message sent as part of the error")
            });
      }
   }
}
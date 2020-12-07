using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Network.Protocol.Messages;
using Network.Protocol.Serialization;
using Network.Protocol.Serialization.Serializers.Messages;
using Network.Protocol.TlvStreams;
using Network.Protocol.TlvStreams.Serializers;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public class InitMessageSerializerTests : BaseMessageSerializerTests<InitMessage>
   {
      public InitMessageSerializerTests()
         : base(new InitMessageSerializer(new TlvStreamSerializer(new Mock<ILogger<TlvStreamSerializer>>().Object,
               new List<ITlvRecordSerializer> { new NetworksTlvSerializer(), new TestDummyTlvSerializer(3) })))
      {
      }

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

         if (expectedMessage.Extension != null)
         {
            Assert.Equal(baseMessage.Extension.Records.Count, expectedMessage.Extension.Records.Count);

            for (int i = 0; i < expectedMessage.Extension.Records.Count; i++)
            {
               Assert.Equal(baseMessage.Extension.Records[i].Type, expectedMessage.Extension.Records[i].Type);
            }
         }
      }

      protected override IEnumerable<(string, InitMessage)> GetData()
      {
         // 0x001000000000 from Bolt 1 without type as it is not sent to the serializer
         yield return ("0x00000000", new InitMessage());
         yield return ("0x0000000001012a030104",
            new InitMessage()
            {
               Extension = new TlVStream()
               {
                  Records = new List<TlvRecord> { new TlvRecord() { Type = 1 }, new TlvRecord() { Type = 3 } }
               }
            });

         // invalid messages
         //yield return ("0000000001", new InitMessage());
         //yield return ("0000000002012a", new InitMessage());
         //yield return ("00000000010101010102", new InitMessage());
      }
   }
}
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using Moq;
using Network.Protocol;
using Network.Protocol.Messages;
using Network.Protocol.Serialization;
using Network.Protocol.Serialization.Serializers.Messages;
using Network.Protocol.Serialization.Serializers.Types;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public class PingMessageSerializerTests
   {
      PingMessageSerializer _serializer;

      private static Random _random = new Random();

      NetworkPeerContext _context = new NetworkPeerContext( //TODO David change this when moved to interface
         new Mock<ILogger>().Object,
         new Mock<IEventBus>().Object,
         PeerConnectionDirection.Inbound, //TODO create a random enum generator
         _random.Next(int.MaxValue).ToString(), // string peerId,
         new Mock<IPEndPoint>((long)_random.Next(int.MaxValue),9735).Object,
         new Mock<IPEndPoint>((long)_random.Next(int.MaxValue),9735).Object,
         new Mock<IPEndPoint>((long)_random.Next(int.MaxValue),9735).Object,
         new Mock<INetworkMessageWriter>().Object
      );
      
      static PingMessage WithRandomPingMessage(Random random) 
         => new PingMessage((ushort)random.Next(PingMessage.MAX_BYTES_LEN));
      
      [Fact]
      public void SerializesTheMessageToOutput()
      {
         var random = new Random();

         var message = WithRandomPingMessage(random);

         _serializer = new PingMessageSerializer(new TlvStreamSerializer(
            new Mock<ILogger<TlvStreamSerializer>>().Object, new List<ITlvRecordSerializer>()));

         var outputBuffer = new ArrayBufferWriter<byte>();

         _serializer.SerializeMessage(message, 0, _context, outputBuffer);

         Assert.Equal(outputBuffer.WrittenCount,2 + 2 + message.Ignored!.Length);
         Assert.NotEmpty(outputBuffer.WrittenSpan.ToArray());
      }

      
      [Theory]
      [InlineData(10,65521,"0xf1ff0a0000000000000000000000")]
      public void DeserializesTheMessageToOutput(ushort bytesLength,ushort numPongBytes,string serializedPingMessageHex)
      {
         _serializer = new PingMessageSerializer(new TlvStreamSerializer(
            new Mock<ILogger<TlvStreamSerializer>>().Object, new List<ITlvRecordSerializer>()));

         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(serializedPingMessageHex.ToByteArray()));
         
         var result= _serializer.DeserializeMessage(ref reader , 0, _context);
         
         Assert.NotNull(result);
         Assert.Equal(result.BytesLen,bytesLength);
         Assert.Equal(result.NumPongBytes,numPongBytes);
         Assert.Equal(result.Ignored,new byte[bytesLength]);
      }
   }
}
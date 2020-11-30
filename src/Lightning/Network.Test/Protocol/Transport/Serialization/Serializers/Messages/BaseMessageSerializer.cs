using Network.Protocol;
using Network.Protocol.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.EventBus;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Protocol;
using Moq;
using Network.Test.Protocol.Transport.Noise;
using Xunit;

namespace Network.Test.Protocol.Transport.Serialization.Serializers.Messages
{
   public abstract class BaseMessageSerializerTests<TMessage> 
      where TMessage : BaseMessage, new()
   {
      protected readonly BaseMessageSerializer<TMessage> serializer;

      private static readonly Random _random = new Random();

      protected NetworkPeerContext context;
      
      protected BaseMessageSerializerTests(BaseMessageSerializer<TMessage> serializer)
      {
         this.serializer = serializer;
         
         context = new NetworkPeerContext( //TODO David change this when moved to interface
            new Mock<ILogger>().Object,
            new Mock<IEventBus>().Object,
            PeerConnectionDirection.Inbound, //TODO create a random enum generator
            _random.Next(int.MaxValue).ToString(), // string peerId,
            new Mock<IPEndPoint>((long)_random.Next(int.MaxValue),9735).Object,
            new Mock<IPEndPoint>((long)_random.Next(int.MaxValue),9735).Object,
            new Mock<IPEndPoint>((long)_random.Next(int.MaxValue),9735).Object,
            new Mock<INetworkMessageWriter>().Object
         );
      }

      protected abstract TMessage WithRandomMessage(Random random);
      protected abstract void AssertExpectedSerialization(ArrayBufferWriter<byte> outputBuffer, TMessage message);
      protected abstract void AssertMessageDeserialized(TMessage baseMessage, TMessage expectedMessage);
      protected abstract (string, TMessage) GetData();

      [Fact]
      public void SerializesTheMessageToOutput()
      {
         var random = new Random();

         TMessage message = WithRandomMessage(random);

         var outputBuffer = new ArrayBufferWriter<byte>();

         serializer.SerializeMessage(message, 0, context, outputBuffer);

         AssertExpectedSerialization(outputBuffer, message);
      }


      [Fact]
      public void DeserializesTheMessageToOutput()
      {
         var (messageHex, expectedMessage) = GetData();
         
         var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(messageHex.ToByteArray()));
         
         var result= serializer.DeserializeMessage(ref reader , 0, context);
         
         AssertMessageDeserialized(result, expectedMessage);
      }
   }
}
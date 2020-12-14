using System;
using System.Buffers;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;
using MithrilShards.Core.Network;
using MithrilShards.Core.Network.Client;
using MithrilShards.Core.Network.Protocol;
using MithrilShards.Core.Network.Protocol.Serialization;
using MithrilShards.Network.Bedrock;
using Network.Protocol.Messages;
using Network.Protocol.Transport.Noise;
using NoiseProtocol;

namespace Network.Protocol.Transport
{
   /// <summary>
   /// Class to handle the common transport protocol.
   /// Based on BOLT8 and BOLT1
   /// </summary>
   public class TransportMessageSerializer : INetworkProtocolMessageSerializer
   {
      private readonly ILogger<TransportMessageSerializer> _logger;
      private readonly INetworkMessageSerializerManager _networkMessageSerializerManager;
      private readonly INoiseProtocol _noiseProtocol;
      private readonly NodeContext _nodeContext;

      private NetworkPeerContext _networkPeerContext;
      private IHandshakeProtocol _handshakeProtocol;
      private readonly DeserializationContext _deserializationContext;

      public TransportMessageSerializer(
         ILogger<TransportMessageSerializer> logger,
         INetworkMessageSerializerManager networkMessageSerializerManager,
         NodeContext nodeContext,
         INoiseProtocol handshakeStateFactory)
      {
         _logger = logger;
         _networkMessageSerializerManager = networkMessageSerializerManager;
         _nodeContext = nodeContext;
         _noiseProtocol = handshakeStateFactory;
         _deserializationContext = new DeserializationContext();

         //initialized by SetPeerContext
         _networkPeerContext = null!;
         _handshakeProtocol = null!;
      }

      public void SetPeerContext(IPeerContext peerContext)
      {
         _networkPeerContext = peerContext as NetworkPeerContext ?? throw new ArgumentException("Expected NetworkPeerContext", nameof(peerContext)); ;
         _deserializationContext.SetInitiator(_networkPeerContext.Direction == PeerConnectionDirection.Outbound);

         LightningEndpoint? lightningEndpoint = null;
         if (peerContext.Direction == PeerConnectionDirection.Outbound)
         {
            OutgoingConnectionEndPoint endpoint = peerContext.Features.Get<OutgoingConnectionEndPoint>();

            if (endpoint == null || !endpoint.Items.TryGetValue(nameof(LightningEndpoint), out object? res))
            {
               _logger.LogError("Remote connection was not found ");
               throw new ApplicationException("Initiator connection must have a public key of the remote node");
            }

            if (res == null)
            {
               _logger.LogError("Remote connection type is invalid");
               throw new ApplicationException("Remote connection type is invalid");
            }

            lightningEndpoint = (LightningEndpoint)res;
         }

         _handshakeProtocol = new HandshakeWithNoiseProtocol(_nodeContext, lightningEndpoint?.NodePubKey, _noiseProtocol);
         _networkPeerContext.SetHandshakeProtocol(_handshakeProtocol);
      }

      public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, /*[MaybeNullWhen(false)]*/  out INetworkMessage message)
      {
         var reader = new SequenceReader<byte>(input);

         if (_networkPeerContext.HandshakeComplete)
         {
            if (!_deserializationContext.EncryptedMessageLengthRead)
            {
               if (reader.Remaining < _handshakeProtocol.HeaderLength)
               {
                  // the payload header is 18 bytes (2 byte payload length and 16 byte MAC)
                  // if the buffer does not have that amount of bytes we wait for more
                  // bytes to arrive in the stream before parsing the header
                  message = default;
                  return false;
               }

               ReadOnlySequence<byte> encryptedHeader = reader.Sequence.Slice(reader.Position.GetInteger(), _handshakeProtocol.HeaderLength);
               
               // decrypt the message length
               _deserializationContext.MessageLength = _handshakeProtocol.ReadMessageLength(encryptedHeader);
               reader.Advance(_handshakeProtocol.HeaderLength);
               consumed = examined = reader.Position;
            }

            if (reader.Remaining < _deserializationContext.MessageLength)
            {
               // if the reader does not have the entire length of the header
               // and message we wait for more data from the stream
               message = default;
               return false;
            }

            var decryptedOutput = new ArrayBufferWriter<byte>();
            ReadOnlySequence<byte> encryptedMessage = reader.Sequence.Slice(reader.Position, (int)_deserializationContext.MessageLength);
            _handshakeProtocol.ReadMessage(encryptedMessage, decryptedOutput);
            _networkPeerContext.Metrics.Received((int)_deserializationContext.MessageLength);

            // reset the reader and message flags
            reader.Advance(_deserializationContext.MessageLength);

            // if(examined.GetInteger() != reader.Position.GetInteger())
            //  throw new IndexOutOfRangeException();

            consumed = examined = reader.Position;

            _deserializationContext.MessageLength = 0;

            // now try to read the payload
            var payload = new ReadOnlySequence<byte>(decryptedOutput.WrittenMemory);
            var payloadReader = new SequenceReader<byte>(payload);

            ushort command = payloadReader.ReadUShort(isBigEndian: true);
            string commandName = command.ToString();
            ReadOnlySequence<byte> innerPayload = payload.Slice(2);

            if (_networkMessageSerializerManager.TryDeserialize(
               commandName,
               ref innerPayload,
               _networkPeerContext.NegotiatedProtocolVersion.Version,
               _networkPeerContext, out message!))
            {
               return true;
            }
            else
            {
               _logger.LogWarning("Serializer for message '{Command}' not found.", commandName);
               message = new UnknownMessage(commandName, innerPayload.ToArray());
               _networkPeerContext.Metrics.Wasted(decryptedOutput.WrittenCount);
               return true;
            }
         }
         else
         {
            // During the handshake the byte sequence is passed as is to the
            // underline processor which will handle serialization of the message.

            long nextLength = _deserializationContext.NextLength();
            if (reader.Remaining >= nextLength)
            {
               message = new HandshakeMessage { Payload = input.Slice(reader.Position, nextLength) };
               reader.Advance(nextLength);
               if (examined.GetInteger() != reader.Position.GetInteger())
                  throw new IndexOutOfRangeException();

               consumed = examined;
               _deserializationContext.AdvanceStep();
               return true;
            }

            message = default;
            return false;
         }
      }

      public void WriteMessage(INetworkMessage message, IBufferWriter<byte> output)
      {
         if (message is null)
         {
            throw new ArgumentNullException(nameof(message));
         }

         if (_networkPeerContext.HandshakeComplete)
         {
            string command = message.Command;
            using (_logger.BeginScope("Serializing and sending '{Command}'", command))
            {
               var payloadOutput = new ArrayBufferWriter<byte>();

               // type: write command type, a 2-byte big-endian field indicating the type of message
               payloadOutput.WriteUShort(ushort.Parse(command), isBigEndian: true);

               if (_networkMessageSerializerManager.TrySerialize(
                  message,
                  _networkPeerContext.NegotiatedProtocolVersion.Version,
                  _networkPeerContext,
                  payloadOutput))
               {
                  // encrypted Lightning message:
                  // 2-byte encrypted message length
                  // 16-byte MAC of the encrypted message length
                  // The encrypted Lightning message
                  // 16-byte MAC of the Lightning message
                  _handshakeProtocol.WriteMessage(new ReadOnlySequence<byte>(payloadOutput.WrittenMemory), output);
                  _networkPeerContext.Metrics.Sent(payloadOutput.WrittenCount);
                  _logger.LogDebug("Sent message '{Command}' with payload size {PayloadSize}.", command, payloadOutput.WrittenCount);
               }
               else
               {
                  _logger.LogError("Serialize for message '{Command}' not found.", command);
               }
            }
         }
         else
         {
            using (_logger.BeginScope("Noise handshake"))
            {
               // During the handshake the byte sequence is passed as is to the
               // remote peer which, the serialization was handled by the processor.

               if (message is HandshakeMessage noiseMessage)
               {
                  foreach (ReadOnlyMemory<byte> memory in noiseMessage.Payload)
                  {
                     output.Write(memory.Span);
                  }
               }
               else
               {
                  _logger.LogError("Invalid handshake message");
                  throw new ApplicationException("Invalid handshake message");
               }
            }
         }
      }

      internal class DeserializationContext
      {
         public DeserializationContext()
         {
            _handshakeStep = 1;
         }

         private bool _initiator;
         private byte _handshakeStep;
         public long MessageLength { get; set; }

         /// <summary>
         /// The lightning payload contains a 2 byte header and a 16 byte mac.
         /// If the header is already found then skip directly to reading the message.
         /// </summary>
         public bool EncryptedMessageLengthRead
         {
            get
            {
               return MessageLength > 0;
            }
         }

         public void SetInitiator(bool initiator)
         {
            _initiator = initiator;
         }

         public long NextLength()
         {
            if (_initiator)
            {
               if (_handshakeStep == 1)
               {
                  return 50; // act two
               }

               throw new SerializationException("Invalid handshake state");
            }
            else
            {
               if (_handshakeStep == 1)
               {
                  return 50; // act two
               }

               if (_handshakeStep == 2)
               {
                  return 66; // act two
               }

               throw new SerializationException("Invalid handshake state");
            }
         }

         public void AdvanceStep()
         {
            _handshakeStep++;
         }
      }
   }
}
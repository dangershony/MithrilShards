using System.Buffers;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol.Messages;

namespace Network.Protocol.Serialization.Serializers.Messages
{
   public class BaseMessageSerializer<TMessage> : NetworkMessageSerializerBase<TMessage> where TMessage : BaseMessage
   {
      readonly IRecordSerializerManager recordSerializerManager;

      public BaseMessageSerializer(IRecordSerializerManager recordSerializerManager)
      {
         this.recordSerializerManager = recordSerializerManager;
      }

      protected void SerializeStream(TMessage message, IBufferWriter<byte> output)
      {
         foreach (var record in message.Extension.Records)
         {

         }
      }

      public override InitMessage Deserialize(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext)
      {
         var message = DeserializeMessage(ref reader, protocolVersion, peerContext);
      }

      protected abstract InitMessage DeserializeMessage(ref SequenceReader<byte> reader, int protocolVersion, NetworkPeerContext peerContext);

      protected void DeserializeStream(ref SequenceReader<byte> reader, ref TMessage message)
      {
         /// while !end_of_stram
         while (true) //until end of tlvstream
         {
            ///   read record type
            ulong recordType = reader.ReadBigSize();

            ///   check if known type
            if (!this.recordSerializerManager.TryGetType(recordType, out IRecordSerializer? recordSerializer))
            {
               // type unknown
               if (recordType % 2 == 0)
               {
                  //if even, throw
                  throw new MessageSerializationException("TlvSerialization error, sequence error");
               }
               else
               {
                  ///   read record length
                  ulong recordLength = reader.ReadBigSize(); //we read but ignore

                  ///   read record value (we aren't interested in these bytes so we just advance)
                  reader.Advance((long)recordLength);
               }
            }
            else
            {
               ///   read record length
               ulong recordLength = reader.ReadBigSize(); //we skip this

               message.Extension.Records.Add(recordSerializer.Deserialize(ref reader));
            }

            //
         }


         ///
         ///   read record length
         ///   read record value
      }
   }
}
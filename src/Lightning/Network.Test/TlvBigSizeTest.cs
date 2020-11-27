using System.Buffers;
using System.IO;
using System.Text.Json;
using MithrilShards.Core.Encoding;
using MithrilShards.Core.Network.Protocol.Serialization;
using Network.Protocol;
using Xunit;
using SequenceReaderExtensions = Network.Protocol.SequenceReaderExtensions;

namespace Network.Test
{
   public class TlvBigSizeTest
   {
      [Fact]
      public void BigSizeDecodingDataTest()
      {
         string rawData = File.ReadAllText("Data/BigSizeDecodingData.json");
         TlvData[] data = JsonSerializer.Deserialize<TlvData[]>(rawData);

         foreach (TlvData tlvData in data)
         {
            byte[] dataBytes = HexEncoder.ToHexBytes(tlvData.bytes);

            if (tlvData.exp_error != null)
            {
               Assert.Throws<MessageSerializationException>(() =>
               {
                  var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(dataBytes));
                  return SequenceReaderExtensions.ReadBigSize(ref reader);
               });
            }
            else
            {
               var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(dataBytes));
               ulong res = SequenceReaderExtensions.ReadBigSize(ref reader);
               Assert.Equal(tlvData.value, res);
            }
         }
      }

      [Fact]
      public void BigSizeEncodingDataTest()
      {
         string rawData = File.ReadAllText("Data/BigSizeEncodingData.json");
         TlvData[] data = JsonSerializer.Deserialize<TlvData[]>(rawData);

         foreach (TlvData tlvData in data)
         {
            byte[] dataBytes = HexEncoder.ToHexBytes(tlvData.bytes);

            var writer = new ArrayBufferWriter<byte>();
            writer.WriteBigSize(tlvData.value);
            Assert.Equal(dataBytes, writer.WrittenSpan.ToArray());
         }
      }

      internal class TlvData
      {
         public string name { get; set; }
         public ulong value { get; set; }
         public string bytes { get; set; }
         public string exp_error { get; set; }
      }
   }
}
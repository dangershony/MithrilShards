using System;
using System.Buffers;
using System.Security.Cryptography;
using Bitcoin.Primitives.Fundamental;
using Moq;
using NBitcoin;
using Network.Protocol;
using Network.Protocol.Messages;
using Network.Protocol.Messages.Gossip;
using Network.Protocol.TlvStreams;
using Network.Protocol.Validators.Gossip;
using Xunit;

namespace Network.Test.Validators
{
   public class NodeAnnouncementValidatorTests
   {
      NodeAnnouncementValidator _validator;

      void WithNewValidator() => _validator = new NodeAnnouncementValidator(new Mock<ITlvStreamSerializer>().Object);

      static void AssertFailedValidation((bool, ErrorMessage) result)
      {
         Assert.False(result.Item1);
         Assert.Null(result.Item2);
      }
      
      (bool, ErrorMessage) WhenValidateMessageIsCalled(NodeAnnouncement nodeAnnouncement)
      {
         return _validator.ValidateMessage(nodeAnnouncement);
      }
      
      [Fact]
      public void ReturnsFalseWhenNodeIdIsNotValidPubKey()
      {
         WithNewValidator();

         var random = new Random();

         byte[] nodeId = new byte[33];
         
         random.NextBytes(nodeId);

         var result = WhenValidateMessageIsCalled(new NodeAnnouncement
         {
            NodeId = (PublicKey) nodeId
         });
         
         AssertFailedValidation(result);
      }

      [Fact]
      public void ReturnsFalseWhenTheSignatureIsNotValid()
      {
         WithNewValidator();

         var random = new Random();

         byte[] signature = new byte[CompressedSignature.SIGNATURE_LENGTH];
         
         random.NextBytes(signature);

         var result = WhenValidateMessageIsCalled(new NodeAnnouncement
         {
            NodeId = (PublicKey) new Key().PubKey.ToBytes(),
            Signature = (CompressedSignature) signature
         });
         
         AssertFailedValidation(result);
      }

      

      [Fact]
      public void ReturnsTrueIfAllParametersAreValid()
      {
         WithNewValidator();

         var key = new Key();
         
         var message = new NodeAnnouncement
         {
            NodeId = (PublicKey) key.PubKey.ToBytes()
         };
         var output = new ArrayBufferWriter<byte>();

         output.WriteUShort(message.Len, true);
         output.WriteBytes(message.Features);
         output.WriteUInt(message.Timestamp, true);
         output.WriteBytes(message.NodeId);
         output.WriteBytes(message.RgbColor);
         output.WriteBytes(message.Alias);
         output.WriteUShort(message.Addrlen, true);
         output.WriteBytes(message.Addresses);

         using var sha256 = SHA256.Create();
         sha256.ComputeHash(output.WrittenMemory.ToArray());
         byte[] hash = sha256.ComputeHash(sha256.Hash);

         var ecSig = key.Sign(new uint256(hash));
         
         message.Signature = (CompressedSignature) ecSig.ToCompact();
         
         var result = WhenValidateMessageIsCalled(message);
         
         Assert.True(result.Item1);
         Assert.Null(result.Item2);
      }
   }
}
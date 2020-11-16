﻿using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace MithrilShards.Network.Bedrock
{
   public class ConnectionContextData
   {
      /// <summary>
      /// The default maximum protocol message length.
      /// </summary>
      private const uint DEFAULT_MAX_PROTOCOL_MESSAGE_LENGTH = 4_000_000;

      public const int SIZE_MAGIC = 4;
      public const int SIZE_COMMAND = 12;
      public const int SIZE_PAYLOAD_LENGTH = 4;
      public const int SIZE_CHECKSUM = 4;
      public const int HEADER_LENGTH = SIZE_MAGIC + SIZE_COMMAND + SIZE_PAYLOAD_LENGTH + SIZE_CHECKSUM;

      private uint payloadLength;
      public bool PayloadLengthRead { get; private set; }

      public bool CommandRead { get; private set; }

      [DisallowNull]
      public string? CommandName { get; private set; }

      /// <summary>
      /// The maximum allowed protocol message length.
      /// </summary>
      private readonly uint maximumProtocolMessageLength;

      /// <summary>
      /// Gets or sets the length of the last parsed INetworkMessage payload (message length - header length).
      /// Sets PayloadRead to true.
      /// </summary>
      /// <value>
      /// The length of the payload.
      /// </value>
      public uint PayloadLength
      {
         get => this.payloadLength;
         set
         {
            if (value > this.maximumProtocolMessageLength)
            {
               throw new ProtocolViolationException($"Message size exceeds the maximum value {this.maximumProtocolMessageLength}.");
            }
            this.payloadLength = value;
            this.PayloadLengthRead = true;
         }
      }

      public ConnectionContextData(uint maximumProtocolMessageLength = DEFAULT_MAX_PROTOCOL_MESSAGE_LENGTH)
      {
         this.maximumProtocolMessageLength = maximumProtocolMessageLength;

         this.ResetFlags();
      }

      public void ResetFlags()
      {
         this.PayloadLengthRead = false;
         this.CommandRead = false;
      }

      /// <summary>
      /// Gets or sets the command that will instruct how to parse the INetworkMessage payload.
      /// Sets CommandRead to true.
      /// </summary>
      /// <value>
      /// The raw byte of command part of the message header (expected 12 chars right padded with '\0').
      /// </value>
      public void SetCommand(ref ReadOnlySequence<byte> command)
      {
         this.CommandName = Encoding.ASCII.GetString((command.IsSingleSegment ? command.FirstSpan : command.ToArray()).Trim((byte)'\0'));
         this.CommandRead = true;
      }

      public int GetTotalMessageLength()
      {
         return HEADER_LENGTH + (int)this.payloadLength;
      }
   }
}
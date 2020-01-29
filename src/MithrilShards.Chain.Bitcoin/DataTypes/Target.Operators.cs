﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      public static Target operator <<(Target? a, int shiftAmount)
      {
         return a;
      }

      public static Target LeftShift(Target a, int shiftAmount)
      {
         return a << shiftAmount;
      }


      //public static arith_uint256 FromCompact(uint compactValue)
      //{
      //   byte exponent = (byte)(compactValue >> 24); // number of bytes of N
      //   uint mantissa = compactValue & 0x007fffff;

      //   /// 0x00800000 is the mask to use to obtain the sign, if needed.
      //   /// Actually this type is only used to express the difficult Target so it's not needed.

      //   //if (exponent <= 3)
      //   //{
      //   //   mantissa >>= 8 * (3 - exponent);
      //   //   *this = mantissa;
      //   //}
      //   //else
      //   //{
      //   //   *this = mantissa;
      //   //   *this <<= 8 * (exponent - 3);
      //   //}
      //   //if (pfNegative)
      //   //   *pfNegative = mantissa != 0 && (compactValue & 0x00800000) != 0;
      //   //if (pfOverflow)
      //   //   *pfOverflow = mantissa != 0 && ((exponent > 34) ||
      //   //                                (mantissa > 0xff && exponent > 33) ||
      //   //                                (mantissa > 0xffff && exponent > 32));
      //   //return *this;
      //}
   }
}
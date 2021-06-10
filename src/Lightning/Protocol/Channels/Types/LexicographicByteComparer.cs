﻿using System;
using System.Collections.Generic;

namespace Protocol.Channels.Types
{
   public class LexicographicByteComparer : IComparer<byte[]>
   {
      public int Compare(byte[] x, byte[] y)
      {
         int lenRet = x.Length.CompareTo(y.Length);

         if (lenRet != 0) return lenRet;

         int len = Math.Min(x.Length, y.Length);
         for (int i = 0; i < len; i++)
         {
            int c = x[i].CompareTo(y[i]);
            if (c != 0)
            {
               return c;
            }
         }

         return 0;
      }
   }
}
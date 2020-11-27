using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Network.Protocol.Transport.Noise
{
   /// <summary>
   /// Various utility functions.
   /// </summary>
   public static class Utilities
   {

      // NoOptimize to prevent the optimizer from deciding this call is unnecessary.
      // NoInlining to prevent the inliner from forgetting that the method was NoOptimize.
      /// <summary>
      ///
      /// </summary>
      /// <param name="buffer"></param>
      [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
      public static void ZeroMemory(Span<byte> buffer)
      {
         buffer.Clear();
      }   
   }
}
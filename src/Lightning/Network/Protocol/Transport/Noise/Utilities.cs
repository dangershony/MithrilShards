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
		private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

		/// <summary>
		/// Alignes the pointer up to the nearest alignment boundary.
		/// </summary>
		public static IntPtr Align(IntPtr ptr, int alignment)
		{
			ulong mask = (ulong)alignment - 1;
			return (IntPtr)(((ulong)ptr + mask) & ~mask);
		}

		/// <summary>
		/// Generates a cryptographically strong pseudorandom sequence of n bytes.
		/// </summary>
		public static byte[] GetRandomBytes(int n)
		{
			Debug.Assert(n > 0);

			var bytes = new byte[n];
			random.GetBytes(bytes);

			return bytes;
		}

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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		public static byte[] GetByteArrayFromHexString(string hex)
		{
			if (string.IsNullOrEmpty(hex)) return null;
			
			var startIndex = hex.ToLower().StartsWith("0x") ? 2 : 0;
			
			return Enumerable.Range(startIndex, hex.Length - startIndex)
				.Where(x => x % 2 == 0)
				.Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
				.ToArray();
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="arr"></param>
		/// <returns></returns>
		public static string GetHexStringFromByteArray(byte[] arr)
		{
			var sb = new StringBuilder();
			sb.Append("0x");
			foreach (byte b in arr)
				sb.Append(b.ToString("X2"));

			return sb.ToString().ToLower();
		}
	}
}

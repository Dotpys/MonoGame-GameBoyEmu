using System;
using System.Collections.Generic;
using System.Text;

namespace JMGBE.Core
{
	public static class StaticMethods
	{
		/// <summary>
		/// Checks whether the bit in the p position is set or not.
		/// </summary>
		/// <param name="position">The position of the bit to test (0 is LSB, 7 is MSB)</param>
		/// <returns>True if bit is set (1) or false if bit is reset(0).</returns>
		public static bool CheckBit(this byte value, byte position) => (value & (1 << position)) != 0;
		public static void SetBit(this byte b, byte p, bool v)
		{
			if (b.CheckBit(p) != v)
				_ = (byte)(1 << p);
		}
	}
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JMGBE.Core
{
	public static class StaticMethods
	{
		/*
		 * Byte
		 */

		/// <summary>
		/// Checks whether the bit in the given position is set or not.
		/// </summary>
		/// <param name="position">The position of the bit to test (0 is LSB, 7 is MSB)</param>
		/// <returns>True if bit is set (1) or false if bit is reset(0).</returns>
		public static bool CheckBit(this byte value, int position) => (value & (1 << position)) != 0;

		/// <summary>
		/// Sets the bit in the given position high or low.
		/// </summary>
		/// <param name="position">The position of the bit to set (0 is LSB, 7 is MSB)</param>
		/// <param name="newBitValue">The value that the new bit should have (true is high (1), false is low (0))</param>
		public static void SetBit(this ref byte currentValue, byte position, bool newBitValue)
		{
			currentValue = newBitValue ? (byte)(currentValue | (1 << position)) : (byte)(currentValue & (0xFF - (1 << position)));
		}
	}
}
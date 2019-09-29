using System;
using System.Collections.Generic;
using System.Text;

namespace JMGBE.Core
{	
	/// <summary>
	/// The base class used to represent any type of memory.
	/// </summary>
	/// <typeparam name="T">
	/// The numeric type used to represent an address in memory.
	/// </typeparam>
	public abstract class MemoryBase<T>
	{
		public abstract void WriteByte(T address, byte value);
		public abstract byte ReadByte(T address);
	}
}

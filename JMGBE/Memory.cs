using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMGBE.Core
{
	public class Memory : MemoryBase<ushort>
	{
		public Memory<byte> raw_memory;

		public Memory()
		{
			raw_memory = new byte[65536];
			File.ReadAllBytes("bootloader.bin").CopyTo(raw_memory);
		}

		public override byte ReadByte(ushort address)
		{
			return raw_memory.Span[address];
		}

		public override void WriteByte(ushort address, byte value)
		{
			raw_memory.Span[address] = value;
			//Simuliamo la regione di memoria 'echo'.
			if (address >= 0xE000 & address < 0xFE00)
				raw_memory.Span[address - 0x2000] = value;
			else if (address >= 0xC000 & address < 0xDE00)
				raw_memory.Span[address + 0x2000] = value;
		}

		public override ushort ReadUShort(ushort address)
		{
			return BitConverter.ToUInt16(raw_memory.Slice(address, 2).Span);
		}

		public override void WriteUShort(ushort address, ushort value)
		{
			BitConverter.GetBytes(value).CopyTo(raw_memory.Slice(address, 2));
		}
	}
}
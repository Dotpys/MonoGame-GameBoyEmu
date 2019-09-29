using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMGBE.Core
{
	public class Memory : MemoryBase<ushort>
	{

		public byte[] memory;
		private const ushort BASE_CARTRIDGE_ADDR = 0x0000;

		public Memory()
		{
			memory = new byte[65536];
			File.ReadAllBytes("bootloader.bin").CopyTo(memory, BASE_CARTRIDGE_ADDR);
			//File.ReadAllBytes(@"C:\Users\Julien\Desktop\tetris.gb").CopyTo(memory, BASE_CARTRIDGE_ADDR);
		}

		public override byte ReadByte(ushort address)
		{
			return memory[address];
		}

		public override void WriteByte(ushort address, byte value)
		{
			memory[address] = value;
			//Simuliamo la regione di memoria 'echo'.
			if (address >= 0xE000 & address < 0xFE00)
				memory[address - 0x2000] = value;
			else if (address >= 0xC000 & address < 0xDE00)
				memory[address + 0x2000] = value;
		}
	}
}

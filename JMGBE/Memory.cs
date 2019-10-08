using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMGBE.Core
{
	public class Memory : MemoryBase<ushort>
	{
		private Memory<byte> memory;
		private Memory<byte> cartridge;
		private Memory<byte> video_ram;

		private const ushort CARTRIDGE_BASE_ADDR =	0x0000;
		private const ushort CARTRIDGE_END_ADDR =	0x7fff;

		private const ushort VIDEO_RAM_BASE_ADDR =	0x8000;
		private const ushort VIDEO_RMA_END_ADDR =	0x9fff;

		public Memory()
		{
			memory = new byte[65536];
			cartridge = memory[CARTRIDGE_BASE_ADDR..CARTRIDGE_END_ADDR];
			video_ram = memory[VIDEO_RAM_BASE_ADDR..VIDEO_RMA_END_ADDR];
			File.ReadAllBytes("bootloader.bin").CopyTo(cartridge);
		}

		public override byte ReadByte(ushort address)
		{
			return memory.Span[address];
		}

		public override void WriteByte(ushort address, byte value)
		{
			memory.Span[address] = value;
			//Simuliamo la regione di memoria 'echo'.
			if (address >= 0xE000 & address < 0xFE00)
				memory.Span[address - 0x2000] = value;
			else if (address >= 0xC000 & address < 0xDE00)
				memory.Span[address + 0x2000] = value;
		}

		public override ushort ReadUshort(ushort address)
		{
			return BitConverter.ToUInt16(memory.Slice(address, 2).Span);
		}

		public override void WriteUshort(ushort address, ushort value)
		{
			BitConverter.GetBytes(value).CopyTo(memory.Slice(address, 2));
		}
	}
}
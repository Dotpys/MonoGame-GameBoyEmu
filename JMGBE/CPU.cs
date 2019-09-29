using System;
using System.Threading;

namespace JMGBE.Core
{
	public class CPU
	{
		private FlagRegister registerF;

		public byte RegisterA { get; set; }
		public byte RegisterB { get; set; }
		public byte RegisterC { get; set; }
		public byte RegisterD { get; set; }
		public byte RegisterE { get; set; }
		public byte RegisterF
		{
			get { return (byte)registerF; }
			set { registerF = (FlagRegister)value; }
		}
		public byte RegisterH { get; set; }
		public byte RegisterL { get; set; }

		public ushort RegisterAF
		{
			get { return (ushort)((RegisterA << 8) | (ushort)RegisterF); }
			set
			{
				RegisterA = (byte)(value >> 8);
				RegisterF = (byte)(value & 0b0000000011111111);
			}
		}
		public ushort RegisterBC
		{
			get { return (ushort)((RegisterB << 8) | RegisterC); }
			set
			{
				RegisterB = (byte)(value >> 8);
				RegisterC = (byte)(value & 0b0000000011111111);
			}
		}
		public ushort RegisterDE
		{
			get { return (ushort)((RegisterD << 8) | RegisterE); }
			set
			{
				RegisterD = (byte)(value >> 8);
				RegisterE = (byte)(value & 0b0000000011111111);
			}
		}
		public ushort RegisterHL
		{
			get { return (ushort)((RegisterH << 8) | RegisterL); }
			set
			{
				RegisterH = (byte)(value >> 8);
				RegisterL = (byte)(value & 0b0000000011111111);
			}
		}

		public ushort PC { get; set; }
		public ushort SP { get; set; }

		public bool ZeroFlag
		{
			get { return (RegisterF >> 7) != 0; }
			set { RegisterF |= (byte)(value ? 1 : 0 << 7); }
		}
		public bool SubtractFlag
		{
			get { }
			set { }
		}
		HalfCarryFlag
		CarryFlag

		private readonly Memory _mem;

		public CPU()
		{
			PC = 0x0000;
			_mem = new Memory();
		}

		public void Execute()
		{
			byte instruction = _mem.ReadByte(PC);
			switch (instruction)
			{
				#region 0E LD C, n
				case 0x0E:  //0E LD C, n (8c)
					Console.WriteLine($"{Hex4String(PC)}: LD C, {Hex2String(_mem.memory[PC + 1])}");
					if (!IsBitSet(RegisterF, 7))
						PC = (ushort)(PC + (sbyte)_mem.memory[PC + 1]);
					PC++;
					break;
				#endregion
				#region 20 JR NZ, n
				case 0x20:  //20 JR NZ,n (8c)
					Console.WriteLine($"{Hex4String(PC)}: JR NZ, {Hex2String(_mem.memory[PC + 1])}");
					if (!IsBitSet(RegisterF, 7))
						PC = (ushort)(PC + (sbyte)_mem.memory[PC + 1]);
					PC++;
					break;
				#endregion
				#region 21 LD HL, nn
				case 0x21:  //LD HL, nn (12c)
					RegisterHL = BitConverter.ToUInt16(new Span<byte>(_mem.memory, PC + 1, 2));
					Console.WriteLine($"{Hex4String(PC)}: LD HL, {Hex4String(RegisterHL)}");
					PC += 2;
					break;
				#endregion
				#region 2E LD L, n
				case 0x2E:  //LD L, n (8c)
					Console.WriteLine($"{Hex4String(PC)}: LD L, n");
					break;
				#endregion
				#region 31 LD SP, nn
				case 0x31:  //LD SP, nn (12c)
					SP = BitConverter.ToUInt16(new Span<byte>(_mem.memory, PC + 1, 2));
					Console.WriteLine($"{Hex4String(PC)}: LD SP, {Hex4String(SP)}");
					PC += 2;
					break;
				#endregion
				#region 32 LD (HL-), A
				case 0x32:  //LD (HL-), A (8c)
					RegisterHL = RegisterA;
					RegisterHL--;
					Console.WriteLine($"{Hex4String(PC)}: LDD (HL), A");
					break;
				#endregion
				#region AF XOR A
				case 0xAF:  //XOR A (4c)
					Console.WriteLine($"{Hex4String(PC)}: XOR A");
					RegisterA ^= RegisterA;
					RegisterF = RegisterA == 0 ? (byte)0b10000000 : (byte)0b00000000;
					break;
				#endregion
				#region CB
				case 0xCB:
					var subOpCode = _mem.ReadByte((ushort)(PC + 1));
					switch (subOpCode)
					{
						#region 7C BIT 7, H
						case 0x7C: //BIT 7, H
							Console.WriteLine($"{Hex4String(PC)}: BIT 7, H");
							RegisterF |= IsBitSet(RegisterH, 7) ? (byte)0b00000000 : (byte)0b10000000;
							RegisterF &= 0b10111111;
							RegisterF |= 0b00100000;
							PC += 1;
							break;
						#endregion
						#region ERROR
						default:
							Console.WriteLine($"{Hex4String(PC)}: Instruction not programmed: $CB {Hex2String(subOpCode)}");
							break;
						#endregion
					}
					break;
				#endregion
				#region ERROR
				default:
					Console.WriteLine($"{Hex4String(PC)}: Instruction not programmed: {Hex2String(instruction)}");
					break;
				#endregion
			}
			PC++;
			Thread.Sleep(500);
		}

		private bool IsBitSet(byte b, byte p) => (b & (1 << p)) != 0;

		private string Hex4String(ushort n)
		{
			return string.Format("${0:X4}", n);
		}

		private string Hex2String(byte n)
		{
			return string.Format("${0:X2}", n);
		}

		[Flags]
		private enum FlagRegister : byte
		{
			ZeroFlag = 0x80,
			SubtractFlag = 0x40,
			HalfCarryFlag = 0x20,
			CarryFlag = 0x10
		}
	}
}
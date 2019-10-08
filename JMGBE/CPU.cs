using System;
using System.Text;
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
			get { return IsBitSet(RegisterF, 7); }
			set { if (ZeroFlag != value) RegisterF ^= 0x80; }
		}
		public bool SubtractFlag
		{
			get { return IsBitSet(RegisterF, 6); }
			set { if (SubtractFlag != value) RegisterF ^= 0x40; }
		}
		public bool HalfCarryFlag
		{
			get { return IsBitSet(RegisterF, 5); }
			set { if (HalfCarryFlag != value) RegisterF ^= 0x20; }
		}
		public bool CarryFlag
		{
			get { return IsBitSet(RegisterF, 4); }
			set { if (CarryFlag != value) RegisterF ^= 0x10; }
		}

		private readonly MemoryBase<ushort> _memory;

		public CPU()
		{
			PC = 0x0000;
			_memory = new Memory();
			Console.WriteLine("              Program              |      A      |      F      |      B      |      C      |      D      |      E      |      H      |      L      | AF | BC | DE | HL | PC | SP |");
			Console.WriteLine("-----------------------------------+-------------+-------------+-------------+-------------+-------------+-------------+-------------+-------------+----+----+----+----+----+----+");
		}

		public void Execute()
		{
			byte instruction = _memory.ReadByte(PC);
			switch (instruction)
			{
				#region 01 LD BC, nn
				case 0x01:  //LD BC, nn
					ushort nn01 = _memory.ReadUshort((ushort)(PC + 1));
					RegisterBC = nn01;
					DumpCPU($"LD BC, {Hex4String(nn01)}");
					break;
				#endregion
				#region 04 INC B
				case 0x04:  //INC B
					RegisterB++;
					if (RegisterB == 0) ZeroFlag = true;
					SubtractFlag = false;
					//Che cosa orrenda che ho fatto :D (P.S. non so se funziona)
					HalfCarryFlag = ((((RegisterB - 1) & 0b00001111) + 1) & 0b00010000) == 1;
					DumpCPU($"INC B");
					break;
				#endregion
				#region 05 DEC B
				case 0x05:  //DEC B
					RegisterB--;
					ZeroFlag = RegisterB == 0;
					SubtractFlag = true;
					//Orrendo pt.2
					HalfCarryFlag = (RegisterB & 0b00001111) == 0;
					DumpCPU("DEC B");
					break;
				#endregion
				#region 06 LD B, n
				case 0x06:	//LD B, n
					var n06 = Get8BitOperand();
					RegisterB = n06;
					DumpCPU($"LD B, {Hex2String(n06)}");
					PC++;
					break;
				#endregion
				#region 0C INC C
				case 0x0C:  //INC C
					RegisterC++;
					if (RegisterC == 0) ZeroFlag = true;
					SubtractFlag = false;
					//Che cosa orrenda che ho fatto :D (P.S. non so se funziona)
					HalfCarryFlag = ((((RegisterC-1) & 0b00001111) + 1) & 0b00010000) == 1;
					DumpCPU($"INC C");
					break;
				#endregion
				#region 0D DEC C
				case 0x0D:  //DEC C
					RegisterC--;
					ZeroFlag = RegisterC == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterC & 0b00001111) == 0;
					DumpCPU("DEC C");
					break;
				#endregion
				#region 0E LD C, n
				case 0x0E:  //LD C, n
					byte n0E = Get8BitOperand();
					RegisterC = n0E;
					DumpCPU($"LD C, {Hex2String(n0E)}");
					PC++;
					break;
				#endregion
				#region 11 LD DE, nn
				case 0x11:  //LD DE, nn
					ushort nn11 = Get16BitOperand();
					RegisterDE = nn11;
					DumpCPU($"LD DE, {Hex4String(nn11)}");
					PC += 2;
					break;
				#endregion
				#region 13 INC DE
				case 0x13:  //INC DE
					RegisterDE++;
					DumpCPU("INC DE");
					break;
				#endregion
				#region 15 DEC D
				case 0x15:  //DEC D
					RegisterD--;
					ZeroFlag = RegisterD == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterD & 0b00001111) == 0;
					DumpCPU("DEC D");
					break;
				#endregion
				#region 16 LD D, n
				case 0x16:  //LD D, n
					byte n16 = Get8BitOperand();
					RegisterD = n16;
					DumpCPU($"LD D, {Hex2String(n16)}");
					PC++;
					break;
				#endregion
				#region 17 RLA
				case 0x17:  //RLA
					CarryFlag = RegisterA > 0x7F;
					HalfCarryFlag = false;
					SubtractFlag = false;
					RegisterA <<= 1;
					ZeroFlag = RegisterA == 0;
					DumpCPU("RLA");
					break;
				#endregion
				#region 18 JR n
				case 0x18:  //JR n
					sbyte n18 = (sbyte)Get8BitOperand();

					DumpCPU($"JR ({Hex4String((ushort)(PC + n18))})");
					PC = (ushort)(PC + n18);
					PC += 1;
					break;
				#endregion
				#region 1A LD A, (DE)
				case 0x1A:  //LD A, DE
					RegisterA = (byte)RegisterDE;
					DumpCPU($"LD A, (DE)");
					break;
				#endregion
				#region 1D DEC E
				case 0x1D:  //DEC E
					RegisterE--;
					ZeroFlag = RegisterE == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterE & 0b00001111) == 0;
					DumpCPU("DEC E");
					break;
				#endregion
				#region 1E LD E, n
				case 0x1E:  //LD E, n
					byte n1E = Get8BitOperand();
					RegisterE = n1E;
					DumpCPU($"LD E, {Hex2String(n1E)}");
					PC++;
					break;
				#endregion
				#region 20 JR NZ, n
				case 0x20:  //20 JR NZ,n (8c)
					sbyte n20 = (sbyte)Get8BitOperand();
					DumpCPU($"JR NZ, ({Hex4String((ushort)(PC + n20))})");
					if (!ZeroFlag)
						PC = (ushort)(PC + n20);
					PC++;
					break;
				#endregion
				#region 21 LD HL, nn
				case 0x21:  //LD HL, nn (12c)
					RegisterHL = _memory.ReadUshort((ushort)(PC + 1));
					DumpCPU($"LD HL, {Hex4String(RegisterHL)}");
					PC += 2;
					break;
				#endregion
				#region 22 LD (HL+), A
				case 0x22:  //LD (HL+),A
					RegisterHL = RegisterA;
					RegisterHL++;
					DumpCPU("LD (HL+), A");
					break;
				#endregion
				#region 23 INC HL
				case 0x23:  //INC HL
					RegisterHL++;
					DumpCPU("INC HL");
					break;
				#endregion
				#region 25 DEC H
				case 0x25:  //DEC H
					RegisterH--;
					ZeroFlag = RegisterH == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterH & 0b00001111) == 0;
					DumpCPU("DEC H");
					break;
				#endregion
				#region 26 LD H, n
				case 0x26:  //LD H, n
					byte n26 = Get8BitOperand();
					RegisterH = n26;
					DumpCPU($"LD H, {Hex2String(n26)}");
					PC++;
					break;
				#endregion
				#region 28 JR Z, n
				case 0x28:  //JR Z, n
					sbyte n28 = (sbyte)Get8BitOperand();
					DumpCPU($"JR Z, ({Hex4String((ushort)(PC + n28))})");
					if (ZeroFlag)
						PC = (ushort)(PC + n28);
					PC++;
					break;
				#endregion
				#region 2D DEC L
				case 0x2D:  //DEC L
					RegisterL--;
					ZeroFlag = RegisterL == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterL & 0b00001111) == 0;
					DumpCPU("DEC L");
					break;
				#endregion
				#region 2E LD L, n
				case 0x2E:  //LD L, n
					byte n2E = Get8BitOperand();
					RegisterL = n2E;
					DumpCPU($"LD L, {Hex2String(n2E)}");
					PC++;
					break;
				#endregion
				#region 30 JR NC, n
				case 0x30:  //JR NC, n
					sbyte n30 = (sbyte)Get8BitOperand();
					DumpCPU($"JR NC, ({Hex4String((ushort)(PC + n30))})");
					if (!CarryFlag)
						PC = (ushort)(PC + n30);
					PC++;
					break;
				#endregion
				#region 31 LD SP, nn
				case 0x31:  //LD SP, nn (12c)
					SP = _memory.ReadUshort((ushort)(PC + 1));
					DumpCPU($"LD SP, {Hex4String(SP)}");
					PC += 2;
					break;
				#endregion
				#region 32 LD (HL-), A
				case 0x32:  //LD (HL-), A (8c)
					RegisterHL = RegisterA;
					RegisterHL--;
					DumpCPU($"LD (HL-), A");
					break;
				#endregion
				#region 35 DEC (HL)
				case 0x35:  //DEC (HL)
					RegisterHL--;
					ZeroFlag = RegisterHL == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterHL & 0b00001111) == 0;
					DumpCPU("DEC (HL)");
					break;
				#endregion
				#region 38 JR C, n
				case 0x38:  //JR C, n
					sbyte n38 = (sbyte)Get8BitOperand();
					DumpCPU($"JR C, ({Hex4String((ushort)(PC + n38))})");
					if (!CarryFlag)
						PC = (ushort)(PC + n38);
					PC++;
					break;
				#endregion
				#region 3D DEC A
				case 0x3D:  //DEC A
					RegisterA--;
					ZeroFlag = RegisterA == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterA & 0b00001111) == 0;
					DumpCPU("DEC A");
					break;
				#endregion
				#region 3E LD A, #
				case 0x3E:  //LD A, #
					RegisterA = Get8BitOperand();
					DumpCPU($"LD A, {Hex2String(RegisterA)}");
					PC++;
					break;
				#endregion
				#region 40 LD B, B
				case 0x40:  //LD B, B
					RegisterB = RegisterB;
					DumpCPU("LD B, B");
					break;
				#endregion
				#region 41 LD B, C
				case 0x41:  //LD B, C
					RegisterB = RegisterC;
					DumpCPU("LD B, C");
					break;
				#endregion
				#region 42 LD B, D
				case 0x42:  //LD B, D
					RegisterB = RegisterD;
					DumpCPU("LD B, D");
					break;
				#endregion
				#region 43 LD B, E
				case 0x43:  //LD B, E
					RegisterB = RegisterE;
					DumpCPU("LD B, E");
					break;
				#endregion
				#region 44 LD B, H
				case 0x44:  //LD B, H
					RegisterB = RegisterH;
					DumpCPU("LD B, H");
					break;
				#endregion
				#region 45 LD B, L
				case 0x45:  //LD B, L
					RegisterB = RegisterL;
					DumpCPU("LD B, L");
					break;
				#endregion
				#region 46 LD B, (HL)
				case 0x46:  //LD B, (HL)
					RegisterB = (byte)RegisterHL;
					DumpCPU("LD B, (HL)");
					break;
				#endregion
				#region 47 LD B, A
				case 0x47:  //LD B, A
					RegisterB = RegisterA;
					DumpCPU($"LD B, A");
					break;
				#endregion
				#region 4F LD C, A
				case 0x4F:  //LD C, A
					RegisterC = RegisterA;
					DumpCPU($"LD C, A");
					break;
				#endregion
				#region 77 LD (HL), A
				case 0x77:  //LD (HL), A
					RegisterHL = RegisterA;
					DumpCPU($"LD (HL), A");
					break;
				#endregion
				#region 78 LD A, B
				case 0x78:  //LD A, B
					RegisterA = RegisterB;
					DumpCPU("LD A, B");
					break;
				#endregion
				#region 79 LD A, C
				case 0x79:  //LD A, C
					RegisterA = RegisterC;
					DumpCPU("LD A, C");
					break;
				#endregion
				#region 7A LD A, D
				case 0x7A:  //LD A, D
					RegisterA = RegisterD;
					DumpCPU("LD A, D");
					break;
				#endregion
				#region 7B LD A, E
				case 0x7B:  //LD A, E
					RegisterA = RegisterE;
					DumpCPU("LD A, E");
					break;
				#endregion
				#region 7C LD A, H
				case 0x7C:  //LD A, H
					RegisterA = RegisterH;
					DumpCPU("LD A, H");
					break;
				#endregion
				#region 7D LD A, L
				case 0x7D:  //LD A, L
					RegisterA = RegisterL;
					DumpCPU("LD A, L");
					break;
				#endregion
				#region 7E LD A, (HL)
				case 0x7E:  //LD A, (HL)
					RegisterA = (byte)RegisterHL;
					DumpCPU("LD A, (HL)");
					break;
				#endregion
				#region 7F LD A, A
				case 0x7F:  //LD A, A
					RegisterA = RegisterA;
					DumpCPU("LD A, A");
					break;
				#endregion
				#region AF XOR A
				case 0xAF:  //XOR A (4c)
					DumpCPU($"XOR A");
					RegisterA ^= RegisterA;
					RegisterF = RegisterA == 0 ? (byte)0b10000000 : (byte)0b00000000;
					break;
				#endregion
				#region C1 POP BC
				case 0xC1:
					SP += 2;
					RegisterBC = _memory.ReadUshort(SP);
					DumpCPU("POP BC");
					break;
				#endregion
				#region C5 PUSH BC
				case 0xC5:
					_memory.WriteUshort(SP, RegisterBC);
					SP -= 2;
					DumpCPU("PUSH BC");
					break;
				#endregion
				#region C9 RET
				case 0xC9:  //RET
					DumpCPU("RET");
					SP += 2;
					PC = _memory.ReadUshort(SP);
					break;
				#endregion
				#region CB
				case 0xCB:
					var subOpCode = _memory.ReadByte((ushort)(PC + 1));
					switch (subOpCode)
					{
						#region 11 RL C
						case 0x11:  //RL C
							CarryFlag = RegisterC > 0x7F;
							HalfCarryFlag = false;
							SubtractFlag = false;
							RegisterC <<= 1;
							ZeroFlag = RegisterC == 0;
							DumpCPU("RL C");
							PC++;
							break;
						#endregion
						#region 7C BIT 7, H
						case 0x7C:	//BIT 7, H
							RegisterF |= IsBitSet(RegisterH, 7) ? (byte)0b00000000 : (byte)0b10000000;
							RegisterF &= 0b10111111;
							RegisterF |= 0b00100000;
							DumpCPU("BIT 7, H");
							PC++;
							break;
						#endregion
						#region ERROR
						default:
							DumpCPU($"Instruction not programmed: $CB {Hex2String(subOpCode)}");
							break;
						#endregion
					}
					break;
				#endregion
				#region CD CALL nn
				case 0xCD:  //CALL nn
					_memory.WriteUshort(SP, (ushort)(PC + 2));
					SP -= 2;
					ushort nnCD = Get16BitOperand();
					DumpCPU($"CALL {Hex4String(nnCD)}");
					PC = --nnCD;
					break;
				#endregion
				#region E0 LD ($FF00 + n), A
				case 0xE0:  //LDH (n), A
					byte nE0 = Get8BitOperand();
					DumpCPU($"LD ({Hex4String(0xFF00)} + {Hex2String(nE0)}), A");
					_memory.WriteByte((ushort)(0xFF00 + nE0), RegisterA);
					PC++;
					break;
				#endregion
				#region E2 LD ($FF00 + C), A
				case 0xE2:  //LD ($FF00+C), A
					_memory.WriteByte((ushort)(0xFF00 + RegisterC), RegisterA);
					DumpCPU($"LD {Hex4String((ushort)(0xFF00 + RegisterC))}, {Hex2String(RegisterA)}");
					break;
				#endregion
				#region EA LD (nn), A
				case 0xEA:  //LD (nn), A
					ushort nnEA = Get16BitOperand();
					_memory.WriteByte(nnEA, RegisterA);
					DumpCPU($"LD ({Hex4String(nnEA)}), A");
					PC += 2;
					break;
				#endregion
				#region FE CP #
				case 0xFE:  //CP #
					byte nFE = Get8BitOperand();
					ZeroFlag = RegisterA == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterA & 0b00001111) == 0;
					CarryFlag = RegisterA < nFE;
					DumpCPU($"CP {Hex2String(nFE)}");
					PC++;
					break;
				#endregion
				#region ERROR
				default:
					DumpCPU($"Instruction not programmed: {Hex2String(instruction)}");
					break;
				#endregion
			}
			PC++;
			//Thread.Sleep(5);
		}

		private byte Get8BitOperand()
		{
			return _memory.ReadByte((ushort)(PC + 1));
		}

		private ushort Get16BitOperand()
		{
			return _memory.ReadUshort((ushort)(PC + 1));
		}

#if DEBUG
		private void DumpCPU(string debug)
		{
			Console.Write(string.Format("{0,-35}|", debug));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterA, 2).PadLeft(8, '0'), RegisterA));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterF, 2).PadLeft(8, '0'), RegisterF));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterB, 2).PadLeft(8, '0'), RegisterB));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterC, 2).PadLeft(8, '0'), RegisterC));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterD, 2).PadLeft(8, '0'), RegisterD));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterE, 2).PadLeft(8, '0'), RegisterE));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterH, 2).PadLeft(8, '0'), RegisterH));
			Console.Write(string.Format("{0,8} ({1:X2})|", Convert.ToString(RegisterL, 2).PadLeft(8, '0'), RegisterL));
			Console.Write(string.Format("{0:X4}|", RegisterAF));
			Console.Write(string.Format("{0:X4}|", RegisterBC));
			Console.Write(string.Format("{0:X4}|", RegisterDE));
			Console.Write(string.Format("{0:X4}|", RegisterHL));
			Console.Write(string.Format("{0:X4}|", PC));
			Console.WriteLine(string.Format("{0:X4}|", SP));
		}
#endif
		private bool IsBitSet(byte b, byte p) => (b & (1 << p)) != 0;
		private string Hex4String(ushort n) => string.Format("${0:X4}", n);
		private string Hex2String(byte n) => string.Format("${0:X2}", n);

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
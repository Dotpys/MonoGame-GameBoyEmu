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
			get { return RegisterF.CheckBit(7); }
			set { if (ZeroFlag != value) RegisterF ^= 0x80; }
		}
		public bool SubtractFlag
		{
			get { return RegisterF.CheckBit(6); }
			set { if (SubtractFlag != value) RegisterF ^= 0x40; }
		}
		public bool HalfCarryFlag
		{
			get { return RegisterF.CheckBit(5); }
			set { if (HalfCarryFlag != value) RegisterF ^= 0x20; }
		}
		public bool CarryFlag
		{
			get { return RegisterF.CheckBit(4); }
			set { if (CarryFlag != value) RegisterF ^= 0x10; }
		}

		private readonly MemoryBase<ushort> _memory;
		private short pci = 0;

		public CPU(Memory memory)
		{
			PC = 0x0000;
			_memory = memory;
#if DEBUG
			Console.WriteLine("╔═══════════════════════════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦════╦════╦════╦════╦════╦════╗");
			Console.WriteLine("║              Program              ║      A      ║      F      ║      B      ║      C      ║      D      ║      E      ║      H      ║      L      ║ AF ║ BC ║ DE ║ HL ║ PC ║ SP ║");
			Console.WriteLine("╠═══════════════════════════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬════╬════╬════╬════╬════╬════╣");
			Console.WriteLine("║                                   ║             ║FNHC         ║             ║             ║             ║             ║             ║             ║    ║    ║    ║    ║    ║    ║");
			Console.WriteLine("╠═══════════════════════════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬════╬════╬════╬════╬════╬════╣");
#endif
		}

		public void NextInstruction()
		{
			byte instruction = ReadByteOperand();
#if DEBUG
			if (PC == 0x0008) RegisterHL = 0x7fff;
#endif
			switch (instruction)
			{
				#region 00 NOP
				case 0x00:
					DumpCPU("NOP");
					break;
				#endregion
				#region 01 LD BC, nn
				case 0x01:
					RegisterBC = ReadUShortOperand();
					DumpCPU($"LD BC, {Hex4String(RegisterBC)}");
					break;
				#endregion
				#region 02 LD (BC), A
				case 0x02:
					_memory.WriteUShort(RegisterBC, RegisterA);
					DumpCPU("LD (BC), A");
					break;
				#endregion
				#region 03 INC BC
				case 0x03:
					RegisterBC++;
					DumpCPU("INC BC");
					break;
				#endregion
				#region 04 INC B
				case 0x04:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterB & 0b00001111) + 1) > 0xf;
					RegisterB++;
					ZeroFlag = RegisterB == 0;
					DumpCPU($"INC B");
					break;
				#endregion
				#region 05 DEC B
				case 0x05:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterB & 0b00001111) == 0;
					RegisterB--;
					ZeroFlag = RegisterB == 0;
					DumpCPU("DEC B");
					break;
				#endregion
				#region 06 LD B, n
				case 0x06:
					RegisterB = ReadByteOperand();
					DumpCPU($"LD B, {Hex2String(RegisterB)}");
					break;
				#endregion
				#region 07 RLCA
				case 0x07:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = RegisterA > 0x7f;
					RegisterA <<= 1;
					if (CarryFlag)
						RegisterA |= 1;
					ZeroFlag = RegisterA == 0;
					DumpCPU("RLCA");
					break;
				#endregion
				#region 08 LD (nn), SP
				case 0x08:
					ushort n08 = ReadUShortOperand();
					_memory.WriteUShort(n08, SP);
					DumpCPU($"LD ({Hex4String(n08)}), SP");
					break;
				#endregion
				#region 09 ADD HL, BC
				case 0x09:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterHL & 0b00001111_11111111) + (RegisterBC & 0b00001111_11111111)) > 0xFFF;
					CarryFlag = (RegisterHL + RegisterBC) > 0xFFF;
					RegisterHL += RegisterBC;
					break;
				#endregion
				#region 0A LD A, (BC)
				case 0x0A:
					RegisterA = _memory.ReadByte(RegisterBC);
					DumpCPU("LD A, (BC)");
					break;
				#endregion
				#region 0B DEC BC
				case 0x0B:
					RegisterBC--;
					DumpCPU("DEC BC");
					break;
				#endregion
				#region 0C INC C
				case 0x0C:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterC & 0b00001111) + 1) > 0xf;
					RegisterC++;
					if (RegisterC == 0)
						ZeroFlag = true;
					DumpCPU($"INC C");
					break;
				#endregion
				#region 0D DEC C
				case 0x0D:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterC & 0b00001111) == 0;
					RegisterC--;
					ZeroFlag = RegisterC == 0;
					DumpCPU("DEC C");
					break;
				#endregion
				#region 0E LD C, n
				case 0x0E:
					RegisterC = ReadByteOperand();
					DumpCPU($"LD C, {Hex2String(RegisterC)}");
					break;
				#endregion
				#region 0F RRCA
				case 0x0F:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = (RegisterA << 7) > 0;
					RegisterA >>= 1;
					if (CarryFlag)
						RegisterA |= 255;
					ZeroFlag = RegisterA == 0;
					DumpCPU("RRCA");
					break;
				#endregion
				#region 10 STOP
				case 0x10:
					if (ReadByteOperand() != 0x00)
						throw new NotImplementedException("STOP not in the form $10 $00");
					throw new NotImplementedException();
					//DumpCPU("STOP");
					break;
				#endregion
				#region 11 LD DE, nn
				case 0x11:
					RegisterDE = ReadUShortOperand();
					DumpCPU($"LD DE, {Hex4String(RegisterDE)}");
					break;
				#endregion
				#region 12 LD (DE), A
				case 0x12:
					_memory.WriteUShort(RegisterDE, RegisterA);
					DumpCPU("LD (DE), A");
					break;
				#endregion
				#region 13 INC DE
				case 0x13:
					RegisterDE++;
					DumpCPU("INC DE");
					break;
				#endregion
				#region 14 INC D
				case 0x14:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterD & 0b00001111) + 1) > 0xf;
					RegisterD++;
					if (RegisterD == 0)
						ZeroFlag = true;
					DumpCPU($"INC D");
					break;
				#endregion
				#region 15 DEC D
				case 0x15:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterD & 0b00001111) == 0;
					RegisterD--;
					ZeroFlag = RegisterD == 0;
					DumpCPU("DEC D");
					break;
				#endregion
				#region 16 LD D, n
				case 0x16:
					RegisterD = ReadByteOperand();
					DumpCPU($"LD D, {Hex2String(RegisterD)}");
					break;
				#endregion
				#region 17 RLA
				case 0x17:
					SubtractFlag = false;
					HalfCarryFlag = false;
					bool b17 = CarryFlag;
					CarryFlag = RegisterA > 0x7F;
					RegisterA <<= 1;
					if (b17)
						RegisterA |= 1;
					ZeroFlag = RegisterA == 0;
					DumpCPU("RLA");
					break;
				#endregion
				#region 18 JR n
				case 0x18:
					sbyte n18 = ReadSbyteOperand();
					DumpCPU($"JR (+{Hex2String((byte)n18)})");
					pci += n18;
					break;
				#endregion
				#region 19 ADD HL, DE
				case 0x19:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterHL & 0b00001111_11111111) + (RegisterDE & 0b00001111_11111111)) > 0xFFF;
					CarryFlag = (RegisterHL + RegisterDE) > 0xFFF;
					RegisterHL += RegisterDE;
					break;
				#endregion
				#region 1A LD A, (DE)
				case 0x1A:
					RegisterA = _memory.ReadByte(RegisterDE);
					DumpCPU("LD A, (DE)");
					break;
				#endregion
				#region 1B DEC DE
				case 0x1B:
					RegisterDE--;
					DumpCPU("DEC DE");
					break;
				#endregion
				#region 1C INC E
				case 0x1C:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterE & 0b00001111) + 1) > 0xf;
					RegisterE++;
					if (RegisterE == 0)
						ZeroFlag = true;
					DumpCPU($"INC E");
					break;
				#endregion
				#region 1D DEC E
				case 0x1D:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterE & 0b00001111) == 0;
					RegisterE--;
					ZeroFlag = RegisterE == 0;
					DumpCPU("DEC E");
					break;
				#endregion
				#region 1E LD E, n
				case 0x1E:
					RegisterE = ReadByteOperand();
					DumpCPU($"LD E, {Hex2String(RegisterE)}");
					break;
				#endregion
				#region 1F RRA
				case 0x1F:
					SubtractFlag = false;
					HalfCarryFlag = false;
					bool b1F = CarryFlag;
					CarryFlag = (RegisterA << 7) > 0;
					RegisterA >>= 1;
					if (b1F)
						RegisterA |= 255;
					ZeroFlag = RegisterA == 0;
					DumpCPU("RRA");
					break;
				#endregion
				#region 20 JR NZ, n
				case 0x20:
					sbyte n20 = ReadSbyteOperand();
					DumpCPU($"JR NZ, (+{Hex2String((byte)n20)})");
					if (!ZeroFlag)
						pci += n20;
					break;
				#endregion
				#region 21 LD HL, nn
				case 0x21:
					RegisterHL = ReadUShortOperand();
					DumpCPU($"LD HL, {Hex4String(RegisterHL)}");
					break;
				#endregion
				#region 22 LD (HL+), A
				case 0x22:
					_memory.WriteUShort(RegisterHL++, RegisterA);
					DumpCPU("LD (HL+), A");
					break;
				#endregion
				#region 23 INC HL
				case 0x23:
					RegisterHL++;
					DumpCPU("INC HL");
					break;
				#endregion
				#region 24 INC H
				case 0x24:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterH & 0b00001111) + 1) > 0xf;
					RegisterH++;
					if (RegisterH == 0)
						ZeroFlag = true;
					DumpCPU($"INC H");
					break;
				#endregion
				#region 25 DEC H
				case 0x25:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterH & 0b00001111) == 0;
					RegisterH--;
					ZeroFlag = RegisterH == 0;
					DumpCPU("DEC H");
					break;
				#endregion
				#region 26 LD H, n
				case 0x26:
					RegisterH = ReadByteOperand();
					DumpCPU($"LD H, {Hex2String(RegisterH)}");
					break;
				#endregion
				#region 28 JR Z, n
				case 0x28:
					sbyte n28 = ReadSbyteOperand();
					DumpCPU($"JR Z, (+{Hex2String((byte)n28)})");
					if (ZeroFlag)
						pci += n28;
					break;
				#endregion
				#region 29 ADD HL, HL
				case 0x29:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterHL & 0b00001111_11111111) + (RegisterHL & 0b00001111_11111111)) > 0xFFF;
					CarryFlag = (RegisterHL + RegisterHL) > 0xFFF;
					RegisterHL += RegisterHL;
					break;
				#endregion
				#region 2B DEC HL
				case 0x2B:
					RegisterHL--;
					DumpCPU("DEC HL");
					break;
				#endregion
				#region 2C INC L
				case 0x2C:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterL & 0b00001111) + 1) > 0xf;
					RegisterL++;
					if (RegisterL == 0)
						ZeroFlag = true;
					DumpCPU($"INC L");
					break;
				#endregion
				#region 2D DEC L
				case 0x2D:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterL & 0b00001111) == 0;
					RegisterL--;
					ZeroFlag = RegisterL == 0;
					DumpCPU("DEC L");
					break;
				#endregion
				#region 2E LD L, n
				case 0x2E:
					RegisterL = ReadByteOperand();
					DumpCPU($"LD L, {Hex2String(RegisterL)}");
					break;
				#endregion
				#region 30 JR NC, n
				case 0x30:
					sbyte n30 = ReadSbyteOperand();
					DumpCPU($"JR NC, (+{Hex2String((byte)n30)})");
					if (!CarryFlag)
						pci += n30;
					break;
				#endregion
				#region 31 LD SP, nn
				case 0x31:
					SP = ReadUShortOperand();
					DumpCPU($"LD SP, {Hex4String(SP)}");
					break;
				#endregion
				#region 32 LD (HL-), A
				case 0x32:
					_memory.WriteUShort(RegisterHL--, RegisterA);
					DumpCPU($"LD (HL-), A");
					break;
				#endregion
				#region 33 INC SP
				case 0x33:
					SP++;
					DumpCPU("INC SP");
					break;
				#endregion
				#region 34 INC (HL)
				case 0x34:
					SubtractFlag = false;
					HalfCarryFlag = ((_memory.ReadByte(RegisterHL) & 0b00001111) + 1) > 0xf;
					_memory.WriteByte(RegisterHL, (byte)(_memory.ReadByte(RegisterHL) + 1));
					if (_memory.ReadByte(RegisterHL) == 0)
						ZeroFlag = true;
					DumpCPU($"INC (HL)");
					break;
				#endregion
				#region 35 DEC (HL)
				case 0x35:
					SubtractFlag = true;
					HalfCarryFlag = (_memory.ReadByte(RegisterHL) & 0b00001111) == 0;
					_memory.WriteByte(RegisterHL, (byte)(_memory.ReadByte(RegisterHL) - 1));
					ZeroFlag = _memory.ReadByte(RegisterHL) == 0;
					DumpCPU("DEC (HL)");
					break;
				#endregion
				#region 38 JR C, n
				case 0x38:
					sbyte n38 = ReadSbyteOperand();
					DumpCPU($"JR C, (+{Hex2String((byte)n38)})");
					if (!CarryFlag)
						pci += n38;
					break;
				#endregion
				#region 39 ADD HL, SP
				case 0x39:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterHL & 0b00001111_11111111) + (SP & 0b00001111_11111111)) > 0xFFF;
					CarryFlag = (RegisterHL + SP) > 0xFFF;
					RegisterHL += SP;
					break;
				#endregion
				#region 3B DEC SP
				case 0x3B:
					RegisterSP--;
					DumpCPU("DEC SP");
					break;
				#endregion
				#region 3C INC A
				case 0x3C:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + 1) > 0xf;
					RegisterA++;
					if (RegisterA == 0)
						ZeroFlag = true;
					DumpCPU($"INC A");
					break;
				#endregion
				#region 3D DEC A
				case 0x3D:
					SubtractFlag = true;
					HalfCarryFlag = (RegisterA & 0b00001111) == 0;
					RegisterA--;
					ZeroFlag = RegisterA == 0;
					DumpCPU("DEC A");
					break;
				#endregion
				#region 3E LD A, #
				case 0x3E:
					RegisterA = ReadByteOperand();
					DumpCPU($"LD A, {Hex2String(RegisterA)}");
					break;
				#endregion
				#region 40 LD B, B
				case 0x40:
					DumpCPU("LD B, B");
					break;
				#endregion
				#region 41 LD B, C
				case 0x41:
					RegisterB = RegisterC;
					DumpCPU("LD B, C");
					break;
				#endregion
				#region 42 LD B, D
				case 0x42:
					RegisterB = RegisterD;
					DumpCPU("LD B, D");
					break;
				#endregion
				#region 43 LD B, E
				case 0x43:
					RegisterB = RegisterE;
					DumpCPU("LD B, E");
					break;
				#endregion
				#region 44 LD B, H
				case 0x44:
					RegisterB = RegisterH;
					DumpCPU("LD B, H");
					break;
				#endregion
				#region 45 LD B, L
				case 0x45:
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
				#region 50 LD D, B
				case 0x50:
					RegisterD = RegisterB;
					DumpCPU("LD D, B");
					break;
				#endregion
				#region 51 LD D, C
				case 0x51:
					RegisterD = RegisterC;
					DumpCPU("LD D, C");
					break;
				#endregion
				#region 52 LD D, D
				case 0x52:
					DumpCPU("LD D, D");
					break;
				#endregion
				#region 53 LD D, E
				case 0x53:
					RegisterD = RegisterE;
					DumpCPU("LD D, E");
					break;
				#endregion
				#region 54 LD D, H
				case 0x54:
					RegisterD = RegisterH;
					DumpCPU("LD D, H");
					break;
				#endregion
				#region 55 LD D, L
				case 0x55:
					RegisterD = RegisterL;
					DumpCPU("LD D, L");
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
					RegisterBC = _memory.ReadUShort(SP);
					DumpCPU("POP BC");
					break;
				#endregion
				#region C5 PUSH BC
				case 0xC5:
					_memory.WriteUShort(SP, RegisterBC);
					SP -= 2;
					DumpCPU("PUSH BC");
					break;
				#endregion
				#region C9 RET
				case 0xC9:  //RET
					DumpCPU("RET");
					SP += 2;
					PC = _memory.ReadUShort(SP);
					break;
				#endregion
				#region CB
				case 0xCB:
					var subOpCode = ReadByteOperand();
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
							break;
						#endregion
						#region 7C BIT 7, H
						case 0x7C:
							ZeroFlag = !RegisterH.CheckBit(7);
							SubtractFlag = false;
							HalfCarryFlag = true;
							DumpCPU("BIT 7, H");
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
					_memory.WriteUShort(SP, (ushort)(PC + 2));
					SP -= 2;
					ushort nnCD = ReadUShortOperand();
					DumpCPU($"CALL {Hex4String(nnCD)}");
					PC = --nnCD;
					break;
				#endregion
				#region E0 LD ($FF00 + n), A
				case 0xE0:  //LDH (n), A
					byte nE0 = ReadByteOperand();
					DumpCPU($"LD ({Hex4String(0xFF00)} + {Hex2String(nE0)}), A");
					_memory.WriteByte((ushort)(0xFF00 + nE0), RegisterA);
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
					ushort nnEA = ReadUShortOperand();
					_memory.WriteByte(nnEA, RegisterA);
					DumpCPU($"LD ({Hex4String(nnEA)}), A");
					PC += 2;
					break;
				#endregion
				#region FE CP #
				case 0xFE:  //CP #
					byte nFE = ReadByteOperand();
					ZeroFlag = RegisterA == 0;
					SubtractFlag = true;
					HalfCarryFlag = (RegisterA & 0b00001111) == 0;
					CarryFlag = RegisterA < nFE;
					DumpCPU($"CP {Hex2String(nFE)}");
					break;
				#endregion
				#region ERROR
				default:
					DumpCPU($"Instruction not programmed: {Hex2String(instruction)}");
					break;
				#endregion
			}
			//TODO: Interrupt request
			PC = (ushort)(PC + pci);
			pci = 0;
		}

		//8 bit helper methods.
		private byte ReadByteOperand()
		{
			byte b = _memory.ReadByte((ushort)(PC + pci));
			pci++;
			return b;
		}
		private sbyte ReadSbyteOperand()
		{
			sbyte s = (sbyte)_memory.ReadByte((ushort)(PC + pci));
			pci++;
			return s;
		}
		//16 bit helper methods.
		private ushort ReadUShortOperand()
		{
			ushort u = _memory.ReadUShort((ushort)(PC + pci));
			pci += 2;
			return u;
		}
		private short ReadShortOperand()
		{
			short s = (short)_memory.ReadUShort((ushort)(PC + pci));
			pci += 2;
			return s;
		}
		//Debug helper for CPU dump.
		private void DumpCPU(string debug)
		{
#if DEBUG
			Console.Write(string.Format("║{0,-35}║", debug));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterA, 2).PadLeft(8, '0'), RegisterA));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterF, 2).PadLeft(8, '0'), RegisterF));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterB, 2).PadLeft(8, '0'), RegisterB));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterC, 2).PadLeft(8, '0'), RegisterC));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterD, 2).PadLeft(8, '0'), RegisterD));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterE, 2).PadLeft(8, '0'), RegisterE));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterH, 2).PadLeft(8, '0'), RegisterH));
			Console.Write(string.Format("{0,8} ({1:X2})║", Convert.ToString(RegisterL, 2).PadLeft(8, '0'), RegisterL));
			Console.Write(string.Format("{0:X4}║", RegisterAF));
			Console.Write(string.Format("{0:X4}║", RegisterBC));
			Console.Write(string.Format("{0:X4}║", RegisterDE));
			Console.Write(string.Format("{0:X4}║", RegisterHL));
			Console.Write(string.Format("{0:X4}║", PC));
			Console.WriteLine(string.Format("{0:X4}║", SP));
#endif
		}
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
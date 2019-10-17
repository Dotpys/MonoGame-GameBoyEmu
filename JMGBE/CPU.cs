using System;
using System.Text;
using System.Threading;

namespace JMGBE.Core
{
	public class CPU
	{

		private FlagRegister registerF;
		#region Properties
		public bool IsRunning { get; set; }

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
		#endregion
		private readonly MemoryBase<ushort> _memory;
		private short pci = 0;

		public CPU(Memory memory)
		{
			PC = 0x0000;
			_memory = memory;
#if DEBUG
			Console.WriteLine(	"╔═══════════════════════════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦═════════════╦════╦════╦════╦════╦════╦════╗" +
								"║              Mnemonic             ║      A      ║      F      ║      B      ║      C      ║      D      ║      E      ║      H      ║      L      ║ AF ║ BC ║ DE ║ HL ║ PC ║ SP ║" +
								"╠═══════════════════════════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬════╬════╬════╬════╬════╬════╣" +
								"║                                   ║             ║FNHC         ║             ║             ║             ║             ║             ║             ║    ║    ║    ║    ║    ║    ║" +
								"╠═══════════════════════════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬═════════════╬════╬════╬════╬════╬════╬════╣");
#endif
		}

		public void NextInstruction()
		{
			if (!IsRunning) return;
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
					//break;
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
				#region 2A LD A, (HL+)
				case 0x2A:
					RegisterA = _memory.ReadByte(RegisterHL++);
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
				#region 36 LD (HL), n
				case 0x36:
					_memory.WriteByte(RegisterHL, ReadByteOperand());
					DumpCPU($"LD (HL), {Hex2String(_memory.ReadByte(RegisterHL))}");
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
				#region 3A LD A, (HL-)
				case 0x3A:
					RegisterA = _memory.ReadByte(RegisterHL--);
					break;
				#endregion
				#region 3B DEC SP
				case 0x3B:
					SP--;
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
				#region 3E LD A, n
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
				case 0x46:
					RegisterB = _memory.ReadByte(RegisterHL);
					DumpCPU("LD B, (HL)");
					break;
				#endregion
				#region 47 LD B, A
				case 0x47:
					RegisterB = RegisterA;
					DumpCPU($"LD B, A");
					break;
				#endregion
				#region 48 LD C, B
				case 0x48:
					RegisterC = RegisterB;
					DumpCPU("LD C, B");
					break;
				#endregion
				#region 49 LD C, C
				case 0x49:
					DumpCPU("LD C, C");
					break;
				#endregion
				#region 4A LD C, D
				case 0x4A:
					RegisterC = RegisterD;
					DumpCPU("LD C, D");
					break;
				#endregion
				#region 4B LD C, E
				case 0x4B:
					RegisterC = RegisterE;
					DumpCPU("LD C, E");
					break;
				#endregion
				#region 4C LD C, H
				case 0x4C:
					RegisterC = RegisterH;
					DumpCPU("LD C, H");
					break;
				#endregion
				#region 4D LD C, L
				case 0x4D:
					RegisterC = RegisterL;
					DumpCPU("LD C, L");
					break;
				#endregion
				#region 4E LD C, (HL)
				case 0x4E:
					RegisterC = _memory.ReadByte(RegisterHL);
					DumpCPU("LD C, (HL)");
					break;
				#endregion
				#region 4F LD C, A
				case 0x4F:
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
				#region 56 LD D, (HL)
				case 0x56:
					RegisterD = _memory.ReadByte(RegisterHL);
					DumpCPU("LD D, (HL)");
					break;
				#endregion
				#region 57 LD D, A
				case 0x57:
					RegisterD = RegisterA;
					DumpCPU($"LD D, A");
					break;
				#endregion
				#region 58 LD E, B
				case 0x58:
					RegisterE = RegisterB;
					DumpCPU("LD E, B");
					break;
				#endregion
				#region 59 LD E, C
				case 0x59:
					RegisterE = RegisterC;
					DumpCPU("LD E, C");
					break;
				#endregion
				#region 5A LD E, D
				case 0x5A:
					RegisterE = RegisterD;
					DumpCPU("LD E, D");
					break;
				#endregion
				#region 5B LD E, E
				case 0x5B:
					DumpCPU("LD E, E");
					break;
				#endregion
				#region 5C LD E, H
				case 0x5C:
					RegisterE = RegisterH;
					DumpCPU("LD E, H");
					break;
				#endregion
				#region 5D LD E, L
				case 0x5D:
					RegisterE = RegisterL;
					DumpCPU("LD E, L");
					break;
				#endregion
				#region 5E LD E, (HL)
				case 0x5E:
					RegisterE = _memory.ReadByte(RegisterHL);
					DumpCPU("LD E, (HL)");
					break;
				#endregion
				#region 5F LD E, A
				case 0x5F:
					RegisterE = RegisterA;
					DumpCPU("LD E, A");
					break;
				#endregion
				#region 60 LD H, B
				case 0x60:
					RegisterH = RegisterB;
					DumpCPU("LD H, B");
					break;
				#endregion
				#region 61 LD H, C
				case 0x61:
					RegisterH = RegisterC;
					DumpCPU("LD H, C");
					break;
				#endregion
				#region 62 LD H, D
				case 0x62:
					RegisterH = RegisterD;
					DumpCPU("LD H, D");
					break;
				#endregion
				#region 63 LD H, E
				case 0x63:
					RegisterH = RegisterE;
					DumpCPU("LD H, E");
					break;
				#endregion
				#region 64 LD H, H
				case 0x64:
					DumpCPU("LD H, H");
					break;
				#endregion
				#region 65 LD H, L
				case 0x65:
					RegisterH = RegisterL;
					DumpCPU("LD H, L");
					break;
				#endregion
				#region 66 LD H, (HL)
				case 0x66:
					RegisterH = _memory.ReadByte(RegisterHL);
					DumpCPU("LD H, (HL)");
					break;
				#endregion
				#region 67 LD H, A
				case 0x67:
					RegisterH = RegisterA;
					DumpCPU($"LD H, A");
					break;
				#endregion
				#region 68 LD L, B
				case 0x68:
					RegisterL = RegisterB;
					DumpCPU("LD L, B");
					break;
				#endregion
				#region 69 LD L, C
				case 0x69:
					RegisterL = RegisterC;
					DumpCPU("LD L, C");
					break;
				#endregion
				#region 6A LD L, D
				case 0x6A:
					RegisterL = RegisterD;
					DumpCPU("LD L, D");
					break;
				#endregion
				#region 6B LD L, E
				case 0x6B:
					RegisterL = RegisterE;
					DumpCPU("LD L, E");
					break;
				#endregion
				#region 6C LD L, H
				case 0x6C:
					RegisterL = RegisterH;
					DumpCPU("LD L, H");
					break;
				#endregion
				#region 6D LD L, L
				case 0x6D:
					DumpCPU("LD L, L");
					break;
				#endregion
				#region 6E LD L, (HL)
				case 0x6E:
					RegisterL = _memory.ReadByte(RegisterHL);
					DumpCPU("LD L, (HL)");
					break;
				#endregion
				#region 6F LD L, A
				case 0x6F:
					RegisterL = RegisterA;
					DumpCPU("LD L, A");
					break;
				#endregion
				#region 70 LD (HL), B
				case 0x70:
					_memory.WriteByte(RegisterHL, RegisterB);
					DumpCPU("LD (HL), B");
					break;
				#endregion
				#region 71 LD (HL), C
				case 0x71:
					_memory.WriteByte(RegisterHL, RegisterC);
					DumpCPU("LD (HL), C");
					break;
				#endregion
				#region 72 LD (HL), D
				case 0x72:
					_memory.WriteByte(RegisterHL, RegisterD);
					DumpCPU("LD (HL), D");
					break;
				#endregion
				#region 73 LD (HL), E
				case 0x73:
					_memory.WriteByte(RegisterHL, RegisterE);
					DumpCPU("LD (HL), E");
					break;
				#endregion
				#region 74 LD (HL), H
				case 0x74:
					_memory.WriteByte(RegisterHL, RegisterH);
					DumpCPU("LD (HL), H");
					break;
				#endregion
				#region 75 LD (HL), L
				case 0x75:
					_memory.WriteByte(RegisterHL, RegisterL);
					DumpCPU("LD (HL), L");
					break;
				#endregion

				#region 77 LD (HL), A
				case 0x77:
					_memory.WriteByte(RegisterHL, RegisterA);
					DumpCPU("LD (HL), A");
					break;
				#endregion
				#region 78 LD A, B
				case 0x78:
					RegisterA = RegisterB;
					DumpCPU("LD A, B");
					break;
				#endregion
				#region 79 LD A, C
				case 0x79:
					RegisterA = RegisterC;
					DumpCPU("LD A, C");
					break;
				#endregion
				#region 7A LD A, D
				case 0x7A:
					RegisterA = RegisterD;
					DumpCPU("LD A, D");
					break;
				#endregion
				#region 7B LD A, E
				case 0x7B:
					RegisterA = RegisterE;
					DumpCPU("LD A, E");
					break;
				#endregion
				#region 7C LD A, H
				case 0x7C:
					RegisterA = RegisterH;
					DumpCPU("LD A, H");
					break;
				#endregion
				#region 7D LD A, L
				case 0x7D:
					RegisterA = RegisterL;
					DumpCPU("LD A, L");
					break;
				#endregion
				#region 7E LD A, (HL)
				case 0x7E:
					RegisterA = _memory.ReadByte(RegisterHL);
					DumpCPU("LD A, (HL)");
					break;
				#endregion
				#region 7F LD A, A
				case 0x7F:
					DumpCPU("LD A, A");
					break;
				#endregion
				#region 80 ADD A, B
				case 0x80:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterB & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterB) > 0xff;
					RegisterA += RegisterB;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, B");
					break;
				#endregion
				#region 81 ADD A, C
				case 0x81:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterC & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterC) > 0xff;
					RegisterA += RegisterC;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, C");
					break;
				#endregion
				#region 82 ADD A, D
				case 0x82:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterD & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterD) > 0xff;
					RegisterA += RegisterD;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, D");
					break;
				#endregion
				#region 83 ADD A, E
				case 0x83:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterE & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterE) > 0xff;
					RegisterA += RegisterE;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, E");
					break;
				#endregion
				#region 84 ADD A, H
				case 0x84:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterH & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterH) > 0xff;
					RegisterA += RegisterH;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, H");
					break;
				#endregion
				#region 85 ADD A, L
				case 0x85:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterL & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterL) > 0xff;
					RegisterA += RegisterL;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, L");
					break;
				#endregion
				#region 86 ADD A, (HL)
				case 0x86:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (_memory.ReadByte(RegisterHL) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + _memory.ReadByte(RegisterHL)) > 0xff;
					RegisterA += _memory.ReadByte(RegisterHL);
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, (HL)");
					break;
				#endregion
				#region 87 ADD A, A
				case 0x87:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + (RegisterA & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + RegisterA) > 0xff;
					RegisterA += RegisterA;
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADD A, A");
					break;
				#endregion
				#region 88 ADC A, B
				case 0x88:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterB + 1 : RegisterB) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterB + 1 : RegisterB)) > 0xff;
					RegisterA += (byte)(RegisterB + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, B");
					break;
				#endregion
				#region 89 ADC A, C
				case 0x89:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterC + 1 : RegisterC) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterC + 1 : RegisterC)) > 0xff;
					RegisterA += (byte)(RegisterC + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, C");
					break;
				#endregion
				#region 8A ADC A, D
				case 0x8A:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterD + 1 : RegisterD) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterD + 1 : RegisterD)) > 0xff;
					RegisterA += (byte)(RegisterD + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, D");
					break;
				#endregion
				#region 8B ADC A, E
				case 0x8B:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterE + 1 : RegisterE) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterE + 1 : RegisterE)) > 0xff;
					RegisterA += (byte)(RegisterE + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, E");
					break;
				#endregion
				#region 8C ADC A, H
				case 0x8C:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterH + 1 : RegisterH) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterH + 1 : RegisterH)) > 0xff;
					RegisterA += (byte)(RegisterH + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, H");
					break;
				#endregion
				#region 8D ADC A, L
				case 0x8D:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterL + 1 : RegisterL) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterL + 1 : RegisterL)) > 0xff;
					RegisterA += (byte)(RegisterL + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, L");
					break;
				#endregion
				#region 8E ADC A, (HL)
				case 0x8E:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? _memory.ReadByte(RegisterHL) + 1 : _memory.ReadByte(RegisterHL)) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? _memory.ReadByte(RegisterHL) + 1 : _memory.ReadByte(RegisterHL))) > 0xff;
					RegisterA += (byte)(_memory.ReadByte(RegisterHL) + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, (HL)");
					break;
				#endregion
				#region 8F ADC A, A
				case 0x8F:
					SubtractFlag = false;
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? RegisterA + 1 : RegisterA) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? RegisterA + 1 : RegisterA)) > 0xff;
					RegisterA += (byte)(RegisterA + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("ADC A, A");
					break;
				#endregion
				#region 90 SUB B
				case 0x90:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterB & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterB) >= 0;
					RegisterA -= RegisterB;
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB B");
					break;
				#endregion
				#region 91 SUB C
				case 0x91:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterC & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterC) >= 0;
					RegisterA -= RegisterC;
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB C");
					break;
				#endregion
				#region 92 SUB D
				case 0x92:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterD & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterD) >= 0;
					RegisterA -= RegisterD;
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB D");
					break;
				#endregion
				#region 93 SUB E
				case 0x93:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterE & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterE) >= 0;
					RegisterA -= RegisterB;
					ZeroFlag = RegisterE == 0;
					DumpCPU("SUB E");
					break;
				#endregion
				#region 94 SUB H
				case 0x94:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterH & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterH) >= 0;
					RegisterA -= RegisterH;
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB H");
					break;
				#endregion
				#region 95 SUB L
				case 0x95:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterL & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterL) >= 0;
					RegisterA -= RegisterL;
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB L");
					break;
				#endregion
				#region 96 SUB (HL)
				case 0x96:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (_memory.ReadByte(RegisterHL) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - _memory.ReadByte(RegisterHL)) >= 0;
					RegisterA -= _memory.ReadByte(RegisterHL);
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB (HL)");
					break;
				#endregion
				#region 97 SUB A
				case 0x97:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterA & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterA) >= 0;
					RegisterA -= RegisterA;
					ZeroFlag = RegisterA == 0;
					DumpCPU("SUB A");
					break;
				#endregion
				#region 98 SBC A, B
				case 0x98:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterB + 1 : RegisterB) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterB + 1 : RegisterB)) >= 0;
					RegisterA -= (byte)(RegisterB + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, B");
					break;
				#endregion
				#region 99 SBC A, C
				case 0x99:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterC + 1 : RegisterC) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterC + 1 : RegisterC)) >= 0;
					RegisterA -= (byte)(RegisterC + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, C");
					break;
				#endregion
				#region 9A SBC A, D
				case 0x9A:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterD + 1 : RegisterD) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterD + 1 : RegisterD)) >= 0;
					RegisterA -= (byte)(RegisterD + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, D");
					break;
				#endregion
				#region 9B SBC A, E
				case 0x9B:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterE + 1 : RegisterE) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterE + 1 : RegisterE)) >= 0;
					RegisterA -= (byte)(RegisterE + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, E");
					break;
				#endregion
				#region 9C SBC A, H
				case 0x9C:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterH + 1 : RegisterH) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterH + 1 : RegisterH)) >= 0;
					RegisterA -= (byte)(RegisterH + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, H");
					break;
				#endregion
				#region 9D SBC A, L
				case 0x9D:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterL + 1 : RegisterL) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterL + 1 : RegisterL)) >= 0;
					RegisterA -= (byte)(RegisterL + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, L");
					break;
				#endregion
				#region 9E SBC A, (HL)
				case 0x9E:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? _memory.ReadByte(RegisterHL) + 1 : _memory.ReadByte(RegisterHL)) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? _memory.ReadByte(RegisterHL) + 1 : _memory.ReadByte(RegisterHL))) >= 0;
					RegisterA -= (byte)(_memory.ReadByte(RegisterHL) + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, (HL)");
					break;
				#endregion
				#region 9F SBC A, A
				case 0x9F:
					SubtractFlag = true;
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? RegisterA + 1 : RegisterA) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? RegisterA + 1 : RegisterA)) >= 0;
					RegisterA -= (byte)(RegisterA + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU("SBC A, A");
					break;
				#endregion
				#region A0 AND B
				case 0xA0:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterB;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND B");
					break;
				#endregion
				#region A1 AND C
				case 0xA1:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterC;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND C");
					break;
				#endregion
				#region A2 AND D
				case 0xA2:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterD;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND D");
					break;
				#endregion
				#region A3 AND E
				case 0xA3:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterE;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND E");
					break;
				#endregion
				#region A4 AND H
				case 0xA4:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterH;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND H");
					break;
				#endregion
				#region A5 AND L
				case 0xA5:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterL;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND L");
					break;
				#endregion
				#region A6 AND (HL)
				case 0xA6:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= _memory.ReadByte(RegisterHL);
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND (HL)");
					break;
				#endregion
				#region A7 AND A
				case 0xA7:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					RegisterA &= RegisterA;
					ZeroFlag = RegisterA == 0;
					DumpCPU("AND A");
					break;
				#endregion
				#region A8 XOR B
				case 0xA8:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterB;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR B");
					break;
				#endregion
				#region A9 XOR C
				case 0xA9:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterC;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR C");
					break;
				#endregion
				#region AA XOR D
				case 0xAA:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterD;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR D");
					break;
				#endregion
				#region AB XOR E
				case 0xAB:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterE;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR E");
					break;
				#endregion
				#region AC XOR H
				case 0xAC:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterH;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR H");
					break;
				#endregion
				#region AD XOR L
				case 0xAD:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterL;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR L");
					break;
				#endregion
				#region AE XOR (HL)
				case 0xAE:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= _memory.ReadByte(RegisterHL);
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR (HL)");
					break;
				#endregion
				#region AF XOR A
				case 0xAF:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA ^= RegisterA;
					ZeroFlag = RegisterA == 0;
					DumpCPU("XOR A");
					break;
				#endregion
				#region B0 OR B
				case 0xB0:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterB;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR B");
					break;
				#endregion
				#region B1 OR C
				case 0xB1:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterC;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR C");
					break;
				#endregion
				#region B2 OR D
				case 0xB2:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterD;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR D");
					break;
				#endregion
				#region B3 OR E
				case 0xB3:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterE;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR E");
					break;
				#endregion
				#region B4 OR H
				case 0xB4:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterH;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR H");
					break;
				#endregion
				#region B5 OR L
				case 0xB5:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterL;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR L");
					break;
				#endregion
				#region B6 OR (HL)
				case 0xB6:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= _memory.ReadByte(RegisterHL);
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR (HL)");
					break;
				#endregion
				#region B7 OR A
				case 0xB7:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					RegisterA |= RegisterA;
					ZeroFlag = RegisterA == 0;
					DumpCPU("OR A");
					break;
				#endregion
				#region B8 CP B
				case 0xB8:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterB;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterB & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterB) >= 0;
					DumpCPU("CP B");
					break;
				#endregion
				#region B9 CP C
				case 0xB9:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterC;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterC & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterC) >= 0;
					DumpCPU("CP C");
					break;
				#endregion
				#region BA CP D
				case 0xBA:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterD;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterD & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterD) >= 0;
					DumpCPU("CP D");
					break;
				#endregion
				#region BB CP E
				case 0xBB:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterE;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterE & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterE) >= 0;
					DumpCPU("CP E");
					break;
				#endregion
				#region BC CP H
				case 0xBC:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterH;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterH & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterH) >= 0;
					DumpCPU("CP H");
					break;
				#endregion
				#region BD CP L
				case 0xBD:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterL;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterL & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterL) >= 0;
					DumpCPU("CP L");
					break;
				#endregion
				#region BE CP (HL)
				case 0xBE:
					SubtractFlag = true;
					ZeroFlag = RegisterA == _memory.ReadByte(RegisterHL);
					HalfCarryFlag = ((RegisterA & 0b00001111) - (_memory.ReadByte(RegisterHL) & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - _memory.ReadByte(RegisterHL)) >= 0;
					DumpCPU("CP (HL)");
					break;
				#endregion
				#region BF CP A
				case 0xBF:
					SubtractFlag = true;
					ZeroFlag = RegisterA == RegisterA;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (RegisterA & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - RegisterA) >= 0;
					DumpCPU("CP A");
					break;
				#endregion
				#region C0 RET NZ
				case 0xC0:
					DumpCPU("RET NZ");
					if (!ZeroFlag) PC = PopStack();
					break;
				#endregion
				#region C1 POP BC
				case 0xC1:
					RegisterBC = PopStack();
					DumpCPU("POP BC");
					break;
				#endregion
				#region C2 JP NZ, nn
				case 0xC2:
					if (!ZeroFlag) PC = ReadUShortOperand();
					DumpCPU($"JP NZ, {Hex4String(PC)}");
					break;
				#endregion
				#region C3 JP nn
				case 0xC3:
					PC = ReadUShortOperand();
					DumpCPU($"JP {Hex4String(PC)}");
					break;
				#endregion
				#region C4 CALL NZ, nn
				case 0xC4:
					if (!ZeroFlag)
					{
						PushStack(PC);
						PC = ReadUShortOperand();
					}
					DumpCPU($"CALL NZ, {Hex4String(PC)}");
					break;
				#endregion
				#region C5 PUSH BC
				case 0xC5:
					PushStack(RegisterBC);
					DumpCPU("PUSH BC");
					break;
				#endregion
				#region C6 ADD A, n
				case 0xC6:
					SubtractFlag = false;
					byte nC6 = ReadByteOperand();
					HalfCarryFlag = ((RegisterA & 0b00001111) + (nC6 & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + nC6) > 0xff;
					RegisterA += nC6;
					ZeroFlag = RegisterA == 0;
					DumpCPU($"ADD A, {Hex2String(nC6)}");
					break;
				#endregion
				#region C7 RST 00H
				case 0xC7:
					PushStack(PC);
					PC = 0x0000;
					pci = 0;
					DumpCPU("RST 00H");
					break;
				#endregion
				#region C8 RET Z
				case 0xC8:
					DumpCPU("RET Z");
					if (ZeroFlag) PC = PopStack();
					break;
				#endregion
				#region C9 RET
				case 0xC9:
					DumpCPU("RET");
					PC = PopStack();
					break;
				#endregion
				#region CA JP Z, nn
				case 0xCA:
					if (ZeroFlag) PC = ReadUShortOperand();
					DumpCPU($"JP NZ, {Hex4String(PC)}");
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
				#region CC CALL Z, nn
				case 0xCC:
					if (ZeroFlag)
					{
						PushStack(PC);
						PC = ReadUShortOperand();
					}
					DumpCPU($"CALL Z, {Hex4String(PC)}");
					break;
				#endregion
				#region CD CALL nn
				case 0xCD:
					PushStack(PC);
					PC = ReadUShortOperand();
					//TODO: Forse servira' fare pci = 0.
					DumpCPU($"CALL {Hex4String(PC)}");
					break;
				#endregion
				#region CE ADC A, n
				case 0xCE:
					SubtractFlag = false;
					byte nCE = ReadByteOperand();
					HalfCarryFlag = ((RegisterA & 0b00001111) + ((CarryFlag ? nCE + 1 : nCE) & 0b00001111)) > 0xf;
					CarryFlag = (RegisterA + (CarryFlag ? nCE + 1 : nCE)) > 0xff;
					RegisterA += (byte)(nCE + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU($"ADC A, {Hex2String(nCE)}");
					break;
				#endregion
				#region CF RST 08H
				case 0xCF:
					PushStack(PC);
					PC = 0x0008;
					pci = 0;
					DumpCPU("RST 08H");
					break;
				#endregion
				#region D0 RET NC
				case 0xD0:
					DumpCPU("RET NC");
					if (!CarryFlag) PC = PopStack();
					break;
				#endregion
				#region D1 POP DE
				case 0xD1:
					RegisterDE = PopStack();
					DumpCPU("POP DE");
					break;
				#endregion
				#region D2 JP NC, nn
				case 0xD2:
					if (!CarryFlag) PC = ReadUShortOperand();
					DumpCPU($"JP NC, {Hex4String(PC)}");
					break;
				#endregion
				//D3
				#region D4 CALL NC, nn
				case 0xD4:
					if (!CarryFlag)
					{
						PushStack(PC);
						PC = ReadUShortOperand();
					}
					DumpCPU($"CALL NC, {Hex4String(PC)}");
					break;
				#endregion
				#region D5 PUSH DE
				case 0xD5:
					PushStack(RegisterDE);
					DumpCPU("PUSH DE");
					break;
				#endregion
				#region D6 SUB n
				case 0xD6:
					SubtractFlag = true;
					byte nD6 = ReadByteOperand();
					HalfCarryFlag = ((RegisterA & 0b00001111) - (nD6 & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - nD6) >= 0;
					RegisterA -= nD6;
					ZeroFlag = RegisterA == 0;
					DumpCPU($"SUB {Hex2String(nD6)}");
					break;
				#endregion
				#region D7 RST 10H
				case 0xD7:
					PushStack(PC);
					PC = 0x0010;
					pci = 0;
					DumpCPU("RST 10H");
					break;
				#endregion
				#region D8 RET C
				case 0xD8:
					DumpCPU("RET C");
					if (CarryFlag) PC = PopStack();
					break;
				#endregion

				#region DA JP C, nn
				case 0xDA:
					if (CarryFlag) PC = ReadUShortOperand();
					DumpCPU($"JP C, {Hex4String(PC)}");
					break;
				#endregion
				//DB
				#region DC CALL C, nn
				case 0xDC:
					if (CarryFlag)
					{
						PushStack(PC);
						PC = ReadUShortOperand();
					}
					DumpCPU($"CALL C, {Hex4String(PC)}");
					break;
				#endregion
				//DD
				#region DE SBC A, n
				case 0xDE:
					SubtractFlag = true;
					byte nDE = ReadByteOperand();
					HalfCarryFlag = ((RegisterA & 0b00001111) - ((CarryFlag ? nDE + 1 : RegisterB) & nDE)) >= 0;
					CarryFlag = (RegisterA - (CarryFlag ? nDE + 1 : nDE)) >= 0;
					RegisterA -= (byte)(nDE + (CarryFlag ? 1 : 0));
					ZeroFlag = RegisterA == 0;
					DumpCPU($"SBC A, {Hex2String(nDE)}");
					break;
				#endregion
				#region DF RST 18H
				case 0xDF:
					PushStack(PC);
					PC = 0x0018;
					pci = 0;
					DumpCPU("RST 18H");
					break;
				#endregion
				#region E0 LD ($FF00 + n), A
				case 0xE0:
					byte nE0 = ReadByteOperand();
					DumpCPU($"LD ({Hex4String(0xFF00)} + {Hex2String(nE0)}), A");
					_memory.WriteByte((ushort)(0xFF00 + nE0), RegisterA);
					break;
				#endregion
				#region E1 POP HL
				case 0xE1:
					RegisterHL = PopStack();
					DumpCPU("POP HL");
					break;
				#endregion
				#region E2 LD ($FF00 + C), A
				case 0xE2:
					_memory.WriteByte((ushort)(0xFF00 + RegisterC), RegisterA);
					DumpCPU($"LD ({Hex4String(0xFF00)} + {Hex2String(RegisterC)}), A");
					break;
				#endregion
				//E3
				//E4
				#region E5 PUSH HL
				case 0xE5:
					PushStack(RegisterHL);
					DumpCPU("PUSH HL");
					break;
				#endregion
				#region E6 AND n
				case 0xE6:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					byte nE6 = ReadByteOperand();
					RegisterA &= nE6;
					ZeroFlag = RegisterA == 0;
					DumpCPU($"AND {Hex2String(nE6)}");
					break;
				#endregion
				#region E7 RST 20H
				case 0xE7:
					PushStack(PC);
					PC = 0x0020;
					pci = 0;
					DumpCPU("RST 20H");
					break;
				#endregion
				#region E8 ADD SP, n
				case 0xE8:
					ZeroFlag = true;
					SubtractFlag = true;
					sbyte nE8 = ReadSbyteOperand();
					SP = (ushort)(SP + nE8);
					HalfCarryFlag = (nE8 & 0b10000000) != 0;
					CarryFlag = (nE8 & 0b10000000) != 0;
					break;
				#endregion
				#region E9 JP (HL)
				case 0xE9:
					PC = RegisterHL;
					DumpCPU($"JP ({Hex4String(RegisterHL)})");
					pci = 0;
					break;
				#endregion
				#region EA LD (nn), A
				case 0xEA:
					ushort nEA = ReadUShortOperand();
					_memory.WriteByte(nEA, RegisterA);
					DumpCPU($"LD ({Hex4String(nEA)}), A");
					break;
				#endregion
				//EB
				//EC
				//ED
				#region EE XOR n
				case 0xEE:
					SubtractFlag = false;
					HalfCarryFlag = true;
					CarryFlag = false;
					byte nEE = ReadByteOperand();
					RegisterA &= nEE;
					ZeroFlag = RegisterA == 0;
					DumpCPU($"XOR {Hex2String(nEE)}");
					break;
				#endregion
				#region EF RST 28H
				case 0xEF:
					PushStack(PC);
					PC = 0x0028;
					pci = 0;
					DumpCPU("RST 28H");
					break;
				#endregion
				#region F0 LD A, ($FF00 + n)
				case 0xF0:
					byte nF0 = ReadByteOperand();
					RegisterA = _memory.ReadByte((ushort)(0xFF00 + nF0));
					DumpCPU($"LD A, ({Hex4String(0xFF00)} + {Hex2String(nF0)})");
					break;
				#endregion
				#region F1 POP AF
				case 0xF1:
					RegisterAF = PopStack();
					DumpCPU("POP AF");
					break;
				#endregion
				#region F2 LD A, ($FF00 + C), A
				case 0xF2:
					RegisterA = _memory.ReadByte((ushort)(0xFF00 + RegisterC));
					DumpCPU($"LD A, ({Hex4String(0xFF00)} + {Hex2String(RegisterC)})");
					break;
				#endregion

				//F4
				#region F5 PUSH AF
				case 0xF5:
					PushStack(RegisterAF);
					DumpCPU("PUSH AF");
					break;
				#endregion
				#region F6 OR n
				case 0xF6:
					SubtractFlag = false;
					HalfCarryFlag = false;
					CarryFlag = false;
					byte nF6 = ReadByteOperand();
					RegisterA |= nF6;
					ZeroFlag = RegisterA == 0;
					DumpCPU($"OR {Hex2String(nF6)}");
					break;
				#endregion
				#region F7 RST 30H
				case 0xF7:
					PushStack(PC);
					PC = 0x0030;
					pci = 0;
					DumpCPU("RST 30H");
					break;
				#endregion
				#region F8 LD HL, SP+n
				case 0xF8:
					ZeroFlag = false;
					SubtractFlag = false;
					sbyte nF8 = ReadSbyteOperand();
					RegisterHL = (ushort)(SP + nF8);
					HalfCarryFlag = (nF8 & 0b10000000) != 1;
					CarryFlag = (nF8 & 0b10000000) != 1;
					DumpCPU($"LD HL, SP + {Hex2String((byte)nF8)}");
					break;
				#endregion
				#region F9 LD SP, HL
				case 0xF9:
					SP = RegisterHL;
					DumpCPU("LD SP, HL");
					break;
				#endregion
				#region FA LD A, (nn)
				case 0xFA:
					RegisterA = _memory.ReadByte(ReadUShortOperand());
					DumpCPU($"LD A, {Hex2String(RegisterA)}");
					break;
				#endregion

				//FC
				//FD
				#region FE CP 8n
				case 0xFE:
					SubtractFlag = true;
					byte nFE = ReadByteOperand();
					ZeroFlag = RegisterA == nFE;
					HalfCarryFlag = ((RegisterA & 0b00001111) - (nFE & 0b00001111)) >= 0;
					CarryFlag = (RegisterA - nFE) >= 0;
					DumpCPU($"CP {Hex2String(nFE)}");
					break;
				#endregion
				#region FF RST 38H
				case 0xFF:
					PushStack(PC);
					PC = 0x0038;
					pci = 0;
					DumpCPU("RST 38H");
					break;
				#endregion
				#region ERROR
				default:
					DumpCPU($"Instruction not programmed: {Hex2String(instruction)}");
					break;
				#endregion
			}
			//Interrupt handling:
			if (instruction == 0x76)
			{
				//Wait for an interrupt.
			}
			PC = (ushort)(PC + pci);
			pci = 0;
		}

		//Stack helper methods.
		public void PushStack(ushort value)
		{
			_memory.WriteUShort(SP, value);
			SP -= 2;
		}
		public ushort PopStack()
		{
			SP += 2;
			return _memory.ReadUShort(SP);
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
	
		[Flags]
		private enum InterruptFlag
		{
			JoypadIR = 0x10,
			SerialIR = 0x08,
			TimerIR = 0x04,
			LCDIR = 0x02,
			VBlank = 0x01
		}
	}
}
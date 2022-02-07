using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace JMGBE.Core;

public struct Instruction
{
	public float Cycles { get; set; }
	public Action Execute { get; set; }
	public Action Fetch { get; set; }
	public string Mnemonic { get; set; }

	public Instruction(float cycles, Action execFun, Action fetchFun, string mnemonic)
	{
		Cycles = cycles;
		Execute = execFun;
		Fetch = fetchFun;
		Mnemonic = mnemonic;
	}
}

public class CPU
{
	private StreamWriter log = new StreamWriter(File.OpenWrite(Environment.CurrentDirectory + "\\logs\\lastlog.txt"));

	private byte A = 0x00;
	private byte F = 0x00;
	private byte B = 0x00;
	private byte C = 0x00;
	private byte D = 0x00;
	private byte E = 0x00;
	private byte H = 0x00;
	private byte L = 0x00;

	/// <summary>Program Counter register.</summary>
	private ushort PC = 0x0000;

	/// <summary>Stack Pointer register.</summary>
	private ushort SP = 0x0000;

	private ushort BC
	{
		get { return (ushort)(B << 8 | C); }
		set
		{
			B = (byte)(value >> 8);
			C = (byte)value;
		}
	}
	private ushort DE
	{
		get { return (ushort)(D << 8 | E); }
		set
		{
			D = (byte)(value >> 8);
			E = (byte)value;
		}
	}
	private ushort HL
	{
		get { return (ushort)(H << 8 | L); }
		set
		{
			H = (byte)(value >> 8);
			L = (byte)value;
		}
	}

	public bool ZeroFlag
	{
		get { return F.CheckBit(7); }
		set { F.SetBit(7, value); }
	}
	public bool SubtractFlag
	{
		get { return F.CheckBit(6); }
		set { F.SetBit(6, value); }
	}
	public bool HalfCarryFlag
	{
		get { return F.CheckBit(5); }
		set { F.SetBit(5, value); }
	}
	public bool CarryFlag
	{
		get { return F.CheckBit(4); }
		set { F.SetBit(4, value); }
	}

	Dictionary<byte, Instruction> opcodes;
	Dictionary<byte, Instruction> cb_opcodes;

	readonly MMU _mmu;

	sbyte immediate8s;
	byte immediate8u;
	ushort immediate16;
	byte pci = 0;

	private bool willEnableIme = false;
	private bool ime = true;

	public CPU(MMU mmu)
	{
		_mmu = mmu;

		opcodes = new Dictionary<byte, Instruction>()
		{
			{0x00, new Instruction( 4, Execute0x00, FetchNone, "NOP")},
			{0x01, new Instruction(12, Execute0x01, FetchIm16, "LD BC, 0x{0:X4}")},
			{0x02, new Instruction( 8, Execute0x02, FetchNone, "LD (BC), A")},
			{0x03, new Instruction( 8, Execute0x03, FetchNone, "INC BC")},
			{0x04, new Instruction( 4, Execute0x04, FetchNone, "INC B")},
			{0x05, new Instruction( 4, Execute0x05, FetchNone, "DEC B")},
			{0x06, new Instruction( 8, Execute0x06, FetchIm8U, "LD B, 0x{2:X2}")},
			{0x07, new Instruction( 4, Execute0x07, FetchNone, "RLCA")},
			{0x08, new Instruction(20, Execute0x08, FetchIm16, "LD (0x{0:X4}), SP")},
			{0x09, new Instruction( 8, Execute0x09, FetchNone, "ADD HL, BC")},
			{0x0A, new Instruction( 8, Execute0x0A, FetchNone, "LD A, (BC)")},
			{0x0B, new Instruction( 8, Execute0x0B, FetchNone, "DEC BC")},
			{0x0C, new Instruction( 4, Execute0x0C, FetchNone, "INC C")},
			{0x0D, new Instruction( 4, Execute0x0D, FetchNone, "DEC C")},
			{0x0E, new Instruction( 8, Execute0x0E, FetchIm8U, "LD C, 0x{2:X2}")},
			{0x0F, new Instruction( 4, Execute0x0F, FetchNone, "RRCA")},
			{0x10, new Instruction( 4, Execute0x10, FetchIm8U, "STOP")},
			{0x11, new Instruction(12, Execute0x11, FetchIm16, "LD DE, 0x{0:X4}")},
			{0x12, new Instruction( 8, Execute0x12, FetchNone, "LD (DE), A")},
			{0x13, new Instruction( 8, Execute0x13, FetchNone, "INC DE")},
			{0x14, new Instruction( 4, Execute0x14, FetchNone, "INC D")},
			{0x15, new Instruction( 4, Execute0x15, FetchNone, "DEC D")},
			{0x16, new Instruction( 8, Execute0x16, FetchIm8U, "LD D, 0x{2:X2}")},
			{0x17, new Instruction( 4, Execute0x17, FetchNone, "RLA")},
			{0x18, new Instruction(12, Execute0x18, FetchIm8S, "JR 0x{1:X2}")},
			{0x19, new Instruction( 8, Execute0x19, FetchNone, "ADD HL, DE")},
			{0x1A, new Instruction( 8, Execute0x1A, FetchNone, "LD A, (DE)")},
			{0x1B, new Instruction( 8, Execute0x1B, FetchNone, "DEC DE")},
			{0x1C, new Instruction( 4, Execute0x1C, FetchNone, "INC E")},
			{0x1D, new Instruction( 4, Execute0x1D, FetchNone, "DEC E")},
			{0x1E, new Instruction( 8, Execute0x1E, FetchIm8U, "LD E, 0x{2:X2}")},
			{0x1F, new Instruction( 4, Execute0x1F, FetchNone, "RRA")},
			{0x20, new Instruction(12, Execute0x20, FetchIm8S, "JR NZ, 0x{1:X2}")},
			{0x21, new Instruction(12, Execute0x21, FetchIm16, "LD HL, 0x{0:X4}")},
			{0x22, new Instruction( 8, Execute0x22, FetchNone, "LD (HL+), A")},
			{0x23, new Instruction( 8, Execute0x23, FetchNone, "INC HL")},
			{0x24, new Instruction( 4, Execute0x24, FetchNone, "INC H")},
			{0x25, new Instruction( 4, Execute0x25, FetchNone, "DEC H")},
			{0x26, new Instruction( 8, Execute0x26, FetchIm8U, "LD H, 0x{2:X2}")},
			{0x27, new Instruction( 4, Execute0x27, FetchNone, "DAA")},
			{0x28, new Instruction(12, Execute0x28, FetchIm8S, "JR Z, 0x{1:X2}")},
			{0x29, new Instruction( 8, Execute0x29, FetchNone, "ADD HL, HL")},
			{0x2A, new Instruction( 8, Execute0x2A, FetchNone, "LD A, (HL+)")},
			{0x2B, new Instruction( 8, Execute0x2B, FetchNone, "DEC HL")},
			{0x2C, new Instruction( 4, Execute0x2C, FetchNone, "INC L")},
			{0x2D, new Instruction( 4, Execute0x2D, FetchNone, "DEC L")},
			{0x2E, new Instruction( 8, Execute0x2E, FetchIm8U, "LD L, 0x{2:X2}")},
			{0x2F, new Instruction( 4, Execute0x2F, FetchNone, "CPL")},
			{0x30, new Instruction(12, Execute0x30, FetchIm8S, "JR NC, 0x{1:X2}")},
			{0x31, new Instruction(12, Execute0x31, FetchIm16, "LD SP, 0x{0:X4}")},
			{0x32, new Instruction( 8, Execute0x32, FetchNone, "LD (HL-), A")},
			{0x33, new Instruction( 8, Execute0x33, FetchNone, "INC SP")},
			{0x34, new Instruction(12, Execute0x34, FetchNone, "INC (HL)")},
			{0x35, new Instruction(12, Execute0x35, FetchNone, "DEC (HL)")},
			{0x36, new Instruction(12, Execute0x36, FetchIm8U, "LD (HL), 0x{2:X2}")},
			{0x37, new Instruction( 4, Execute0x37, FetchNone, "SCF")},
			{0x38, new Instruction(12, Execute0x38, FetchIm8S, "JR C, 0x{1:X2}")},
			{0x39, new Instruction( 8, Execute0x39, FetchNone, "ADD HL, SP")},
			{0x3A, new Instruction( 8, Execute0x3A, FetchNone, "LD A, (HL-)")},
			{0x3B, new Instruction( 8, Execute0x3B, FetchNone, "DEC SP")},
			{0x3C, new Instruction( 4, Execute0x3C, FetchNone, "INC A")},
			{0x3D, new Instruction( 4, Execute0x3D, FetchNone, "DEC A")},
			{0x3E, new Instruction( 8, Execute0x3E, FetchIm8U, "LD A, 0x{2:X2}")},
			{0x3F, new Instruction( 4, Execute0x3F, FetchNone, "CCF")},
			{0x40, new Instruction( 4, Execute0x40, FetchNone, "LD B, B")},
			{0x41, new Instruction( 4, Execute0x41, FetchNone, "LD B, C")},
			{0x42, new Instruction( 4, Execute0x42, FetchNone, "LD B, D")},
			{0x43, new Instruction( 4, Execute0x43, FetchNone, "LD B, E")},
			{0x44, new Instruction( 4, Execute0x44, FetchNone, "LD B, H")},
			{0x45, new Instruction( 4, Execute0x45, FetchNone, "LD B, L")},
			{0x46, new Instruction( 4, Execute0x46, FetchNone, "LD B, (HL)")},
			{0x47, new Instruction( 4, Execute0x47, FetchNone, "LD B, A")},
			{0x48, new Instruction( 4, Execute0x48, FetchNone, "LD C, B")},
			{0x49, new Instruction( 4, Execute0x49, FetchNone, "LD C, C")},
			{0x4A, new Instruction( 4, Execute0x4A, FetchNone, "LD C, D")},
			{0x4B, new Instruction( 4, Execute0x4B, FetchNone, "LD C, E")},
			{0x4C, new Instruction( 4, Execute0x4C, FetchNone, "LD C, H")},
			{0x4D, new Instruction( 4, Execute0x4D, FetchNone, "LD C, L")},
			{0x4E, new Instruction( 4, Execute0x4E, FetchNone, "LD C, (HL)")},
			{0x4F, new Instruction( 4, Execute0x4F, FetchNone, "LD C, A")},
			{0x50, new Instruction( 4, Execute0x50, FetchNone, "LD D, B")},
			{0x51, new Instruction( 4, Execute0x51, FetchNone, "LD D, C")},
			{0x52, new Instruction( 4, Execute0x52, FetchNone, "LD D, D")},
			{0x53, new Instruction( 4, Execute0x53, FetchNone, "LD D, E")},
			{0x54, new Instruction( 4, Execute0x54, FetchNone, "LD D, H")},
			{0x55, new Instruction( 4, Execute0x55, FetchNone, "LD D, L")},
			{0x56, new Instruction( 8, Execute0x56, FetchNone, "LD D, (HL)")},
			{0x57, new Instruction( 4, Execute0x57, FetchNone, "LD D, A")},
			{0x58, new Instruction( 4, Execute0x58, FetchNone, "LD E, B")},
			{0x59, new Instruction( 4, Execute0x59, FetchNone, "LD E, C")},
			{0x5A, new Instruction( 4, Execute0x5A, FetchNone, "LD E, D")},
			{0x5B, new Instruction( 4, Execute0x5B, FetchNone, "LD E, E")},
			{0x5C, new Instruction( 4, Execute0x5C, FetchNone, "LD E, H")},
			{0x5D, new Instruction( 4, Execute0x5D, FetchNone, "LD E, L")},
			{0x5E, new Instruction( 4, Execute0x5E, FetchNone, "LD E, (HL)")},
			{0x5F, new Instruction( 4, Execute0x5F, FetchNone, "LD E, A")},
			{0x60, new Instruction( 4, Execute0x60, FetchNone, "LD H, B")},
			{0x61, new Instruction( 4, Execute0x61, FetchNone, "LD H, C")},
			{0x62, new Instruction( 4, Execute0x62, FetchNone, "LD H, D")},
			{0x63, new Instruction( 4, Execute0x63, FetchNone, "LD H, E")},
			{0x64, new Instruction( 4, Execute0x64, FetchNone, "LD H, H")},
			{0x65, new Instruction( 4, Execute0x65, FetchNone, "LD H, L")},
			{0x66, new Instruction( 8, Execute0x66, FetchNone, "LD H, (HL)")},
			{0x67, new Instruction( 4, Execute0x67, FetchNone, "LD H, A")},
			{0x68, new Instruction( 4, Execute0x68, FetchNone, "LD L, B")},
			{0x69, new Instruction( 4, Execute0x69, FetchNone, "LD L, C")},
			{0x6A, new Instruction( 4, Execute0x6A, FetchNone, "LD L, D")},
			{0x6B, new Instruction( 4, Execute0x6B, FetchNone, "LD L, E")},
			{0x6C, new Instruction( 4, Execute0x6C, FetchNone, "LD L, H")},
			{0x6D, new Instruction( 4, Execute0x6D, FetchNone, "LD L, L")},
			{0x6E, new Instruction( 8, Execute0x6E, FetchNone, "LD L, (HL)")},
			{0x6F, new Instruction( 4, Execute0x6F, FetchNone, "LD L, A")},
			{0x70, new Instruction( 8, Execute0x70, FetchNone, "LD (HL), B")},
			{0x71, new Instruction( 8, Execute0x71, FetchNone, "LD (HL), C")},
			{0x72, new Instruction( 8, Execute0x72, FetchNone, "LD (HL), D")},
			{0x73, new Instruction( 8, Execute0x73, FetchNone, "LD (HL), E")},
			{0x74, new Instruction( 8, Execute0x74, FetchNone, "LD (HL), H")},
			{0x75, new Instruction( 8, Execute0x75, FetchNone, "LD (HL), L")},
			{0x76, new Instruction( 4, Execute0x76, FetchNone, "HALT")},
			{0x77, new Instruction( 8, Execute0x77, FetchNone, "LD (HL), A")},
			{0x78, new Instruction( 4, Execute0x78, FetchNone, "LD A, B")},
			{0x79, new Instruction( 4, Execute0x79, FetchNone, "LD A, C")},
			{0x7A, new Instruction( 4, Execute0x7A, FetchNone, "LD A, D")},
			{0x7B, new Instruction( 4, Execute0x7B, FetchNone, "LD A, E")},
			{0x7C, new Instruction( 4, Execute0x7C, FetchNone, "LD A, H")},
			{0x7D, new Instruction( 4, Execute0x7D, FetchNone, "LD A, L")},
			{0x7E, new Instruction( 8, Execute0x7E, FetchNone, "LD A, (HL)")},
			{0x7F, new Instruction( 4, Execute0x7F, FetchNone, "LD A, A")},
			{0x80, new Instruction( 4, Execute0x80, FetchNone, "ADD B")},
			{0x81, new Instruction( 4, Execute0x81, FetchNone, "ADD C")},
			{0x82, new Instruction( 4, Execute0x82, FetchNone, "ADD D")},
			{0x83, new Instruction( 4, Execute0x83, FetchNone, "ADD E")},
			{0x84, new Instruction( 4, Execute0x84, FetchNone, "ADD H")},
			{0x85, new Instruction( 4, Execute0x85, FetchNone, "ADD L")},
			{0x86, new Instruction( 8, Execute0x86, FetchNone, "ADD (HL)")},
			{0x87, new Instruction( 4, Execute0x87, FetchNone, "ADD A")},
			{0x88, new Instruction( 4, Execute0x88, FetchNone, "ADC B")},
			{0x89, new Instruction( 4, Execute0x89, FetchNone, "ADC C")},
			{0x8A, new Instruction( 4, Execute0x8A, FetchNone, "ADC D")},
			{0x8B, new Instruction( 4, Execute0x8B, FetchNone, "ADC E")},
			{0x8C, new Instruction( 4, Execute0x8C, FetchNone, "ADC H")},
			{0x8D, new Instruction( 4, Execute0x8D, FetchNone, "ADC L")},
			{0x8E, new Instruction( 8, Execute0x8E, FetchNone, "ADC (HL)")},
			{0x8F, new Instruction( 4, Execute0x8F, FetchNone, "ADC A")},
			{0x90, new Instruction( 4, Execute0x90, FetchNone, "SUB B")},
			{0x91, new Instruction( 4, Execute0x91, FetchNone, "SUB C")},
			{0x92, new Instruction( 4, Execute0x92, FetchNone, "SUB D")},
			{0x93, new Instruction( 4, Execute0x93, FetchNone, "SUB E")},
			{0x94, new Instruction( 4, Execute0x94, FetchNone, "SUB H")},
			{0x95, new Instruction( 4, Execute0x95, FetchNone, "SUB L")},
			{0x96, new Instruction( 8, Execute0x96, FetchNone, "SUB (HL)")},
			{0x97, new Instruction( 4, Execute0x97, FetchNone, "SUB A")},
			{0x98, new Instruction( 4, Execute0x98, FetchNone, "SBC B")},
			{0x99, new Instruction( 4, Execute0x99, FetchNone, "SBC C")},
			{0x9A, new Instruction( 4, Execute0x9A, FetchNone, "SBC D")},
			{0x9B, new Instruction( 4, Execute0x9B, FetchNone, "SBC E")},
			{0x9C, new Instruction( 4, Execute0x9C, FetchNone, "SBC H")},
			{0x9D, new Instruction( 4, Execute0x9D, FetchNone, "SBC L")},
			{0x9E, new Instruction( 8, Execute0x9E, FetchNone, "SBC (HL)")},
			{0x9F, new Instruction( 4, Execute0x9F, FetchNone, "SBC A")},
			{0xA0, new Instruction( 4, Execute0xA0, FetchNone, "AND B")},
			{0xA1, new Instruction( 4, Execute0xA1, FetchNone, "AND C")},
			{0xA2, new Instruction( 4, Execute0xA2, FetchNone, "AND D")},
			{0xA3, new Instruction( 4, Execute0xA3, FetchNone, "AND E")},
			{0xA4, new Instruction( 4, Execute0xA4, FetchNone, "AND H")},
			{0xA5, new Instruction( 4, Execute0xA5, FetchNone, "AND L")},
			{0xA6, new Instruction( 8, Execute0xA6, FetchNone, "AND (HL)")},
			{0xA7, new Instruction( 4, Execute0xA7, FetchNone, "AND A")},
			{0xA8, new Instruction( 4, Execute0xA8, FetchNone, "XOR B")},
			{0xA9, new Instruction( 4, Execute0xA9, FetchNone, "XOR C")},
			{0xAA, new Instruction( 4, Execute0xAA, FetchNone, "XOR D")},
			{0xAB, new Instruction( 4, Execute0xAB, FetchNone, "XOR E")},
			{0xAC, new Instruction( 4, Execute0xAC, FetchNone, "XOR H")},
			{0xAD, new Instruction( 4, Execute0xAD, FetchNone, "XOR L")},
			{0xAE, new Instruction( 4, Execute0xAE, FetchNone, "XOR (HL)")},
			{0xAF, new Instruction( 4, Execute0xAF, FetchNone, "XOR A")},
			{0xB0, new Instruction( 4, Execute0xB0, FetchNone, "OR B")},
			{0xB1, new Instruction( 4, Execute0xB1, FetchNone, "OR C")},
			{0xB2, new Instruction( 4, Execute0xB2, FetchNone, "OR D")},
			{0xB3, new Instruction( 4, Execute0xB3, FetchNone, "OR E")},
			{0xB4, new Instruction( 4, Execute0xB4, FetchNone, "OR H")},
			{0xB5, new Instruction( 4, Execute0xB5, FetchNone, "OR L")},
			{0xB6, new Instruction( 8, Execute0xB6, FetchNone, "OR (HL)")},
			{0xB7, new Instruction( 4, Execute0xB7, FetchNone, "OR A")},
			{0xB8, new Instruction( 4, Execute0xB8, FetchNone, "CP B")},
			{0xB9, new Instruction( 4, Execute0xB9, FetchNone, "CP C")},
			{0xBA, new Instruction( 4, Execute0xBA, FetchNone, "CP D")},
			{0xBB, new Instruction( 4, Execute0xBB, FetchNone, "CP E")},
			{0xBC, new Instruction( 4, Execute0xBC, FetchNone, "CP H")},
			{0xBD, new Instruction( 4, Execute0xBD, FetchNone, "CP L")},
			{0xBE, new Instruction( 8, Execute0xBE, FetchNone, "CP (HL)")},
			{0xBF, new Instruction( 4, Execute0xBF, FetchNone, "CP A")},

			{0xC1, new Instruction(12, Execute0xC1, FetchNone, "POP BC")},

			{0xC3, new Instruction(16, Execute0xC3, FetchIm16, "JP 0x{0:X4}")},

			{0xC5, new Instruction(16, Execute0xC5, FetchNone, "PUSH BC")},



			{0xC9, new Instruction(16, Execute0xC9, FetchNone, "RET")},

			{0xCB, new Instruction( 4, Execute0xCB, FetchNone, "")},

			{0xCD, new Instruction(24, Execute0xCD, FetchIm16, "CALL 0x{0:X4}")},








			{0xD6, new Instruction( 8, Execute0xD6, FetchIm8U, "SUB 0x{2:X2}")},









			{0xE0, new Instruction(12, Execute0xE0, FetchIm8U, "LD (0xFF00 + 0x{2:X2}), A")},

			{0xE2, new Instruction( 8, Execute0xE2, FetchNone, "LD (0xFF00 + C), A")},



			{0xE6, new Instruction( 8, Execute0xE6, FetchIm8U, "AND 0x{2:X2}")},



			{0xEA, new Instruction(16, Execute0xEA, FetchIm16, "LD (0x{0:X4}), A")},





			{0xF0, new Instruction(12, Execute0xF0, FetchIm8U, "LD A, (0xFF00 + 0x{2:X2})")},


			{0xF3, new Instruction( 4, Execute0xF3, FetchNone, "DI")},







			{0xFB, new Instruction( 4, Execute0xFB, FetchNone, "EI")},

			
			{0xFE, new Instruction( 8, Execute0xFE, FetchIm8U, "CP 0x{2:X2}")},

		};

		cb_opcodes = new Dictionary<byte, Instruction>()
		{
			{0x11, new Instruction( 8, Execute0xCB11, FetchNone, "RL C")},
			{0x17, new Instruction( 8, Execute0xCB17, FetchNone, "RL A")},
			{0x3F, new Instruction( 8, Execute0xCB3F, FetchNone, "BIT 4, E")},
			{0x7C, new Instruction( 8, Execute0xCB7C, FetchNone, "BIT 7, H")}
		};
	}

	public void Clock()
	{
		byte instruction = _mmu.ReadByte(PC);
		pci++;
		opcodes[instruction].Fetch();
		opcodes[instruction].Execute();
		//Effettuare il debug su un file di log per migliorare le performance.
		log.WriteLine($"{PC:X4} : " + string.Format(opcodes[instruction].Mnemonic, immediate16, immediate8s, immediate8u));
		PC += pci;
		pci = 0;

		//Gestione clock
		//TODO
		//Fine gestione clock

		//Gestione interrupt
		//IME enable/disable
		if (instruction != 0xFB && willEnableIme)
		{	//Delays by one instruction the IME set.
			ime = true;
			willEnableIme = false;
		}
		//Interrupt check:
		byte if_reg = _mmu[0xFF0F];//Registers.IF;
		byte ie_reg = _mmu[0xFFFF];//Registers.IE;
		for (byte i = 0; i < 5; i++)
		{
			//Se è possibile chiamare gli interrupt (ime = true)
			//Se l'i esimo interrupt è attivo (ie[pos] = 1) 
			//Se è richiesto l'i esimo interrupt (if[pos] = 1)
			if (ime && ie_reg.CheckBit(i) && if_reg.CheckBit(i))
			{
				//Resetta la richiesta di interrupt
				if_reg.SetBit(i, false);
				_mmu[0xFF0F] = if_reg;
				//Richiama la routine dell'interrupt.
				PUSH(PC);
				PC = (ushort)(0x0040 + 0x0008 * i);
				break;
			}
		}
		//Fine gestione interrupt
	}

#region Fetch
	void FetchNone()
	{
	}
	void FetchIm8S()
	{
		immediate8s = _mmu.ReadSByte(PC + 1);
		pci++;
	}
	void FetchIm8U()
	{
		immediate8u = _mmu.ReadByte(PC + 1);
		pci++;
	}
	void FetchIm16()
	{
		immediate16 = (ushort)(_mmu.ReadByte(PC + 1) | _mmu.ReadByte(PC + 2) << 8);
		pci += 2;
	}
#endregion
#region Execute0xXX
	void Execute0x00()
	{

	}
	void Execute0x01()
	{
		BC = immediate16;
	}
	void Execute0x02()
	{
		_mmu.WriteByte(BC, A);
	}
	void Execute0x03()
	{
		BC++;
	}
	void Execute0x04()
	{
		INC(ref B);
	}
	void Execute0x05()
	{
		DEC(ref B);
	}
	void Execute0x06()
	{
		B = immediate8u;
	}
	void Execute0x07()
	{
		CarryFlag = A.CheckBit(7);
		SubtractFlag = false;
		HalfCarryFlag = false;
		A <<= 1;
		ZeroFlag = A == 0;
	}
	void Execute0x08()
	{
		_mmu.WriteByte(immediate16, (byte)SP);
		_mmu.WriteByte(immediate16+1, (byte)(SP >> 8));
	}
	void Execute0x09()
	{
		SubtractFlag = false;
		HalfCarryFlag = ((HL & 0xfff) + (BC & 0xfff)) > 0xfff;
		CarryFlag = ((HL & 0xffff) + (BC & 0xffff)) > 0xffff;
		HL += BC;
	}
	void Execute0x0A()
	{
		A = _mmu.ReadByte(BC);
	}
	void Execute0x0B()
	{
		BC--;
	}
	void Execute0x0C()
	{
		INC(ref C);
	}
	void Execute0x0D()
	{
		DEC(ref C);
	}
	void Execute0x0E()
	{
		C = immediate8u;
	}
	void Execute0x0F()
	{
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = A.CheckBit(0);
		A >>= 1;
		ZeroFlag = A == 0;
	}
	void Execute0x10()
	{
		//TODO
	}
	void Execute0x11()
	{
		DE = immediate16;
	}
	void Execute0x12()
	{
		_mmu.WriteByte(DE, A);
	}
	void Execute0x13()
	{
		DE++;
	}
	void Execute0x14()
	{
		INC(ref D);
	}
	void Execute0x15()
	{
		DEC(ref D);
	}
	void Execute0x16()
	{
		D = immediate8u;
	}
	void Execute0x17()
	{
		SubtractFlag = false;
		HalfCarryFlag = false;
		byte mask = CarryFlag ? (byte)1 : (byte)0;
		CarryFlag = (A & 0x80) == 0x80;
		A <<= 1;
		A |= mask;
		ZeroFlag = A == 0;
	}
	void Execute0x18()
	{
		PC = (ushort)(PC + immediate8s);
	}
	void Execute0x19()
	{
		SubtractFlag = false;
		HalfCarryFlag = ((HL & 0xfff) + (DE & 0xfff)) > 0xfff;
		CarryFlag = ((HL & 0xffff) + (DE & 0xffff)) > 0xffff;
		HL += DE;
	}
	void Execute0x1A()
	{
		A = _mmu.ReadByte(DE);
	}
	void Execute0x1B()
	{
		DE--;
	}
	void Execute0x1C()
	{
		INC(ref E);
	}
	void Execute0x1D()
	{
		DEC(ref E);
	}
	void Execute0x1E()
	{
		E = immediate8u;
	}
	void Execute0x1F()
	{
		SubtractFlag = false;
		HalfCarryFlag = false;
		byte mask = CarryFlag ? (byte)0x80 : (byte)0;
		CarryFlag = A.CheckBit(0);
		A >>= 1;
		A |= mask;
		ZeroFlag = A == 0;
	}
	void Execute0x20()
	{
		//TODO: Rivedere il funzionamento di clock cycles (12/8)
		//If the zero flag is reset...
		if (!ZeroFlag)
			//...the PC will be set relative to the instruction NEXT to JR.
			PC = (ushort)(PC + immediate8s);
	}
	void Execute0x21()
	{
		HL = immediate16;
	}
	void Execute0x22()
	{
		_mmu.WriteByte(HL++, A);
	}
	void Execute0x23()
	{
		HL++;
	}
	void Execute0x24()
	{
		INC(ref H);
	}
	void Execute0x25()
	{
		DEC(ref H);
	}
	void Execute0x26()
	{
		H = immediate8u;
	}
	void Execute0x27()
	{
		//TODO
	}
	void Execute0x28()
	{
		//TODO: Rivedere il funzionamento di clock cycles (12/8)
		//If the zero flag is set...
		if (ZeroFlag)
			//...the PC will be set relative to the instruction NEXT to JR.
			PC = (ushort)(PC + immediate8s);
	}
	void Execute0x29()
	{
		SubtractFlag = false;
		HalfCarryFlag = ((HL & 0xfff) + (HL & 0xfff)) > 0xfff;
		CarryFlag = ((HL & 0xffff) + (HL & 0xffff)) > 0xffff;
		HL += HL;
	}
	void Execute0x2A()
	{
		A = _mmu.ReadByte(HL++);
	}
	void Execute0x2B()
	{
		HL--;
	}
	void Execute0x2C()
	{
		INC(ref L);
	}
	void Execute0x2D()
	{
		DEC(ref L);
	}
	void Execute0x2E()
	{
		L = immediate8u;
	}
	void Execute0x2F()
	{
		SubtractFlag = true;
		HalfCarryFlag = true;
		A = (byte)~A;
	}
	void Execute0x30()
	{
		//TODO: Rivedere il funzionamento di clock cycles (12/8)
		//If the zero flag is set...
		if (!CarryFlag)
			//...the PC will be set relative to the instruction NEXT to JR.
			PC = (ushort)(PC + immediate8s);
	}
	void Execute0x31()
	{
		SP = immediate16;
	}
	void Execute0x32()
	{
		_mmu.WriteByte(HL--, A);
	}
	void Execute0x33()
	{
		SP++;
	}
	void Execute0x34()
	{
		byte temp = _mmu.ReadByte(HL);
		INC(ref temp);
		_mmu.WriteByte(HL, temp);
	}
	void Execute0x35()
	{
		byte temp = _mmu.ReadByte(HL);
		DEC(ref temp);
		_mmu.WriteByte(HL, temp);
	}
	void Execute0x36()
	{
		_mmu.WriteByte(HL, immediate8u);
	}
	void Execute0x37()
	{
		//TODO
	}
	void Execute0x38()
	{
		//TODO: Rivedere il funzionamento di clock cycles (12/8)
		//If the zero flag is set...
		if (CarryFlag)
			//...the PC will be set relative to the instruction NEXT to JR.
			PC = (ushort)(PC + immediate8s);
	}
	void Execute0x39()
	{
		SubtractFlag = false;
		HalfCarryFlag = ((HL & 0xfff) + (SP & 0xfff)) > 0xfff;
		CarryFlag = ((HL & 0xffff) + (SP & 0xffff)) > 0xffff;
		HL += SP;
	}
	void Execute0x3A()
	{
		A = _mmu.ReadByte(HL--);
	}
	void Execute0x3B()
	{
		SP--;
	}
	void Execute0x3C()
	{
		INC(ref A);
	}
	void Execute0x3D()
	{
		DEC(ref A);
	}
	void Execute0x3E()
	{
		A = immediate8u;
	}
	void Execute0x3F()
	{
		CarryFlag = !CarryFlag;
		SubtractFlag = false;
		HalfCarryFlag = false;
	}
	void Execute0x40()
	{
	}
	void Execute0x41()
	{
		B = C;
	}
	void Execute0x42()
	{
		B = D;
	}
	void Execute0x43()
	{
		B = E;
	}
	void Execute0x44()
	{
		B = H;
	}
	void Execute0x45()
	{
		B = L;
	}
	void Execute0x46()
	{
		B = _mmu.ReadByte(HL);
	}
	void Execute0x47()
	{
		B = A;
	}
	void Execute0x48()
	{
		C = B;
	}
	void Execute0x49()
	{
	}
	void Execute0x4A()
	{
		C = D;
	}
	void Execute0x4B()
	{
		C = E;
	}
	void Execute0x4C()
	{
		C = H;
	}
	void Execute0x4D()
	{
		C = L;
	}
	void Execute0x4E()
	{
		C = _mmu.ReadByte(HL);
	}
	void Execute0x4F()
	{
		C = A;
	}
	void Execute0x50()
	{
		D = B;
	}
	void Execute0x51()
	{
		D = C;
	}
	void Execute0x52()
	{
	}
	void Execute0x53()
	{
		D = E;
	}
	void Execute0x54()
	{
		D = H;
	}
	void Execute0x55()
	{
		D = L;
	}
	void Execute0x56()
	{
		D = _mmu.ReadByte(HL);
	}
	void Execute0x57()
	{
		D = A;
	}
	void Execute0x58()
	{
		E = B;
	}
	void Execute0x59()
	{
		E = C;
	}
	void Execute0x5A()
	{
		E = D;
	}
	void Execute0x5B()
	{
	}
	void Execute0x5C()
	{
		E = H;
	}
	void Execute0x5D()
	{
		E = L;
	}
	void Execute0x5E()
	{
		E = _mmu.ReadByte(HL);
	}
	void Execute0x5F()
	{
		E = A;
	}
	void Execute0x60()
	{
		H = B;
	}
	void Execute0x61()
	{
		H = C;
	}
	void Execute0x62()
	{
		H = D;
	}
	void Execute0x63()
	{
		H = E;
	}
	void Execute0x64()
	{
	}
	void Execute0x65()
	{
		H = L;
	}
	void Execute0x66()
	{
		H = _mmu.ReadByte(HL);
	}
	void Execute0x67()
	{
		H = A;
	}
	void Execute0x68()
	{
		L = B;
	}
	void Execute0x69()
	{
		L = C;
	}
	void Execute0x6A()
	{
		L = D;
	}
	void Execute0x6B()
	{
		L = E;
	}
	void Execute0x6C()
	{
		L = H;
	}
	void Execute0x6D()
	{
	}
	void Execute0x6E()
	{
		L = _mmu.ReadByte(HL);
	}
	void Execute0x6F()
	{
		L = A;
	}
	void Execute0x70()
	{
		_mmu.WriteByte(HL, B);
	}
	void Execute0x71()
	{
		_mmu.WriteByte(HL, C);
	}
	void Execute0x72()
	{
		_mmu.WriteByte(HL, D);
	}
	void Execute0x73()
	{
		_mmu.WriteByte(HL, E);
	}
	void Execute0x74()
	{
		_mmu.WriteByte(HL, H);
	}
	void Execute0x75()
	{
		_mmu.WriteByte(HL, L);
	}
	void Execute0x76()
	{
		//TODO
	}
	void Execute0x77()
	{
		_mmu.WriteByte(HL, A);
	}
	void Execute0x78()
	{
		A = B;
	}
	void Execute0x79()
	{
		A = C;
	}
	void Execute0x7A()
	{
		A = D;
	}
	void Execute0x7B()
	{
		A = E;
	}
	void Execute0x7C()
	{
		A = H;
	}
	void Execute0x7D()
	{
		A = L;
	}
	void Execute0x7E()
	{
		A = _mmu.ReadByte(HL);
	}
	void Execute0x7F()
	{
	}
	void Execute0x80()
	{
		ADD(B);
	}
	void Execute0x81()
	{
		ADD(C);
	}
	void Execute0x82()
	{
		ADD(D);
	}
	void Execute0x83()
	{
		ADD(E);
	}
	void Execute0x84()
	{
		ADD(H);
	}
	void Execute0x85()
	{
		ADD(L);
	}
	void Execute0x86()
	{
		ADD(_mmu.ReadByte(HL));
	}
	void Execute0x87()
	{
		ADD(A);
	}
	void Execute0x88()
	{
		ADC(B);
	}
	void Execute0x89()
	{
		ADC(C);
	}
	void Execute0x8A()
	{
		ADC(D);
	}
	void Execute0x8B()
	{
		ADC(E);
	}
	void Execute0x8C()
	{
		ADC(H);
	}
	void Execute0x8D()
	{
		ADC(L);
	}
	void Execute0x8E()
	{
		ADC(_mmu.ReadByte(HL));
	}
	void Execute0x8F()
	{
		ADC(A);
	}
	void Execute0x90()
	{
		SUB(B);
	}
	void Execute0x91()
	{
		SUB(C);
	}
	void Execute0x92()
	{
		SUB(D);
	}
	void Execute0x93()
	{
		SUB(E);
	}
	void Execute0x94()
	{
		SUB(H);
	}
	void Execute0x95()
	{
		SUB(L);
	}
	void Execute0x96()
	{
		SUB(_mmu.ReadByte(HL));
	}
	void Execute0x97()
	{
		SUB(A);
	}
	void Execute0x98()
	{
		SBC(B);
	}
	void Execute0x99()
	{
		SBC(C);
	}
	void Execute0x9A()
	{
		SBC(D);
	}
	void Execute0x9B()
	{
		SBC(E);
	}
	void Execute0x9C()
	{
		SBC(H);
	}
	void Execute0x9D()
	{
		SBC(L);
	}
	void Execute0x9E()
	{
		SBC(_mmu.ReadByte(HL));
	}
	void Execute0x9F()
	{
		SBC(A);
	}
	void Execute0xA0()
	{
		AND(B);
	}
	void Execute0xA1()
	{
		AND(C);
	}
	void Execute0xA2()
	{
		AND(D);
	}
	void Execute0xA3()
	{
		AND(E);
	}
	void Execute0xA4()
	{
		AND(H);
	}
	void Execute0xA5()
	{
		AND(L);
	}
	void Execute0xA6()
	{
		AND(_mmu.ReadByte(HL));
	}
	void Execute0xA7()
	{
		AND(A);
	}
	void Execute0xA8()
	{
		XOR(B);
	}
	void Execute0xA9()
	{
		XOR(C);
	}
	void Execute0xAA()
	{
		XOR(D);
	}
	void Execute0xAB()
	{
		XOR(E);
	}
	void Execute0xAC()
	{
		XOR(H);
	}
	void Execute0xAD()
	{
		XOR(L);
	}
	void Execute0xAE()
	{
		XOR(_mmu.ReadByte(HL));
	}
	void Execute0xAF()
	{
		XOR(A);
	}
	void Execute0xB0()
	{
		OR(B);
	}
	void Execute0xB1()
	{
		OR(C);
	}
	void Execute0xB2()
	{
		OR(D);
	}
	void Execute0xB3()
	{
		OR(E);
	}
	void Execute0xB4()
	{
		OR(H);
	}
	void Execute0xB5()
	{
		OR(L);
	}
	void Execute0xB6()
	{
		OR(_mmu.ReadByte(HL));
	}
	void Execute0xB7()
	{
		OR(A);
	}
	void Execute0xB8()
	{
		CP(B);
	}
	void Execute0xB9()
	{
		CP(C);
	}
	void Execute0xBA()
	{
		CP(D);
	}
	void Execute0xBB()
	{
		CP(E);
	}
	void Execute0xBC()
	{
		CP(H);
	}
	void Execute0xBD()
	{
		CP(L);
	}
	void Execute0xBE()
	{
		CP(_mmu.ReadByte(HL));
	}
	void Execute0xBF()
	{
		CP(A);
	}
	
	void Execute0xC1()
	{
		BC = POP();
	}
	void Execute0xC3()
	{
		PC = immediate16;
	}
	void Execute0xC5()
	{
		PUSH(BC);
	}
	void Execute0xC6()
	{
		ADD(immediate8u);
	}
	void Execute0xC9()
	{
		PC = POP();
	}
	void Execute0xCB()
	{
		byte instruction = _mmu.ReadByte(PC + 1);
		pci++;
		cb_opcodes[instruction].Execute();
		//Debug.Write(string.Format(cb_opcodes[instruction].Mnemonic));
	}
	void Execute0xCD()
	{
		PUSH(PC + 2);
		PC = immediate16;
		pci = 0;
	}
	void Execute0xCE()
	{
		ADC(immediate8u);
	}
	void Execute0xD6()
	{
		SUB(immediate8u);
	}
	void Execute0xE0()
	{
		_mmu.WriteByte(0xFF00 + immediate8u, A);
	}
	void Execute0xE2()
	{
		_mmu.WriteByte(0xFF00 + C, A);
	}
	void Execute0xE6()
	{
		AND(immediate8u);
	}
	void Execute0xEA()
	{
		_mmu.WriteByte(immediate16, A);
	}
	void Execute0xF0()
	{
		A = _mmu.ReadByte(0xFF00 + immediate8u);
	}
	void Execute0xF3()
	{
		ime = false;
	}
	void Execute0xFB()
	{
		willEnableIme = true;
	}
	void Execute0xFE()
	{
		CP(immediate8u);
	}
#endregion
#region Execute0xCBXX
	void Execute0xCB11()
	{
		RL(ref C);
	}
	void Execute0xCB17()
	{
		RL(ref A);
	}
	void Execute0xCB3F()
	{
		BIT(4, E);
	}
	void Execute0xCB40()
	{
		BIT(0, B);
	}
	void Execute0xCB50()
	{
		BIT(2, B);
	}
	void Execute0xCB60()
	{
		BIT(4, B);
	}
	void Execute0xCB70()
	{
		BIT(6, B);
	}
	void Execute0xCB7C()
	{
		BIT(7, H);
	}
#endregion
#region helper routines
	void PUSH(int data)
	{
		_mmu.WriteByte(--SP, (byte)data);
		_mmu.WriteByte(--SP, (byte)(data >> 8));
	}
	ushort POP()
	{
		return (ushort)(_mmu.ReadByte(SP++) << 8 | _mmu.ReadByte(SP++));
	}
	void ADD(byte n)
	{
		SubtractFlag = false;
		HalfCarryFlag = ((A & 0xF) + (n & 0xF)) >= 0x10;
		CarryFlag = ((short)A + (short)n) >= 0x100;
		A += n;
		ZeroFlag = A == 0;
	}
	void ADC(byte n)
	{
		if (CarryFlag) n += 1;
		ADD(n);
	}
	void SUB(byte n)
	{
		SubtractFlag = true;
		HalfCarryFlag = (A & 0xF) < (n & 0xF);
		CarryFlag = A < n;
		A -= n;
		ZeroFlag = A == 0;
	}
	void SBC(byte n)
	{
		if (CarryFlag) n += 1;
		SUB(n);
	}
	void AND(byte n)
	{
		SubtractFlag = false;
		HalfCarryFlag = true;
		CarryFlag = false;
		A &= n;
		ZeroFlag = A == 0;
	}
	void XOR(byte n)
	{
		A ^= n;
		ZeroFlag = A == 0;
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
	}
	void OR(byte n)
	{
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = false;
		A |= n;
		ZeroFlag = A == 0;
	}
	void INC(ref byte register)
	{
		SubtractFlag = false;
		HalfCarryFlag = (register & 0xf) == 0xf;
		register++;
		ZeroFlag = register == 0;
	}
	void DEC(ref byte register)
	{
		SubtractFlag = true;
		HalfCarryFlag = (register & 0xf) == 0x0;
		register--;
		ZeroFlag = register == 0;
	}
	void RL(ref byte register)
	{
		SubtractFlag = false;
		HalfCarryFlag = false;
		CarryFlag = (register & 0x80) == 0x80;
		register <<= 1;
		ZeroFlag = register == 0;
	}
	void CP(byte data)
	{
		ZeroFlag = A == data;
		SubtractFlag = true;
		HalfCarryFlag = (A & 0xF) < (data & 0xF);
		CarryFlag = A < data;
	}
	void BIT(byte position, byte register)
	{
		ZeroFlag = !register.CheckBit(position);
		SubtractFlag = false;
		HalfCarryFlag = true;
	}
#endregion
}

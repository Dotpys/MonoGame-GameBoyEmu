using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace JMGBE.Core
{
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
        private byte A = 0x00;
        private byte F = 0x00;
        private byte B = 0x00;
        private byte C = 0x00;
        private byte D = 0x00;
        private byte E = 0x00;
        private byte H = 0x00;
        private byte L = 0x00;

        /// <summary>
        /// Program Counter register.
        /// </summary>
        private ushort PC = 0x0000;
        /// <summary>
        /// Stack Pointer register.
        /// </summary>
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
                {0x0C, new Instruction( 4, Execute0x0C, FetchNone, "INC C")},
                {0x0D, new Instruction( 4, Execute0x0D, FetchNone, "DEC C")},
                {0x0E, new Instruction( 8, Execute0x0E, FetchIm8U, "LD C, 0x{2:X2}")},
                {0x11, new Instruction(12, Execute0x11, FetchIm16, "LD DE, 0x{0:X4}")},
                {0x12, new Instruction( 8, Execute0x12, FetchNone, "LD (DE), A")},
                {0x13, new Instruction( 8, Execute0x13, FetchNone, "INC DE")},
                {0x14, new Instruction( 4, Execute0x14, FetchNone, "INC D")},
                {0x15, new Instruction( 4, Execute0x15, FetchNone, "DEC D")},
                {0x17, new Instruction( 4, Execute0x17, FetchNone, "RLA")},
                {0x18, new Instruction(12, Execute0x18, FetchIm8S, "JR 0x{1:X2}")},
                {0x1A, new Instruction( 8, Execute0x1A, FetchNone, "LD A, (DE)")},
                {0x1C, new Instruction( 4, Execute0x1C, FetchNone, "INC E")},
                {0x1D, new Instruction( 4, Execute0x1D, FetchNone, "DEC E")},
                {0x1E, new Instruction( 8, Execute0x1E, FetchIm8U, "LD E, 0x{2:X2}")},
                {0x20, new Instruction(12, Execute0x20, FetchIm8S, "JR NZ, 0x{1:X2}")},		//TODO: Rivedere il funzionamento di clock cycles (12/8)
                {0x21, new Instruction(12, Execute0x21, FetchIm16, "LD HL, 0x{0:X4}")},
                {0x22, new Instruction( 8, Execute0x22, FetchNone, "LD (HL+), A")},
                {0x23, new Instruction( 8, Execute0x23, FetchNone, "INC HL")},
                {0x24, new Instruction( 4, Execute0x24, FetchNone, "INC H")},
                {0x25, new Instruction( 4, Execute0x25, FetchNone, "DEC H")},
                {0x28, new Instruction(12, Execute0x28, FetchIm8S, "JR Z, 0x{1:X2}")},		//TODO: Rivedere il funzionamento di clock cycles (12/8)
                {0x2C, new Instruction( 4, Execute0x2C, FetchNone, "INC L")},
                {0x2D, new Instruction( 4, Execute0x2D, FetchNone, "DEC L")},
                {0x2E, new Instruction( 8, Execute0x2E, FetchIm8U, "LD L, 0x{2:X2}")},
                {0x31, new Instruction(12, Execute0x31, FetchIm16, "LD SP, 0x{0:X4}")},
                {0x32, new Instruction( 8, Execute0x32, FetchNone, "LD (HL-), A")},
                {0x33, new Instruction( 8, Execute0x33, FetchNone, "INC SP")},
                {0x3C, new Instruction( 4, Execute0x3C, FetchNone, "INC A")},
                {0x3D, new Instruction( 4, Execute0x3D, FetchNone, "DEC A")},
                {0x3E, new Instruction( 8, Execute0x3E, FetchIm8U, "LD A, 0x{2:X2}")},
                {0x4F, new Instruction( 4, Execute0x4F, FetchNone, "LD C, A")},
                {0x50, new Instruction( 4, Execute0x50, FetchNone, "LD D, B")},
                {0x51, new Instruction( 4, Execute0x51, FetchNone, "LD D, C")},
                {0x52, new Instruction( 4, Execute0x52, FetchNone, "LD D, D")},
                {0x53, new Instruction( 4, Execute0x53, FetchNone, "LD D, E")},
                {0x54, new Instruction( 4, Execute0x54, FetchNone, "LD D, H")},
                {0x55, new Instruction( 4, Execute0x55, FetchNone, "LD D, L")},
                {0x57, new Instruction( 4, Execute0x57, FetchNone, "LD D, A")},
                {0x60, new Instruction( 4, Execute0x60, FetchNone, "LD H, B")},
                {0x61, new Instruction( 4, Execute0x61, FetchNone, "LD H, C")},
                {0x62, new Instruction( 4, Execute0x62, FetchNone, "LD H, D")},
                {0x63, new Instruction( 4, Execute0x63, FetchNone, "LD H, E")},
                {0x64, new Instruction( 4, Execute0x64, FetchNone, "LD H, H")},
                {0x65, new Instruction( 4, Execute0x65, FetchNone, "LD H, L")},
                {0x66, new Instruction( 8, Execute0x66, FetchNone, "LD H, (HL)")},
                {0x67, new Instruction( 4, Execute0x67, FetchNone, "LD H, A")},
                {0x77, new Instruction( 8, Execute0x77, FetchNone, "LD (HL), A")},
                {0x7B, new Instruction( 4, Execute0x7B, FetchNone, "LD A, E")},
                {0xAF, new Instruction( 4, Execute0xAF, FetchNone, "XOR A")},
                {0xC1, new Instruction(12, Execute0xC1, FetchNone, "POP BC")},
                {0xC5, new Instruction(16, Execute0xC5, FetchNone, "PUSH BC")},
                {0xC9, new Instruction(16, Execute0xC9, FetchNone, "RET")},
                {0xCB, new Instruction( 4, Execute0xCB, FetchNone, "")},
                {0xCD, new Instruction(24, Execute0xCD, FetchIm16, "CALL 0x{0:X4}")},
                {0xE0, new Instruction(12, Execute0xE0, FetchIm8U, "LD (0xFF00 + 0x{2:X2}), A")},
                {0xE2, new Instruction( 8, Execute0xE2, FetchNone, "LD (0xFF00 + C), A")},
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
                {0x7C, new Instruction( 8, Execute0xCB7C, FetchNone, "BIT 7, H")}
            };
        }

        public void Clock()
        {
			//Skip the memory erasing part.
			//if (PC == 0x007) PC = 0x000C;
			//if (PC == 0x0032)
                //Debug.WriteLine("Test");

			//
			//Debug.Write($"[0x{PC:X4}]: ");
			//

            byte instruction = _mmu.ReadByte(PC);
			pci++;
            opcodes[instruction].Fetch();
            opcodes[instruction].Execute();

			//
            //Debug.WriteLine(string.Format(opcodes[instruction].Mnemonic, immediate16, immediate8s, immediate8u));
			//

			PC += pci;
			pci = 0;
            //Gestione clock

            //Fine gestione clock
            //Gestione interrupt
            //IME enable/disable
            if (instruction != 0xFB && willEnableIme)
            {   //Delays by one instruction the IME set.
                ime = true;
                willEnableIme = false;
            }
            //Interrupt check:
            byte if_reg = _mmu.ReadByte(0xFF0F);
            byte ie_reg = _mmu.ReadByte(0xFFFF);
            for (byte i = 0; i < 5; i++)
            {
                if (ime && ie_reg.CheckBit(i) && if_reg.CheckBit(i))
                {
                    if_reg.SetBit(i, false);
                    PUSH(PC);
                    PC = (ushort)(0x0040 + 0x0008 * i);
                    break;
                }
            }
            _mmu.WriteByte(0xFF0F, if_reg);
            //Fine gestione interrupt
        }

        //Fetch
        void FetchNone() { }
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
        //Execute XX
        void Execute0x00()
        {

        }
        void Execute0x01() => BC = immediate16;
        void Execute0x02() => _mmu.WriteByte(BC, A);
        void Execute0x03() => BC++;
        void Execute0x04() => INC(ref B);
        void Execute0x05() => DEC(ref B);
        void Execute0x06() => B = immediate8u;
        void Execute0x0C() => INC(ref C);
        void Execute0x0D() => DEC(ref C);
        void Execute0x0E() => C = immediate8u;
        void Execute0x11() => DE = immediate16;
        void Execute0x12() => _mmu.WriteByte(DE, A);
        void Execute0x13() => DE++;
        void Execute0x14() => INC(ref D);
        void Execute0x15() => DEC(ref D);
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
        void Execute0x1A() => A = _mmu.ReadByte(DE);
        void Execute0x1C() => INC(ref E);
        void Execute0x1D() => DEC(ref E);
        void Execute0x1E() => E = immediate8u;
        void Execute0x20()
        {
			//If the zero flag is reset...
            if (!ZeroFlag)
				//...the PC will be set relative to the instruction NEXT to JR.
                PC = (ushort)(PC + immediate8s);
        }
        void Execute0x21() => HL = immediate16;
        void Execute0x22() => _mmu.WriteByte(HL++, A);
        void Execute0x23() => HL++;
        void Execute0x24() => INC(ref H);
        void Execute0x25() => DEC(ref H);
        void Execute0x28()
        {
			//If the zero flag is set...
            if (ZeroFlag)
				//...the PC will be set relative to the instruction NEXT to JR.
                PC = (ushort)(PC + immediate8s);
        }
        void Execute0x2C() => INC(ref L);
        void Execute0x2D() => DEC(ref L);
        void Execute0x2E() => L = immediate8u;
        void Execute0x31() => SP = immediate16;
        void Execute0x32() => _mmu.WriteByte(HL--, A);
        void Execute0x33() => SP++;
        void Execute0x3C() => INC(ref A);
        void Execute0x3D() => DEC(ref A);
        void Execute0x3E() => A = immediate8u;
        void Execute0x4F() => C = A;
        void Execute0x50() => D = B;
        void Execute0x51() => D = C;
        void Execute0x52()
        {
        }
        void Execute0x53() => D = E;
        void Execute0x54() => D = H;
        void Execute0x55() => D = L;
        void Execute0x57() => D = A;
        void Execute0x60() => H = B;
        void Execute0x61() => H = C;
        void Execute0x62() => H = D;
        void Execute0x63() => H = E;
        void Execute0x64()
        {
        }
        void Execute0x65() => H = L;
        void Execute0x66() => H = _mmu.ReadByte(HL);
        void Execute0x67() => H = A;
        void Execute0x77() => _mmu.WriteByte(HL, A);
        void Execute0x7B() => A = E;
        void Execute0xAF()
        {
            A = (byte)(A ^ A);
            ZeroFlag = A == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;
        }
        void Execute0xC1() => BC = POP();
        void Execute0xC5() => PUSH(BC);
        void Execute0xC9() => PC = POP();
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
        void Execute0xE0() => _mmu.WriteByte(0xFF00 + immediate8u, A);
        void Execute0xE2() => _mmu.WriteByte(0xFF00 + C, A);
        void Execute0xEA() => _mmu.WriteByte(immediate16, A);
        void Execute0xF0() => A = _mmu.ReadByte(0xFF00 + immediate8u);
        void Execute0xF3() => ime = false;
        void Execute0xFB() => willEnableIme = true;
        void Execute0xFE() => CP(immediate8u);
        //Execute CBXX
        void Execute0xCB11() => RL(ref C);
        void Execute0xCB17() => RL(ref A);
        void Execute0xCB40() => BIT(0, B);
        void Execute0xCB50() => BIT(2, B);
        void Execute0xCB60() => BIT(4, B);
        void Execute0xCB70() => BIT(6, B);
        void Execute0xCB7C() => BIT(7, H);
        //Helper routines
        void PUSH(int data)
        {
            _mmu.WriteByte(--SP, (byte)data);
            _mmu.WriteByte(--SP, (byte)(data >> 8));
        }
        ushort POP()
        {
            return (ushort)(_mmu.ReadByte(SP++) << 8 | _mmu.ReadByte(SP++));
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
    }
}

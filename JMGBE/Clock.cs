using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace JMGBE.Core
{
	public class Clock
	{
		private Memory<byte> timer_registers;
		private Timer tClock;
		private Timer dividerClock;
		private Timer timer;

		public byte Divider { get; set; }
		public byte Counter { get; set; }
		public byte Modulo { get; set; }
		/*public ClockSpeeds Speed
		{
			get { return (ClockSpeeds)(control & 0b00000011); }
			set { control = (byte)((byte)(control & 0b11111100) | ((byte)value)); }
		}*/
		public bool IsRunning
		{
			get { return timer_registers.Span[3].CheckBit(2); }
			set { timer_registers.Span[3].SetBit(2, value); }
		}

		public Clock(Memory memory)
		{
			//Gets the registers from 0xFF04 to 0xFF07
			timer_registers = memory.raw_memory.Slice(0xFF04, 4);



			timer = new Timer();
		}

		private void OnClock()
		{

		}

		public enum ClockSpeeds
		{
			Hz4096 = 0b00,
			Hz16384 = 0b11,
			Hz65536 = 0b10,
			Hz262144 = 0b01
		}
	}
}

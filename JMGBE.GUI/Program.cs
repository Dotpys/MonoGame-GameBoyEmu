using System;
using System.Threading;
using JMGBE.Core;

namespace JMGBE.MonoGame;

public static class Program
{
	static CPU cpu;

	[STAThread]
	static void Main()
	{
		MMU mmu = new MMU();
		mmu.LoadRom("roms\\Tetris.gb");
		cpu = new CPU(mmu);
		Thread a = new Thread(ThreadExec);
		a.Start();

		using (GameBoyEmulator game = new GameBoyEmulator(cpu, mmu))
			game.Run();
	}

	private static void ThreadExec()
	{
		while(true)
		{
			cpu.Clock();
		}
	}
}

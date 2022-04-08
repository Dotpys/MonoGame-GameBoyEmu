using System;

namespace JMGBE.Core;

public record Instruction
{
	public float Cycles { get; set; }
	public Action Execute { get; set; }
	public Action Fetch { get; set; }
	public string Mnemonic { get; set; }

	public Instruction(float cycles, Action executeProc, Action fetchProc, string mnemonic)
	{
		Cycles = cycles;
		Execute = executeProc;
		Fetch = fetchProc;
		Mnemonic = mnemonic;
	}
}
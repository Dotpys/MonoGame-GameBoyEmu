using JMGBE;
using JMGBE.Core;
using System;
using System.Threading;

namespace JMGBEDBG
{
	class Program
	{
		static void Main()
		{
			Memory m = new Memory();
			CPU c = new CPU(m);
			while (true)
			{
				c.NextInstruction();
				//Thread.Sleep(1);
			}
		}
	}
}

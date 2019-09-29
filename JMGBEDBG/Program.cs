using JMGBE;
using JMGBE.Core;
using System;

namespace JMGBEDBG
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Test GBA!\n");
			CPU c = new CPU();
			while (true)
			{
				c.Execute();
			}
		}
	}
}

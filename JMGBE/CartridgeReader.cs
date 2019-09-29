using System;
using System.IO;

namespace JMGBE.Core
{
	public class CartridgeReader
	{
		public byte[] cartridgeCode;

		public void OpenRom(string file)
		{
			cartridgeCode = File.ReadAllBytes(file);
		}
	}
}

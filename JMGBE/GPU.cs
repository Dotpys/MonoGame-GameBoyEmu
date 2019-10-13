namespace JMGBE.Core
{
	public class GPU
	{
		private readonly Memory _memory;
		public byte[] tiles = new byte[4096];

		public GPU(Memory memory)
		{
			_memory = memory;
		}

		public void LoadTilesImage()
		{
			int index = 0;

			for (int j = 0; j < 8; j++)
			{
				for (int i = 0; i < 8; i++)
				{
					tiles[index++] = _memory.ReadByte((ushort)(0x8000 + (i * 16) + (j * 2)));
					tiles[index++] = _memory.ReadByte((ushort)(0x8001 + (i * 16) + (j * 2)));
				}
			}
		}
	}
}

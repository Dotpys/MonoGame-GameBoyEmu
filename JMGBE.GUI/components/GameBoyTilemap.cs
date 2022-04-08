using System;
using JMGBE.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JMGBE.MonoGame.Components;

public class GameBoyTilemap : DrawableGameComponent
{
	private SpriteBatch _spriteBatch;
	private MMU _mmu;

	public Texture2D[] Tiles { get; init; }
	public RenderTarget2D TileMap { get; init; }
	
	public GameBoyTilemap(Game game, SpriteBatch spriteBatch, MMU mmu) : base(game)
	{
		_spriteBatch = spriteBatch;
		_mmu = mmu;

		Tiles = new Texture2D[256];
		for (int i = 0; i < 256; i++)
			Tiles[i] = new Texture2D(GraphicsDevice, 8, 8, false, SurfaceFormat.Color);
		TileMap = new RenderTarget2D(GraphicsDevice, 128, 128);
	}

	public override void Initialize()
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
	}

	public override void Update(GameTime gameTime)
	{
	}

	public override void Draw(GameTime gameTime)
	{
		int tileMapAddress = _mmu[Registers.LCDC].CheckBit(4) ? 0x8000 : 0x8800;
		byte[] tileData = new byte[256];
		for (int tileIndex=0; tileIndex<256; tileIndex++)
		{
			for(int rowIndex=0; rowIndex<8; rowIndex++)
			{
				byte lsb = _mmu[(ushort)(tileMapAddress + tileIndex*16 + rowIndex*2 + 0)];
				byte msb = _mmu[(ushort)(tileMapAddress + tileIndex*16 + rowIndex*2 + 1)];
				for(int colIndex=0; colIndex<8; colIndex++)
				{
					int val = 0;
					val |= lsb.CheckBit(7-colIndex) ? 0x01 : 0x00;
					val |= msb.CheckBit(7-colIndex) ? 0x10 : 0x00;
					byte color = val switch
					{
						0x00 => 0xff,
						0x01 => 0xaa,
						0x10 => 0x55,
						0x11 => 0x00,
						_ => throw new ArithmeticException()
					};
					tileData[4*8*rowIndex + 4*colIndex+0] = color;
					tileData[4*8*rowIndex + 4*colIndex+1] = color;
					tileData[4*8*rowIndex + 4*colIndex+2] = color;
					tileData[4*8*rowIndex + 4*colIndex+3] = 0xff;
				}
			}
			Tiles[tileIndex].SetData(tileData, 0, 256);
		}

		_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
		GraphicsDevice.SetRenderTarget(TileMap);
		for(int row=0; row<16; row++)
		{
			for(int col=0; col<16; col++)
			{
				_spriteBatch.Draw(Tiles[16*row + col], new Rectangle(col*8, row*8, 8, 8), Color.White);
			}
		}
		_spriteBatch.End();
	}
}
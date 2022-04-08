using JMGBE.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JMGBE.MonoGame.Components;

public class GameBoyBackground : DrawableGameComponent
{
	private SpriteBatch _spriteBatch;
	private MMU _mmu;
	private GameBoyTilemap _tilemap;

	public RenderTarget2D Background { get; init; }

	public GameBoyBackground(Game game, SpriteBatch spriteBatch, MMU mmu, GameBoyTilemap tilemap) : base(game)
	{
		_spriteBatch = spriteBatch;
		_mmu = mmu;
		_tilemap = tilemap;

		Background = new RenderTarget2D(GraphicsDevice, 256, 256);
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
		int tileMapAddress = _mmu[Registers.LCDC].CheckBit(3) ? 0x9C00 : 0x9800;

		_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
		GraphicsDevice.SetRenderTarget(Background);
		for(int row=0; row<32; row++)
		{
			for(int col=0; col<32; col++)
			{
				//_spriteBatch.Draw(Tiles[16*row + col], new Rectangle(col*8, row*8, 8, 8), Color.White);
				_spriteBatch.Draw(_tilemap.Tiles[_mmu[(ushort)(tileMapAddress + 32*row + col)]], new Rectangle(col*8, row*8, 8, 8), Color.White);
			}
		}
		_spriteBatch.End();
	}
}
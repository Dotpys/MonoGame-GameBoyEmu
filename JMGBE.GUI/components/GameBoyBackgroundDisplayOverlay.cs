using JMGBE.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JMGBE.MonoGame.Components;

public class GameBoyBackgroundDisplayOverlay : DrawableGameComponent
{
	private const int DMG_SCREEN_WIDTH = 160;
	private const int DMG_SCREEN_HEIGHT = 144;

	private SpriteBatch _spriteBatch;
	private MMU _mmu;
	private GameBoyTilemap _tilemap;

	public RenderTarget2D BackgroundDisplayOverlay { get; init; }
	public bool OverlayEnabled { get; set; }

	public GameBoyBackgroundDisplayOverlay(Game game, SpriteBatch spriteBatch, MMU mmu, bool enable) : base(game)
	{
		_spriteBatch = spriteBatch;
		_mmu = mmu;

		BackgroundDisplayOverlay = new RenderTarget2D(GraphicsDevice, 160, 144);
		OverlayEnabled = enable;
	}

	public override void Initialize()
	{
		byte[] pixelData = new byte[4 * DMG_SCREEN_WIDTH * DMG_SCREEN_HEIGHT];
		for(int row=0; row<DMG_SCREEN_HEIGHT; row++)
		{
			for(int col=0; col<DMG_SCREEN_WIDTH; col++)
			{
				if (row == 0 || row == DMG_SCREEN_HEIGHT-1 || col == 0 || col == DMG_SCREEN_WIDTH-1)
				{
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 0] = 255;
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 1] = 0;
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 2] = 0;
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 3] = 128;
				}
				else
				{
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 0] = 255;
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 1] = 0;
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 2] = 0;
					pixelData[4*DMG_SCREEN_WIDTH*row+4*col + 3] = 32;
				}
					
			}
		}
		BackgroundDisplayOverlay.SetData(pixelData, 0, 4 * DMG_SCREEN_WIDTH * DMG_SCREEN_HEIGHT);
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
		
	}
}
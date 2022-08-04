using JMGBE.Core;
using JMGBE.MonoGame.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JMGBE.MonoGame;

public class GameBoyEmulator : Game
{
	private readonly GraphicsDeviceManager _graphics;
	//oggetto che disegna le texture a video.
	private SpriteBatch _spriteBatch;
	private CPU _cpu;
	private MMU _mmu;
	private GameBoyTilemap _dmgTilemap;
	private GameBoyBackground _dmgBackground;
	private GameBoyDisplay _dmgDisplay;
	private GameBoyBackgroundDisplayOverlay _dmgBackgroundDisplayOverlay;

	private const int DMG_SCREEN_WIDTH = 160;
	private const int DMG_SCREEN_HEIGHT = 144;

	private SpriteFont debugFont;

	public GameBoyEmulator(CPU cpu, MMU mmu)
	{
		_cpu = cpu;
		_mmu = mmu;
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1800,
			PreferredBackBufferHeight = 1000
		};
		
		IsMouseVisible = true;
	}

	protected override void Initialize()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		_dmgTilemap = new GameBoyTilemap(this, _spriteBatch, _mmu);
		_dmgBackground = new GameBoyBackground(this, _spriteBatch, _mmu, _dmgTilemap);
		_dmgBackgroundDisplayOverlay = new GameBoyBackgroundDisplayOverlay(this, _spriteBatch, _mmu, true);

		_dmgBackgroundDisplayOverlay.Initialize();
		base.Initialize();
	}

	protected override void LoadContent()
	{
		debugFont = Content.Load<SpriteFont>("DebugFont");
	}

	private byte[] PaletteToRGBA(byte[] in_arr)
	{
		byte[] result = new byte[in_arr.Length * 4];
		for (int i = 0; i < in_arr.Length; i++)
		{
			result[4 * i + 0] = in_arr[i];
			result[4 * i + 1] = in_arr[i];
			result[4 * i + 2] = in_arr[i];
			result[4 * i + 3] = 0xFF;
		}
		return result;
	}

	protected override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		byte stat = 0;
		_mmu[Registers.LY] = 0;
		for (int i=0; i<154; i++)
		{
			//Per ogni scanline
			for (int j=0; j<114; j++)
			{
		
				stat = _mmu[Registers.STAT];
				if (j<20)
				{
					//Mode 2 10
					stat.SetBit(1, true);
					stat.SetBit(0, false);
				}
				if (j>20)
				{
					//Mode 3 11
					stat.SetBit(1, true);
					stat.SetBit(0, true);
				}
				if (j>63)
				{
					//Mode 0 00
					stat.SetBit(1, false);
					stat.SetBit(0, false);
				}
				if (i>143)
				{
					//Mode 1 01
					stat.SetBit(1, false);
					stat.SetBit(0, true);
				}
				_mmu[Registers.STAT] = stat;
			}
			_mmu[Registers.LY]++;
		}

		if (_mmu[Registers.LY] == _mmu[Registers.LYC])
		{
			/*
The Game Boy permanently compares the value of the LYC and LY registers.
When both values are identical, the “LYC=LY” flag in the STAT register is set, and (if enabled) a STAT interrupt is requested.
			*/
		}

		_dmgTilemap.Draw(gameTime);
		_dmgBackground.Draw(gameTime);

		_spriteBatch.Begin(samplerState: SamplerState.PointClamp);
		GraphicsDevice.SetRenderTarget(null);
		////////////////////
		_spriteBatch.Draw(_dmgTilemap.TileMap, new Rectangle(2, 2, 128 * 2, 128 * 2), Color.White);
		_spriteBatch.Draw(_dmgBackground.Background, new Rectangle(260, 2, 256 * 2, 256 * 2), Color.White);
		_spriteBatch.Draw(_dmgBackgroundDisplayOverlay.BackgroundDisplayOverlay, new Rectangle(260, 2 + _mmu[Registers.SCY]*2, DMG_SCREEN_WIDTH*2, DMG_SCREEN_HEIGHT*2), Color.White);
		_spriteBatch.End();
		//////////////////////////////////////////////

		//Trigghera l'interrupt V-Blank
		byte oldif = _mmu.ReadByte(0xFF0F);
		oldif.SetBit(0, true);

		//Trigghera l'interrupt STAT
		byte oldstat= _mmu[Registers.STAT];
		if (oldstat.CheckBit(6))
		{
			if (_mmu[Registers.LY] == _mmu[Registers.LYC])
			{
				oldif.SetBit(1, true);
			}
		}
		_mmu.WriteByte(0xFF0F, oldif);
		base.Draw(gameTime);

		_spriteBatch.Begin();
		GraphicsDevice.SetRenderTarget(null);
		int b = 100;
		//8 bit registers
		_spriteBatch.DrawString(debugFont, $"A: (0x{_cpu.A:X2}) {_cpu.A}", new Vector2(2, b + 300), Color.White);
		_spriteBatch.DrawString(debugFont, $"F: (0x{_cpu.F:X2}) {_cpu.F}", new Vector2(2, b + 320), Color.White);
		_spriteBatch.DrawString(debugFont, $"B: (0x{_cpu.B:X2}) {_cpu.B}", new Vector2(2, b + 340), Color.White);
		_spriteBatch.DrawString(debugFont, $"C: (0x{_cpu.C:X2}) {_cpu.C}", new Vector2(2, b + 360), Color.White);
		_spriteBatch.DrawString(debugFont, $"D: (0x{_cpu.D:X2}) {_cpu.D}", new Vector2(2, b + 380), Color.White);
		_spriteBatch.DrawString(debugFont, $"E: (0x{_cpu.E:X2}) {_cpu.E}", new Vector2(2, b + 400), Color.White);
		_spriteBatch.DrawString(debugFont, $"H: (0x{_cpu.H:X2}) {_cpu.H}", new Vector2(2, b + 420), Color.White);
		_spriteBatch.DrawString(debugFont, $"H: (0x{_cpu.H:X2}) {_cpu.H}", new Vector2(2, b + 440), Color.White);

		//16 bit registers
		_spriteBatch.DrawString(debugFont, $"BC: (0x{_cpu.BC:X4}) {_cpu.BC}", new Vector2(2, b + 480), Color.White);
		_spriteBatch.DrawString(debugFont, $"DE: (0x{_cpu.DE:X4}) {_cpu.DE}", new Vector2(2, b + 500), Color.White);
		_spriteBatch.DrawString(debugFont, $"HL: (0x{_cpu.HL:X4}) {_cpu.HL}", new Vector2(2, b + 520), Color.White);

		//PC e SP
		_spriteBatch.DrawString(debugFont, $"SP: (0x{_cpu.SP:X4}) {_cpu.SP:X4}", new Vector2(2, b + 560), Color.White);
		_spriteBatch.DrawString(debugFont, $"PC: (0x{_cpu.PC:X4}) {_cpu.PC:X4}", new Vector2(2, b + 580), Color.White);

		//Current instruction
		_spriteBatch.DrawString(debugFont, $"{_cpu.PC:X4} (3F)(5E)(C2) PUSH HL", new Vector2(1420, 2), Color.White);
		_spriteBatch.End();
	}
}

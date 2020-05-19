using System.IO;
using JMGBE.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace JMGBE.GUI
{
    public class GameBoyEmulator : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D[] tile_map;
        private Texture2D screen_mask;
        private byte[] background_map;
        private byte[] tile_colors;
        private MMU _mmu;

        public GameBoyEmulator(MMU mmu)
        {
            _mmu = mmu;
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 774,
                PreferredBackBufferHeight = 516
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            tile_map = new Texture2D[256];
            for (int i = 0; i < 256; i++)
                tile_map[i] = new Texture2D(_graphics.GraphicsDevice, 8, 8, false, SurfaceFormat.Color);
            screen_mask = new Texture2D(_graphics.GraphicsDevice, 162, 146, false, SurfaceFormat.Color);

            byte[] screen_mask_data = new byte[94608];
            for (int y = 0; y < 146; y++)
            {
                for (int x = 0; x < 162; x++)
                {
                    if (x == 0 || x == 161 || y == 0 || y == 145)
                    {
                        screen_mask_data[y*4*162 + x*4 + 0] = 0xFF;
                        screen_mask_data[y*4*162 + x*4 + 1] = 0xFF;
                        screen_mask_data[y*4*162 + x*4 + 2] = 0xFF;
                        screen_mask_data[y*4*162 + x*4 + 3] = 0xAA;
                    }
                }
            }
            screen_mask.SetData(screen_mask_data);

            tile_colors = new byte[16384];
            background_map = new byte[1024];
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
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
            // Gestisca la VRAM (TODO: Spostare il tutto in una classe a parte.)
            int tile_map_addr = _mmu.LCDC.CheckBit(4) ? 0x8000 : 0x8800;
            int bakg_map_addr = _mmu.LCDC.CheckBit(3) ? 0x9C00 : 0x9800;

            int pixel_index = 0;
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    byte lob = _mmu.ReadByte(tile_map_addr + i * 16 + j * 2 + 0);
                    byte hib = _mmu.ReadByte(tile_map_addr + i * 16 + j * 2 + 1);
                    for (int k = 7; k >= 0; k--)
                    {
                        if (!hib.CheckBit(k) && !lob.CheckBit(k))
                            tile_colors[pixel_index++] = 0xFF;//0b00
                        else if (!hib.CheckBit(k) && lob.CheckBit(k))
                            tile_colors[pixel_index++] = 0xAA;//0b01
                        else if (hib.CheckBit(k) && !lob.CheckBit(k))
                            tile_colors[pixel_index++] = 0x55;//0b10
                        else
                            tile_colors[pixel_index++] = 0x00;//0b11
                    }
                }
            }

            byte[] textureData = PaletteToRGBA(tile_colors);

            for (int i = 0; i < 256; i++)
            {
                tile_map[i].SetData(textureData, 256 * i, 256);
            }

            int background_index = 0;
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    background_map[background_index++] = _mmu.ReadByte(bakg_map_addr + 32 * y + x);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            //Tilemap.
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    _spriteBatch.Draw(tile_map[16 * y + x], new Rectangle(x * 16 + 2, y * 16 + 2, 16, 16), Color.White);
                }
            }
            //Background.
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {

                    _spriteBatch.Draw(tile_map[background_map[y * 32 + x]], new Rectangle(x * 16 + 260, y * 16 + 2, 16, 16), Color.White);
                }
            }

            Color screen_color = _mmu.LCDC.CheckBit(7) ? Color.Green : Color.Red;
            _spriteBatch.Draw(screen_mask, new Rectangle(258 + _mmu.SCX*2, 0 + _mmu.SCY*2, 324, 292), screen_color);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

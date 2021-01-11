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
        //oggetto che disegna le texture a video.
        private SpriteBatch _spriteBatch;
        //array di 256 texture che contiene tutte le tile attualmente in vram.
        private Texture2D[] tile_map;
        private byte[] background_map;
        private byte[] tile_colors;
        private MMU _mmu;
        private RenderTarget2D dbg_gb_tile_palette;
        private RenderTarget2D dbg_gb_bg;
        private RenderTarget2D dbg_gb_display;


        public GameBoyEmulator(MMU mmu)
        {
            _mmu = mmu;
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1774,
                PreferredBackBufferHeight = 916
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //Inizializzo in memoria 256 texture 8x8 per ogni tile.
            tile_map = new Texture2D[256];
            for (int i = 0; i < 256; i++)
                tile_map[i] = new Texture2D(_graphics.GraphicsDevice, 8, 8, false, SurfaceFormat.Color);

            dbg_gb_display = new RenderTarget2D(_graphics.GraphicsDevice, 160, 144);
            dbg_gb_tile_palette = new RenderTarget2D(_graphics.GraphicsDevice, 128, 128);
            dbg_gb_bg = new RenderTarget2D(_graphics.GraphicsDevice, 256, 256);

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
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
#region TILEMAP_TEXTURES_GEN
            // Gestisca la VRAM (TODO: Spostare il tutto in una classe a parte.)
            int tile_map_addr = _mmu.LCDC.CheckBit(4) ? 0x8000 : 0x8800;
            int bakg_map_addr = _mmu.LCDC.CheckBit(3) ? 0x9C00 : 0x9800;
            // Crea un array per una tile (formato RGBA)
            byte[] tile_data = new byte[256];
            int pixel_index = 0;
            //Scorrimento Tile per Tile.
            for (int tile_index = 0; tile_index < 256; tile_index++)
            {
                pixel_index = 0;
                //Scorrimento Riga per Riga.
                for (int row_index = 0; row_index < 8; row_index++)
                {
                    //LSB Dei pixel
                    byte loB = _mmu.ReadByte(tile_map_addr + tile_index * 16 + row_index * 2 + 0);
                    //MSB Dei pixel
                    byte hiB = _mmu.ReadByte(tile_map_addr + tile_index * 16 + row_index * 2 + 1);
                    //Scorrimento Pixel per Pixel.
                    for (int k = 7; k >= 0; k--)
                    {
                        if (!hiB.CheckBit(k) && !loB.CheckBit(k))
                        {   //0b00
                            tile_data[pixel_index*4+0] = 0xFF;
                            tile_data[pixel_index*4+1] = 0xFF;
                            tile_data[pixel_index*4+2] = 0xFF;
                            tile_data[pixel_index*4+3] = 0xFF;
                        }
                        else if (!hiB.CheckBit(k) && loB.CheckBit(k))
                        {   //0b01
                            tile_data[pixel_index*4+0] = 0xAA;
                            tile_data[pixel_index*4+1] = 0xAA;
                            tile_data[pixel_index*4+2] = 0xAA;
                            tile_data[pixel_index*4+3] = 0xFF;
                        }
                        else if (hiB.CheckBit(k) && !loB.CheckBit(k))
                        {   //0b10
                            tile_data[pixel_index*4+0] = 0x55;
                            tile_data[pixel_index*4+1] = 0x55;
                            tile_data[pixel_index*4+2] = 0x55;
                            tile_data[pixel_index*4+3] = 0xFF;
                        }
                        else
                        {   //0b11
                            tile_data[pixel_index*4+0] = 0x00;
                            tile_data[pixel_index*4+1] = 0x00;
                            tile_data[pixel_index*4+2] = 0x00;
                            tile_data[pixel_index*4+3] = 0xFF;
                        }
                        pixel_index++;
                    }
                }
                //Aggiorna nella 
                tile_map[tile_index].SetData(tile_data, 0, 256);
            }
#endregion

            ////////////////////////////////////////////////
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            GraphicsDevice.SetRenderTarget(dbg_gb_bg);
            //////////////////////
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    _spriteBatch.Draw(tile_map[background_map[y*32+x]], new Rectangle(x * 8, y * 8, 8, 8), Color.White);
                    background_map[32*y+x] = _mmu.ReadByte(bakg_map_addr + 32 * y + x);
                }
            }
            //////////////////////
            _spriteBatch.End();
            ////////////////////////////////////////////////



            ////////////////////////////////////////////////
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            GraphicsDevice.SetRenderTarget(dbg_gb_display);
            //////////////////////
            //Display.
            _spriteBatch.Draw(dbg_gb_bg, new Vector2(0, 0), new Rectangle(_mmu.SCX, _mmu.SCY, 160, 144), Color.White);
            byte stat = 0;
            _mmu.LY = 0;
            for (int i=0; i<154; i++)
            {
                //Per ogni scanline
                for (int j=0; j<114; j++)
                {

                    stat = _mmu.STAT;
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
                    _mmu.STAT = stat;
                }
                _mmu.LY++;
            }
            //////////////////////
            _spriteBatch.End();
            ////////////////////////////////////////////////



            ////////////////////////////////////////////////
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            GraphicsDevice.SetRenderTarget(null);
            //////////////////////
            //Tilemap.
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    _spriteBatch.Draw(tile_map[16 * y + x], new Rectangle(x * 16 + 2, y * 16 + 2, 16, 16), Color.White);
                }
            }
            //Background
            _spriteBatch.Draw(dbg_gb_bg, new Rectangle(260, 2, 512, 512), Color.White);
            //Display
            _spriteBatch.Draw(dbg_gb_display, new Rectangle(774, 2, 320, 288), Color.White);
            //////////////////////
            _spriteBatch.End();
            ////////////////////////////////////////////////

            //Trigghera l'interrupt V-Blank
            byte oldif = _mmu.ReadByte(0xFF0F);
            oldif.SetBit(0, true);

            //Trigghera l'interrupt STAT
            byte oldstat= _mmu.STAT;
            if (oldstat.CheckBit(6))
            {
                if (_mmu.LY == _mmu.LYC)
                {
                    oldif.SetBit(1, true);
                }
            }
            _mmu.WriteByte(0xFF0F, oldif);
            base.Draw(gameTime);
        }
    }
}

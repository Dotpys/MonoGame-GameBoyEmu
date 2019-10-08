using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JMGBEVDBG
{
	public partial class MainWindow : Window
	{

		private static int x = 80;
		private static int y = 72;
		private WriteableBitmap img;

		public MainWindow()
		{
			byte[] videoBuffer= new byte[160 * 144];
			for(int i=0; i<160*144; i++)
			{
				if (i < 160 * 36)		videoBuffer[i] = 0b00;
				else if (i < 160 * 72)	videoBuffer[i] = 0b01;
				else if (i < 160 * 104) videoBuffer[i] = 0b10;
				else if (i < 160 * 144) videoBuffer[i] = 0b11;
			}
			//End loading
			InitializeComponent();
			img = new WriteableBitmap(160, 144, 82.79, 82.79, PixelFormats.Gray2, null);
			UpdateTexture(videoBuffer);
		}

		public void UpdateTexture(byte[] buf)
		{
			img.Lock();
			unsafe
			{
				IntPtr p = img.BackBuffer;
				for (int i=0; i<buf.Length; i+=4)
				{
					*(int*)p = (buf[i] << 6) | (buf[i + 1] << 4) | (buf[i + 2] << 2) | buf[i + 3];
					p += 1;
				}
			}
			img.AddDirtyRect(new Int32Rect(0, 0, 160, 144));
			img.Unlock();
			display.Source = img;
		}
	}
}

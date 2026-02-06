using System.Drawing;
using System.Windows.Forms;

namespace Manager
{
	public partial class ImageDisplay: UserControl
	{
		public ImageDisplay ()
		{
			InitializeComponent ();
			IconSize = 16;
			Ratio = (decimal)(8.0 / 7.0);
		}
		private Size iconSize = new Size (16, 16);
		private decimal ratio = (decimal)(8.0 / 7.0);
		private bool originImgSize = false;
		public void RefreshPictureDisplay ()
		{
			if (originImgSize)
			{
				var backSizeWidth = (foregroundPicture.Image?.Size.Width ?? 0) * ratio;
				var backSizeHeight = (foregroundPicture.Image?.Size.Width ?? 0) * ratio;
				foregroundPicture.Size = ForegroundImage.Size;
				backgroundPanel.Size = new Size ((int)backSizeWidth, (int)backSizeHeight);
				sizeDisplay.Text = backgroundPanel.Size.ToString ();
			}
			else
			{
				foregroundPicture.Size = new Size (
					(int)(iconSize.Width / ratio),
					(int)(iconSize.Height / ratio)
				);
				backgroundPanel.Size = iconSize;
			}
			foregroundPicture.Left = (int)((backgroundPanel.Width - foregroundPicture.Width) * 0.5);
			foregroundPicture.Top = (int)((backgroundPanel.Height - foregroundPicture.Height) * 0.5);
			this.Size = new Size (
				(int)(iconSize.Width * 1),
				(int)((iconSize.Height + sizeDisplay.Height) * 1)
			);
		}
		public decimal Ratio
		{
			get { return ratio; }
			set
			{
				ratio = value;
				RefreshPictureDisplay ();
			}
		}
		public int IconSize
		{
			get { return iconSize.Width; }
			set
			{
				sizeDisplay.Text = value.ToString ();
				iconSize = new Size (value, value);
				RefreshPictureDisplay ();
			}
		}
		public bool IsOriginPicSize
		{
			get { return originImgSize; }
			set { originImgSize = true; RefreshPictureDisplay (); }
		}
		public Color BackgroundColor { get { return backgroundPanel.BackColor; } set { backgroundPanel.BackColor = value; } }
		public Image ForegroundImage { get { return foregroundPicture.Image; } set { foregroundPicture.Image = value; } }
	}
}

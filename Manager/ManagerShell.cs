using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
namespace Manager
{
	public partial class ManagerShell: WAShell.WebAppForm
	{
		public ManagerShell ()
		{
			InitializeComponent ();
			try
			{
				var relativePath = DataUtils.VisualElementsStore.Vemanifest.SplashScreenImage (Program.g_appId);
				var img = Image.FromFile (relativePath);
				SplashScreen.SplashImage = img;
			} catch (Exception e) {
				var ex = e;
			}
			try
			{
				SplashScreen.SplashBackgroundColor = DataUtils.UITheme.StringToColor (DataUtils.VisualElementsStore.Vemanifest.SplashScreenBackgroundColor (Program.g_appId));
			}
			catch { }
			InitSize ();
		}
		private void InitSize ()
		{
			uint ww = 0, wh = 0;
			var ini = Bridge.InitFileStore.Config;
			var setsect = ini ["Settings"];
			var savepos = setsect.GetKey ("PackageManager:SavePosAndSizeBeforeCancel");
			var lastw = setsect.GetKey ("PackageManager:LastWidth");
			var lasth = setsect.GetKey ("PackageManager:LastHeight");
			var defw = setsect.GetKey ("PackageManager:DefaultWidth");
			var defh = setsect.GetKey ("PackageManager:DefaultHeight");
			var minw = setsect.GetKey ("PackageManager:MinimumWidth");
			var minh = setsect.GetKey ("PackageManager:MinimumHeight");
			var lasts = setsect.GetKey ("PackageManager:LastWndState");
			if (savepos.ReadBool ())
			{
				ww = lastw.ReadUInt (defw.ReadUInt (Properties.Resources.IDS_DEFAULTWIDTH.ParseTo <uint> ()));
				wh = lasth.ReadUInt (defh.ReadUInt (Properties.Resources.IDS_DEFAULTHEIGHT.ParseTo <uint> ()));
			}
			else
			{
				ww = defw.ReadUInt (Properties.Resources.IDS_DEFAULTWIDTH.ParseTo<uint> ());
				wh = defh.ReadUInt (Properties.Resources.IDS_DEFAULTHEIGHT.ParseTo<uint> ());
			}
			ClientSize = new Size ((int)(ww * DataUtils.UITheme.DPIDouble), (int)(wh * DataUtils.UITheme.DPIDouble));
			int hborder = Size.Width - ClientSize.Width,
				vborder = Size.Height - ClientSize.Height;
			MinimumSize = new Size (
				(int)(minw.ReadUInt (Properties.Resources.IDS_MINWIDTH.ParseTo <uint> ()) * DataUtils.UITheme.DPIDouble) + hborder,
				(int)(minh.ReadUInt (Properties.Resources.IDS_MINHEIGHT.ParseTo <uint> ()) * DataUtils.UITheme.DPIDouble) + vborder
			);
			WindowState = (FormWindowState)lasts.ReadInt ((int)FormWindowState.Normal);
		}
		private void ManagerShell_Load (object sender, EventArgs e)
		{
			var root = Path.GetDirectoryName (DataUtils.Utilities.GetCurrentProgramPath ());
			WebUI.Navigate (Path.Combine (root, "html\\manager.html"));
		}
		private void ManagerShell_Resize (object sender, EventArgs e)
		{
			var ini = Bridge.InitFileStore.Config;
			var setsect = ini ["Settings"];
			var savepos = setsect.GetKey ("PackageManager:SavePosAndSizeBeforeCancel");
			var lastw = setsect.GetKey ("PackageManager:LastWidth");
			var lasth = setsect.GetKey ("PackageManager:LastHeight");
			var lasts = setsect.GetKey ("PackageManager:LastWndState");
			switch (WindowState)
			{
				case FormWindowState.Normal:
				case FormWindowState.Maximized:
					lasts.Write ((int)WindowState);
					break;
			}
			if (WindowState == FormWindowState.Normal && savepos.ReadBool ())
			{
				lastw.Write ((int)(ClientSize.Width / DataUtils.UITheme.DPIDouble));
				lasth.Write ((int)(ClientSize.Height / DataUtils.UITheme.DPIDouble));
			}
		}
	}
}

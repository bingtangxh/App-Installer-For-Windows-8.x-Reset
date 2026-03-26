using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace Reader
{
	public partial class ReaderShell: WAShell.WebAppForm
	{
		public ReaderShell ()
		{
			InitializeComponent ();
			//this.PublicObjectForScripting = new BridgeExt (this, this, this, this);
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
			Text = Bridge.ResXmlStore.StringRes.Get ("MANAGER_APPTITLE");
			this.Load += Form_Load;
		}
		private void InitSize ()
		{
			uint ww = 0, wh = 0;
			var ini = Bridge.InitFileStore.Config;
			var setsect = ini ["Settings"];
			var savepos = setsect.GetKey ("PackageReader:SavePosAndSizeBeforeCancel");
			var lastw = setsect.GetKey ("PackageReader:LastWidth");
			var lasth = setsect.GetKey ("PackageReader:LastHeight");
			var defw = setsect.GetKey ("PackageReader:DefaultWidth");
			var defh = setsect.GetKey ("PackageReader:DefaultHeight");
			var minw = setsect.GetKey ("PackageReader:MinimumWidth");
			var minh = setsect.GetKey ("PackageReader:MinimumHeight");
			var lasts = setsect.GetKey ("PackageReader:LastWndState");
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
		private void Form_Load (object sender, EventArgs e)
		{
			var current = Process.GetCurrentProcess ();
			var processes = Process.GetProcessesByName (current.ProcessName);
			int count = processes.Length;
			int offset = 30; // 每个窗口偏移
			int x = 20 + (count - 1) * offset;
			int y = 20 + (count - 1) * offset;
			this.StartPosition = FormStartPosition.Manual;
			this.Location = new Point (x, y);
		}
		private void ManagerShell_Load (object sender, EventArgs e)
		{
			var root = Path.GetDirectoryName (DataUtils.Utilities.GetCurrentProgramPath ());
			WebUI.Navigate (Path.Combine (root, "html\\reader.html"));
		}
		private void ManagerShell_Resize (object sender, EventArgs e)
		{
			var ini = Bridge.InitFileStore.Config;
			var setsect = ini ["Settings"];
			var savepos = setsect.GetKey ("PackageReader:SavePosAndSizeBeforeCancel");
			var lastw = setsect.GetKey ("PackageReader:LastWidth");
			var lasth = setsect.GetKey ("PackageReader:LastHeight");
			var lasts = setsect.GetKey ("PackageReader:LastWndState");
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

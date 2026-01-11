using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DataUtils
{
	public class _I_UI_Size
	{
		private int m_width;
		private int m_height;
		public _I_UI_Size (int w, int h)
		{
			m_width = w;
			m_height = h;
		}
		public int Width => m_width;
		public int Height => m_height;
		public int GetWidth () => m_width;
		public int GetHeight () => m_height;
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_UI
	{
		private Form wndInst;
		public _I_UI (Form wnd)
		{
			wndInst = wnd;
		}
		public int DPIPercent => UITheme.GetDPI ();
		public double DPI => DPIPercent * 0.01;
		public _I_UI_Size WndSize => new _I_UI_Size (wndInst.Width, wndInst.Height);
		public _I_UI_Size ClientSize
		{
			get
			{
				var cs = wndInst.ClientSize;
				return new _I_UI_Size (cs.Width, cs.Height);
			}
		}
		public string ThemeColor => UITheme.ColorToHtml (UITheme.GetDwmThemeColor ());
		public bool DarkMode => UITheme.IsAppInDarkMode ();
		public string HighContrast
		{
			get
			{
				switch (UITheme.GetHighContrastTheme ())
				{
					case HighContrastTheme.None: return "none";
					case HighContrastTheme.Black: return "black";
					case HighContrastTheme.White: return "white";
					case HighContrastTheme.Other: return "high";
					default: return "none";
				}
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Window
	{
		private IScriptBridge iscrp;
		public _I_Window (IScriptBridge _iscrp)
		{
			iscrp = _iscrp;
		}
		public object CallEvent (string name, params object [] args)
		{
			if (iscrp == null) return null;
			object arg0 = (args != null && args.Length > 0) ? args [0] : null;
			return iscrp.CallEvent (name, arg0);
		}
	}
}

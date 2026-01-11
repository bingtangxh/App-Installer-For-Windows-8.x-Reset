using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DataUtils;
namespace WAShell
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public partial class WebAppForm: Form, IScriptBridge, IWebBrowserPageScale
	{
		SplashForm splash;
		ITaskbarList3 taskbar = null;
		public WebAppForm ()
		{
			InitializeComponent ();
		}
		public int PageScale
		{
			get
			{
				var web2 = WebBrowserHelper.GetWebBrowser2 (webui);
				if (web2 == null) return 0;
				object inArg = null;
				object outArg = null;
				try
				{
					web2.ExecWB (
						OLECMDID.OLECMDID_OPTICAL_ZOOM,
						OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
						ref inArg,
						ref outArg
					);
					if (outArg is int) return (int)outArg;
				}
				catch { }
				return 0;
			}
			set
			{
				var web2 = WebBrowserHelper.GetWebBrowser2 (webui);
				if (web2 == null) return;
				object inArg = value;
				object outArg = null;
				try
				{
					web2.ExecWB (
						OLECMDID.OLECMDID_OPTICAL_ZOOM,
						OLECMDEXECOPT.OLECMDEXECOPT_DONTPROMPTUSER,
						ref inArg,
						ref outArg
					);
				}
				catch { }
			}
		}
		public object CallEvent (string funcName, object e)
		{
			return null;
		}
	}
}

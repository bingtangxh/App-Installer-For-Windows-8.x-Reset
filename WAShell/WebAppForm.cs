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
	public partial class WebAppForm: NormalForm, IScriptBridge, IWebBrowserPageScale, ITaskbarProgress
	{
		SplashForm splash = new SplashForm ();
		TaskbarProgress taskbar = null;
		public WebBrowser WebUI => webui;
		public SplashForm SplashScreen => splash;
		public object PublicObjectForScripting { get { return webui?.ObjectForScripting; } set { webui.ObjectForScripting = null; webui.ObjectForScripting = value; } }
		protected Bridge._I_BridgeBase NowObject { get { return webui?.ObjectForScripting as Bridge._I_BridgeBase; } }
		public WebAppForm ()
		{
			splash.Host = this;
			InitializeComponent ();
			webui.ObjectForScripting = new Bridge._I_BridgeBase (this, this, this, this);
			taskbar = new TaskbarProgress (Handle);
		}
		public virtual int PageScale
		{
			get { return IEHelper.WebBrowserHelper.GetPageScale (webui); }
			set { IEHelper.WebBrowserHelper.SetPageScale (webui, value); }
		}
		public double ProgressValue
		{
			set
			{
				if (taskbar == null) return;
				double total = 1000000;
				taskbar.SetValue ((ulong)(value * total), (ulong)total);
			}
		}
		public TBPFLAG ProgressStatus { set { taskbar.SetState (value); } }
		public virtual object CallEvent (string funcName, object e)
		{
			return null;
		}
		private void WebAppForm_Load (object sender, EventArgs e)
		{
			// splash.SplashBackgroundColor = Color.Green;
			splash.ResizeSplashScreen ();
			splash.Show ();
			splash.Update ();
			// splash.FadeOut ();
		}
		private bool issetdpi = false;
		protected virtual void webui_DocumentCompleted (object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			if (!issetdpi)
			{
				issetdpi = true;
				ExecScript ("Bridge.Frame.scale = Bridge.Frame.scale * Bridge.UI.dpi");
			}
			ExecScript ("Windows.UI.DPI.mode = 1");
			if (e.Url.ToString () == webui.Url.ToString ())
			{
				splash.FadeOut ();
			}
		}
		protected object CallScriptFunction (string funcName, params object [] args) { return webui.Document.InvokeScript (funcName, args); }
		protected object CallScriptFunction (string funcName) { return webui.Document.InvokeScript (funcName); }
		public object InvokeCallScript (string funcName, params object [] args)
		{
			if (this.InvokeRequired)
			{
				return this.Invoke (
					new Func<string, object [], object> (CallScriptFunction),
					funcName, args
				);
			}
			else return CallScriptFunction (funcName, args);
		}
		public object InvokeCallScript (string funcName)
		{
			if (this.InvokeRequired)
			{
				return this.Invoke (
					new Func<string, object> (CallScriptFunction),
					funcName
				);
			}
			else return CallScriptFunction (funcName);
		}
		public object ExecScript (params object [] cmdline) { return InvokeCallScript ("eval", cmdline); }
		private void WebAppForm_FormClosing (object sender, FormClosingEventArgs e)
		{
			webui.ObjectForScripting = null;
		}
		private void webui_PreviewKeyDown (object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.F5) e.IsInputKey = true;
		}
	}
}

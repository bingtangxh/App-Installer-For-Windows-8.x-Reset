using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Manager
{
	static class Program
	{
		static public readonly string g_appUserId = "WindowsModern.PracticalToolsProject!Manager";
		static public readonly string g_appId = "Manager";
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main ()
		{
			AppxPackage.PackageReader.AddApplicationItem ("SmallLogo");
			DataUtils.BrowserEmulation.SetWebBrowserEmulation ();
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			Application.Run (new ManagerShell ());
		}
	}
}

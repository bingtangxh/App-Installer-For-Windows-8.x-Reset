using System;
using System.IO;
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
			Directory.SetCurrentDirectory (AppDomain.CurrentDomain.BaseDirectory);
			//System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo ("en-US");
			//System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo ("en-US");
			AppxPackage.PackageReader.AddApplicationItem ("SmallLogo");
			AppxPackage.PackageReader.AddApplicationItem ("Square30x30Logo");
			AppxPackage.PackageReader.AddApplicationItem ("Logo");
			AppxPackage.PackageReader.AddApplicationItem ("Square44x44Logo");
			DataUtils.BrowserEmulation.SetWebBrowserEmulation ();
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			Application.Run (new ManagerShell ());
		}
	}
}

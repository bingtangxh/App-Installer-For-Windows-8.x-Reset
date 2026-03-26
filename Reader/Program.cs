using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reader
{
	static class Program
	{
		static public readonly string g_appUserId = "WindowsModern.PracticalToolsProject!Reader";
		static public readonly string g_appId = "Reader";
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main ()
		{
			Directory.SetCurrentDirectory (AppDomain.CurrentDomain.BaseDirectory);
			AppxPackage.PackageReader.AddApplicationItem ("SmallLogo");
			AppxPackage.PackageReader.AddApplicationItem ("Square30x30Logo");
			AppxPackage.PackageReader.AddApplicationItem ("Logo");
			AppxPackage.PackageReader.AddApplicationItem ("Square44x44Logo");
			DataUtils.BrowserEmulation.SetWebBrowserEmulation ();
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			Application.Run (new ReaderShell ());
		}
	}
}

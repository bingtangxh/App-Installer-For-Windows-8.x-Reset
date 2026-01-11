using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace DataUtils
{
	public static class BrowserEmulation
	{
		// The emulation value used in original C++ (11001).
		// 11001 typically corresponds to IE11 in edge mode for most hosts.
		private const int EmulationValue = 11001;
		/// <summary>
		/// Set the browser emulation value for the current user and current executable.
		/// Writes to HKCU\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION.
		/// Creates the key if it does not exist.
		/// </summary>
		public static void SetWebBrowserEmulation ()
		{
			try
			{
				string exeName = GetCurrentProcessFileName ();
				if (string.IsNullOrEmpty (exeName)) return;

				// Registry path under HKCU (per-user setting)
				const string subKey = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

				using (RegistryKey key = Registry.CurrentUser.CreateSubKey (subKey, RegistryKeyPermissionCheck.ReadWriteSubTree))
				{
					if (key == null) return;
					// set DWORD value
					key.SetValue (exeName, EmulationValue, RegistryValueKind.DWord);
				}
			}
			catch
			{
				// ignore exceptions to match original "best-effort" behavior
			}
		}
		/// <summary>
		/// 获取系统安装的 Internet Explorer 主版本号（如 8、9、10、11）。
		/// 会尝试从 64-bit registry view 和 32-bit registry view 读取（HKLM\SOFTWARE\Microsoft\Internet Explorer）。
		/// 返回 0 表示未能读取到版本信息。
		/// </summary>
		public static int GetInternetExplorerVersionMajor ()
		{
			const string ieKey = @"SOFTWARE\Microsoft\Internet Explorer";
			string versionStr = null;

			// Try RegistryView.Registry64 then Registry32 for robustness on WOW64 systems
			foreach (RegistryView view in new [] { RegistryView.Registry64, RegistryView.Registry32 })
			{
				try
				{
					using (RegistryKey baseKey = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine, view))
					using (RegistryKey key = baseKey.OpenSubKey (ieKey))
					{
						if (key == null) continue;
						object svcVersion = key.GetValue ("svcVersion");
						if (svcVersion is string)
						{
							versionStr = (string)svcVersion;
						}
						else
						{
							object ver = key.GetValue ("Version");
							if (ver is string) versionStr = (string)ver;
						}
					}
				}
				catch
				{
					// ignore and continue to next view
				}
				if (!string.IsNullOrEmpty (versionStr)) break;
			}

			if (string.IsNullOrEmpty (versionStr)) return 0;

			// parse major number before first non-digit/dot
			int major = 0;
			try
			{
				// common version formats: "11.0.9600.16428" etc.
				string [] parts = versionStr.Split (new char [] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length > 0 && int.TryParse (parts [0], out major))
				{
					return major;
				}
				// fallback: try to read first integer found
				string num = "";
				foreach (char c in versionStr)
				{
					if (char.IsDigit (c)) num += c;
					else break;
				}
				if (!string.IsNullOrEmpty (num) && int.TryParse (num, out major)) return major;
			}
			catch
			{
				// ignore parsing errors
			}
			return 0;
		}
		public static int IEVersionMajor => GetInternetExplorerVersionMajor ();
		public static bool IsInternetExplorer10 ()
		{
			return GetInternetExplorerVersionMajor () == 10;
		}
		public static bool IE10 => IsInternetExplorer10 ();
		public static bool IsInternetExplorer11AndLater ()
		{
			return GetInternetExplorerVersionMajor () >= 11;
		}
		public static bool IE11 => IsInternetExplorer11AndLater ();
		// Helper: get file name of current process executable, e.g. "myapp.exe"
		private static string GetCurrentProcessFileName ()
		{
			try
			{
				string path = null;
				try
				{
					// prefer process main module (may throw in some restricted environments)
					path = Process.GetCurrentProcess ().MainModule.FileName;
				}
				catch
				{
					try
					{
						var asm = System.Reflection.Assembly.GetEntryAssembly ();
						if (asm != null) path = asm.Location;
					}
					catch { }
				}
				if (string.IsNullOrEmpty (path))
				{
					path = AppDomain.CurrentDomain.FriendlyName;
				}
				if (string.IsNullOrEmpty (path)) return string.Empty;
				return Path.GetFileName (path);
			}
			catch
			{
				return string.Empty;
			}
		}
	}
	[ComVisible (true)]
	public interface IScriptBridge
	{
		/// <summary>
		/// 调用程序窗口实例中的事件
		/// </summary>
		/// <param name="funcName">函数名</param>
		/// <param name="e">事件参数</param>
		/// <returns>调用结果</returns>
		object CallEvent (string funcName, object e);
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_IEFrame
	{
		public int Version => BrowserEmulation.IEVersionMajor;
		public string ParseHtmlColor (string color)
		{
			try
			{
				Color dcolor = ColorTranslator.FromHtml (color);
				var obj = new
				{
					r = (ushort)dcolor.R,
					g = (ushort)dcolor.G,
					b = (ushort)dcolor.B,
					a = (ushort)dcolor.A
				};
				return JsonConvert.SerializeObject (obj);
			}
			catch
			{
				return "{}";
			}
		}
		private IWebBrowserPageScale iwbps;
		public _I_IEFrame (IWebBrowserPageScale _iwbps)
		{
			iwbps = _iwbps;
		}
		public int Scale
		{
			get { return iwbps.PageScale; }
			set { iwbps.PageScale = value; }
		}
	}
}

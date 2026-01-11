using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Win32;
using DataUtils;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;

namespace Bridge
{
	public static class InitFileStore
	{
		public static readonly InitConfig Config;
		static InitFileStore ()
		{
			try
			{
				string programRoot = GetProgramRootDirectory ();
				string manifestPath = Path.Combine (programRoot, "config.ini");
				Config = new InitConfig (manifestPath);
			}
			catch
			{
				Config = new InitConfig ();
			}
		}
		public static string GetProgramRootDirectory ()
		{
			try
			{
				// Prefer the directory of the executing assembly
				string codeBase = Assembly.GetExecutingAssembly ().Location;
				if (!string.IsNullOrEmpty (codeBase))
				{
					string dir = Path.GetDirectoryName (codeBase);
					if (!string.IsNullOrEmpty (dir)) return dir;
				}
			}
			catch { }

			try
			{
				return AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
			}
			catch { }

			return Environment.CurrentDirectory;
		}
	}
	public static class ResXmlStore
	{
		public static readonly StringResXmlDoc StringRes;
		static ResXmlStore ()
		{
			try
			{
				string programRoot = InitFileStore.GetProgramRootDirectory ();
				string manifestPath = Path.Combine (programRoot, "locale\\resources.xml");
				StringRes = new StringResXmlDoc (manifestPath);
			}
			catch
			{
				StringRes = new StringResXmlDoc ();
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_InitConfig
	{
		InitConfig Create (string filepath) => new InitConfig (filepath);
		InitConfig GetConfig () => InitFileStore.Config;
		InitConfig Current => InitFileStore.Config;
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_System
	{
		private readonly _I_Resources ires = new _I_Resources ();
		private readonly _I_Locale ilocale = new _I_Locale ();
		private readonly _I_UI ui;
		public _I_Resources Resources { get { return ires; } }
		public _I_Locale Locale { get { return ilocale; } }
		public _I_UI UI => ui;
		// Determines if the OS major version is 10 or greater.
		// Uses RtlGetVersion for a reliable OS version.
		public bool IsWindows10
		{
			get
			{
				try
				{
					OSVERSIONINFOEX info = new OSVERSIONINFOEX ();
					info.dwOSVersionInfoSize = Marshal.SizeOf (typeof (OSVERSIONINFOEX));
					int status = RtlGetVersion (ref info);
					if (status == 0) // STATUS_SUCCESS
					{
						return info.dwMajorVersion >= 10;
					}
				}
				catch
				{
					// fallback below
				}

				// Fallback: Environment.OSVersion (may be unreliable on some systems)
				try
				{
					return Environment.OSVersion.Version.Major >= 10;
				}
				catch
				{
					return false;
				}
			}
		}
		#region Native interop (RtlGetVersion)
		[StructLayout (LayoutKind.Sequential)]
		private struct OSVERSIONINFOEX
		{
			public int dwOSVersionInfoSize;
			public int dwMajorVersion;
			public int dwMinorVersion;
			public int dwBuildNumber;
			public int dwPlatformId;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szCSDVersion;
			public ushort wServicePackMajor;
			public ushort wServicePackMinor;
			public ushort wSuiteMask;
			public byte wProductType;
			public byte wReserved;
		}
		// RtlGetVersion returns NTSTATUS (0 = STATUS_SUCCESS)
		[DllImport ("ntdll.dll", SetLastError = false)]
		private static extern int RtlGetVersion (ref OSVERSIONINFOEX versionInfo);
		#endregion
		public _I_System (Form mainWnd)
		{
			ui = new _I_UI (mainWnd);
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_BridgeBase
	{
		protected readonly _I_String str = new _I_String ();
		protected readonly _I_InitConfig conf = new _I_InitConfig ();
		protected readonly _I_Storage stog = new _I_Storage ();
		protected readonly _I_Window window;
		protected readonly _I_System system;
		protected readonly _I_IEFrame ieframe;
		protected readonly _I_Process proc = new _I_Process ();
		public _I_String String => str;
		public _I_InitConfig Config => conf;
		public _I_Storage Storage => stog;
		public _I_Window Window => window;
		public _I_IEFrame IEFrame => ieframe;
		public _I_VisualElements VisualElements => new _I_VisualElements ();
		public StringResXmlDoc StringResources => ResXmlStore.StringRes;
		public string CmdArgs
		{
			get
			{
				return JsonConvert.SerializeObject (
					Environment.GetCommandLineArgs ()
				);
			}
		}
		public _I_BridgeBase (Form wnd, IScriptBridge isc, IWebBrowserPageScale iwbps)
		{
			window = new _I_Window (isc);
			system = new _I_System (wnd);
			ieframe = new _I_IEFrame (iwbps);
		}
	}

}
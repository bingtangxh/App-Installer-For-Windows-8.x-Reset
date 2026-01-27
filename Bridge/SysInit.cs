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
using AppxPackage;
using ModernNotice;
using System.Threading;

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
		public InitConfig Create (string filepath) => new InitConfig (filepath);
		public InitConfig GetConfig () => InitFileStore.Config;
		public InitConfig Current => InitFileStore.Config;
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
	public class _I_PackageManager
	{
		public enum JsAsyncResultKind
		{
			Success,
			Failed,
			None        // 什么都不回调（极少数情况）
		}
		public sealed class JsAsyncResult
		{
			public JsAsyncResultKind Kind { get; private set; }
			public object Value { get; private set; }
			private JsAsyncResult (JsAsyncResultKind kind, object value)
			{
				Kind = kind;
				Value = value;
			}
			public static JsAsyncResult Success (object value = null)
			{
				return new JsAsyncResult (JsAsyncResultKind.Success, value);
			}
			public static JsAsyncResult Failed (object value = null)
			{
				return new JsAsyncResult (JsAsyncResultKind.Failed, value);
			}
			public static JsAsyncResult None ()
			{
				return new JsAsyncResult (JsAsyncResultKind.None, null);
			}
		}
		internal static class JsAsyncRunner
		{
			public static void Run (
				Func<Action<object>, JsAsyncResult> work,
				object jsSuccess,
				object jsFailed,
				object jsProgress)
			{
				var success = jsSuccess;
				var failed = jsFailed;
				var progress = jsProgress;

				System.Threading.ThreadPool.QueueUserWorkItem (_ =>
				{
					try
					{
						Action<object> reportProgress = p =>
						{
							if (progress != null)
								CallJS (progress, p);
						};
						JsAsyncResult result = work (reportProgress);
						if (result == null) return;
						switch (result.Kind)
						{
							case JsAsyncResultKind.Success:
								CallJS (success, result.Value);
								break;

							case JsAsyncResultKind.Failed:
								CallJS (failed, result.Value);
								break;

							case JsAsyncResultKind.None:
							default:
								break;
						}
					}
					catch (Exception ex)
					{
						// 框架级异常兜底 → failed
						CallJS (jsFailed, ex.Message);
					}
				});
			}
			private static void CallJS (object jsFunc, params object [] args)
			{
				if (jsFunc == null) return;
				try
				{
					object [] invokeArgs = new object [(args?.Length ?? 0) + 1];
					invokeArgs [0] = 1; 
					if (args != null)
						for (int i = 0; i < args.Length; i++)
							invokeArgs [i + 1] = args [i];
					jsFunc.GetType ().InvokeMember (
						"call",
						System.Reflection.BindingFlags.InvokeMethod,
						null,
						jsFunc,
						invokeArgs
					);
				}
				catch
				{
				}
			}
		}
		private string BuildJsonText (object obj)
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject (
				obj,
				Newtonsoft.Json.Formatting.Indented
			);
		}
		public void AddPackage (string path, int options, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.AddPackage (
						path,
						null,
						(DeploymentOptions)options,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void GetPackages (object success, object failed)
		{
			JsAsyncRunner.Run (
				report => {
					var res = PackageManager.GetPackages ();
					var hr = res.Item1;
					var jsstr = "";
					{
						var ret = new
						{
							result = res.Item1,
							list = res.Item2
						};
						jsstr = BuildJsonText (ret);
					}
					if (hr == null) return JsAsyncResult.Failed (jsstr);
					return hr.Succeeded ? JsAsyncResult.Success (jsstr) : JsAsyncResult.Failed (jsstr);
				},
				success,
				failed,
				null
			);
		}
		public void RemovePackage (string packageFullName, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.RemovePackage (
						packageFullName,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void ClearupPackage (string packageName, string userSID, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.ClearupPackage (
						packageName,
						userSID,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void RegisterPackage (string path, int options, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.RegisterPackage (
						path,
						null,
						(DeploymentOptions)options,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void RegisterPackageByFullName (string packageFullName, int options, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.RegisterPackageByFullName (
						packageFullName,
						null,
						(DeploymentOptions)options,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public _I_HResult SetPackageStatus (string packageFullName, int status)
		{
			return PackageManager.SetPackageStatus (packageFullName, (PackageStatus)status);
		}
		public void StagePackage (string path, int options, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.StagePackage (
						path,
						null,
						(DeploymentOptions)options,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void StageUserData (string packageFullName, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.StageUserData (
						packageFullName,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void UpdatePackage (string path, int options, object success, object failed, object progress)
		{
			JsAsyncRunner.Run (
				report => {
					var hr = PackageManager.UpdatePackage (
						path,
						null,
						(DeploymentOptions)options,
						e => report (e)
					);
					if (hr == null) return JsAsyncResult.Failed ("Unknown error");
					return hr.Succeeded ? JsAsyncResult.Success (hr) : JsAsyncResult.Failed (hr);
				},
				success,
				failed,
				progress
			);
		}
		public void FindPackageByIdentity (string packageName, string packagePublisher, object success, object failed)
		{
			JsAsyncRunner.Run (
				report => {
					var res = PackageManager.FindPackage (packageName, packagePublisher);
					var hr = res.Item1;
					var jsstr = "";
					{
						var ret = new
						{
							result = res.Item1,
							list = res.Item2
						};
						jsstr = BuildJsonText (ret);
					}
					if (hr == null) return JsAsyncResult.Failed (jsstr);
					return hr.Succeeded ? JsAsyncResult.Success (jsstr) : JsAsyncResult.Failed (jsstr);
				},
				success,
				failed,
				null
			);
		}
		public void FindPackageByFamilyName (string packageFamilyName, object success, object failed)
		{
			JsAsyncRunner.Run (
				report => {
					var res = PackageManager.FindPackage (packageFamilyName);
					var hr = res.Item1;
					var jsstr = "";
					{
						var ret = new
						{
							result = res.Item1,
							list = res.Item2
						};
						jsstr = BuildJsonText (ret);
					}
					if (hr == null) return JsAsyncResult.Failed (jsstr);
					return hr.Succeeded ? JsAsyncResult.Success (jsstr) : JsAsyncResult.Failed (jsstr);
				},
				success,
				failed,
				null
			);
		}
		public void FindPackageByFullName (string packageFullName, object success, object failed)
		{
			JsAsyncRunner.Run (
				report => {
					var res = PackageManager.FindPackageByFullName (packageFullName);
					var hr = res.Item1;
					var jsstr = "";
					{
						var ret = new
						{
							result = res.Item1,
							list = res.Item2
						};
						jsstr = BuildJsonText (ret);
					}
					if (hr == null) return JsAsyncResult.Failed (jsstr);
					return hr.Succeeded ? JsAsyncResult.Success (jsstr) : JsAsyncResult.Failed (jsstr);
				},
				success,
				failed,
				null
			);
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Package
	{
		private static void CallJS (object jsFunc, params object [] args)
		{
			if (jsFunc == null) return;
			try
			{
				// 这里固定第一个参数为 thisArg（比如 1）
				object [] realArgs = new object [args.Length + 1];
				realArgs [0] = jsFunc;     // thisArg
				Array.Copy (args, 0, realArgs, 1, args.Length);

				jsFunc.GetType ().InvokeMember (
					"call",
					BindingFlags.InvokeMethod,
					null,
					jsFunc,
					realArgs
				);
			}
			catch
			{
				// ignore errors in callback invocation
			}
		}
		public AppxPackage.PackageReader Reader (string packagePath) { return new AppxPackage.PackageReader (packagePath); }
		public _I_PackageManager Manager => new _I_PackageManager ();
		public AppxPackage.ManifestReader Manifest (string manifestPath) { return new AppxPackage.ManifestReader (manifestPath); }
		public AppxPackage.ManifestReader FromInstallLocation (string installLocation) { return Manifest (Path.Combine (installLocation, "AppxManifest.xml")); }
		public void ReadFromPackageAsync (string packagePath, bool enablePri, object successCallback, object failedCallback)
		{
			Thread thread = new Thread (() => {
				try
				{
					using (var reader = Reader (packagePath))
					{
						if (enablePri)
						{
							reader.EnablePri = true;
							reader.UsePri = true;
						}
						if (!reader.IsValid)
						{
							var failObj = new
							{
								status = false,
								message = "Reader invalid",
								jsontext = ""
							};
							string failJson = Newtonsoft.Json.JsonConvert.SerializeObject (failObj);
							if (failedCallback != null) CallJS (failedCallback, failJson);
							return;
						}
						var obj = new
						{
							status = true,
							message = "ok",
							jsontext = reader.BuildJsonText ()   // 你之前写好的函数
						};
						string json = Newtonsoft.Json.JsonConvert.SerializeObject (obj);
						if (successCallback != null) CallJS (successCallback, json);
					}
				}
				catch (Exception ex)
				{
					var errObj = new
					{
						status = false,
						message = ex.Message,
						jsontext = ""
					};
					string errJson = Newtonsoft.Json.JsonConvert.SerializeObject (errObj);
					if (failedCallback != null) CallJS (failedCallback, errJson);
				}
			});
			thread.IsBackground = true;
			thread.SetApartmentState (ApartmentState.MTA);
			thread.Start ();
		}
		public void ReadFromManifestAsync (string manifestPath, bool enablePri, object successCallback, object failedCallback)
		{
			Thread thread = new Thread (() => {
				try
				{
					using (var reader = Manifest (manifestPath))
					{
						if (enablePri)
						{
							reader.EnablePri = true;
							reader.UsePri = true;
						}
						if (!reader.IsValid)
						{
							var failObj = new
							{
								status = false,
								message = "Reader invalid",
								jsontext = ""
							};
							string failJson = Newtonsoft.Json.JsonConvert.SerializeObject (failObj);
							if (failedCallback != null) CallJS (failedCallback, failJson);
							return;
						}
						var obj = new
						{
							status = true,
							message = "ok",
							jsontext = reader.BuildJsonText ()   // 你之前写好的函数
						};
						string json = Newtonsoft.Json.JsonConvert.SerializeObject (obj);
						if (successCallback != null) CallJS (successCallback, json);
					}
				}
				catch (Exception ex)
				{
					var errObj = new
					{
						status = false,
						message = ex.Message,
						jsontext = ""
					};
					string errJson = Newtonsoft.Json.JsonConvert.SerializeObject (errObj);
					if (failedCallback != null) CallJS (failedCallback, errJson);
				}
			});
			thread.IsBackground = true;
			thread.SetApartmentState (ApartmentState.MTA);
			thread.Start ();
		}
		public void ReadFromInstallLocationAsync (string installLocation, bool enablePri, object successCallback, object failedCallback)
		{
			Thread thread = new Thread (() => {
				try
				{
					using (var reader = FromInstallLocation (installLocation))
					{
						if (enablePri)
						{
							reader.EnablePri = true;
							reader.UsePri = true;
						}
						if (!reader.IsValid)
						{
							var failObj = new
							{
								status = false,
								message = "Reader invalid",
								jsontext = ""
							};
							string failJson = Newtonsoft.Json.JsonConvert.SerializeObject (failObj);
							if (failedCallback != null) CallJS (failedCallback, failJson);
							return;
						}
						var obj = new
						{
							status = true,
							message = "ok",
							jsontext = reader.BuildJsonText ()   // 你之前写好的函数
						};
						string json = Newtonsoft.Json.JsonConvert.SerializeObject (obj);
						if (successCallback != null) CallJS (successCallback, json);
					}
				}
				catch (Exception ex)
				{
					var errObj = new
					{
						status = false,
						message = ex.Message,
						jsontext = ""
					};
					string errJson = Newtonsoft.Json.JsonConvert.SerializeObject (errObj);
					if (failedCallback != null) CallJS (failedCallback, errJson);
				}
			});
			thread.IsBackground = true;
			thread.SetApartmentState (ApartmentState.MTA);
			thread.Start ();
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Notice
	{
		public string TemplateXml (string templateName) { return Notice.GetTemplateString (templateName); }
		public string SimpleTemplateXml (string text, string imgPath = null) { return Notice.GetSimpleTemplateString (text, imgPath); }
		public string SimpleTemplateXml2 (string title, string text = null, string imgPath = null) { return Notice.GetSimpleTemplateString2 (title, text, imgPath); }
		public _I_HResult NoticeByXml (string appId, string xmlstr) { return Notice.Create (appId, xmlstr); }
		public _I_HResult NoticeSimply (string appId, string text, string imgPath = null) { return Notice.Create (appId, text, imgPath); }
		public _I_HResult NoticeSimply2 (string appId, string title, string text = null, string imgPath = null) { return Notice.Create (appId, title, text, imgPath); }
		public _I_HResult NoticeSimplyByBase64 (string appId, string text, string imgBase64 = null) { return Notice.CreateWithImgBase64 (appId, text, imgBase64); }
		public _I_HResult NoticeSimply2ByBase64 (string appId, string title, string text = null, string imgBase64 = null) { return Notice.CreateWithImgBase64 (appId, title, text, imgBase64); }
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
		public _I_Package Package => new _I_Package ();
		public _I_Taskbar Taskbar { get; private set; } = null;
		public _I_System System => system;
		public _I_Notice Notice => new _I_Notice ();
		public string CmdArgs
		{
			get
			{
				return JsonConvert.SerializeObject (
					Environment.GetCommandLineArgs ()
				);
			}
		}
		public _I_BridgeBase (Form wnd, IScriptBridge isc, IWebBrowserPageScale iwbps, ITaskbarProgress itp)
		{
			window = new _I_Window (isc);
			system = new _I_System (wnd);
			ieframe = new _I_IEFrame (iwbps);
			Taskbar = new _I_Taskbar (itp);
		}
	}

}
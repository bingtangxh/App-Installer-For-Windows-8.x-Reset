using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Threading;

namespace DataUtils
{
	public static class ProcessHelper
	{
		public static int ExecuteProgram (
		string cmdline,
		string file,
		int wndShowMode,
		bool wait,
		string execDir = "")
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = string.IsNullOrEmpty (file) ? cmdline : file,
					Arguments = string.IsNullOrEmpty (file) ? "" : cmdline,
					WorkingDirectory = string.IsNullOrEmpty (execDir) ? null : execDir,
					UseShellExecute = false,
					CreateNoWindow = false,
					WindowStyle = (ProcessWindowStyle)wndShowMode
				};
				using (var p = Process.Start (psi))
				{
					if (wait && p != null)
						p.WaitForExit ();
				}
				return 0;
			}
			catch (Exception ex)
			{
				return Marshal.GetHRForException (ex);
			}
		}
		public static bool KillProcessByFilePath (
			string filepath,
			bool multiple = false,
			bool isOnlyName = false)
		{
			if (string.IsNullOrEmpty (filepath)) return false;
			bool killed = false;
			string targetName = isOnlyName ? Path.GetFileName (filepath) : null;
			foreach (var p in Process.GetProcesses ())
			{
				try
				{
					bool match = false;

					if (isOnlyName)
					{
						match = string.Equals (p.ProcessName + ".exe", targetName,
							StringComparison.OrdinalIgnoreCase);
					}
					else
					{
						string fullPath = p.MainModule.FileName;
						match = string.Equals (fullPath, filepath,
							StringComparison.OrdinalIgnoreCase);
					}
					if (match)
					{
						p.Kill ();
						killed = true;
						if (!multiple) break;
					}
				}
				catch { }
			}
			return killed;
		}
		public static string GetFileVersionAsJson (string filePath)
		{
			try
			{
				var info = FileVersionInfo.GetVersionInfo (filePath);
				var obj = new
				{
					info.CompanyName,
					info.FileDescription,
					info.FileVersion,
					info.InternalName,
					info.OriginalFilename,
					info.ProductName,
					info.ProductVersion,
					info.LegalCopyright,
					FileVersionRaw = info.FileMajorPart + "." +
									 info.FileMinorPart + "." +
									 info.FileBuildPart + "." +
									 info.FilePrivatePart
				};
				return JsonConvert.SerializeObject (obj);
			}
			catch (Exception ex)
			{
				return JsonConvert.SerializeObject (new { error = ex.Message });
			}
		}
		public static int ExploreSaveFile (
			IWin32Window owner,
			List<string> results,
			string filter = "Windows Store App Package (*.appx; *.appxbundle)|*.appx;*.appxbundle",
			string defExt = "appx",
			string title = "Please select the file to save:",
			string initDir = null)
		{
			results.Clear ();
			using (var dlg = new SaveFileDialog ())
			{
				dlg.Filter = filter;
				dlg.DefaultExt = defExt;
				dlg.Title = title;
				dlg.InitialDirectory = initDir;
				if (dlg.ShowDialog (owner) == DialogResult.OK)
				{
					results.Add (dlg.FileName);
				}
			}
			return results.Count;
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Process
	{
		public int Run (
			string cmdline,
			string filepath,
			int wndtype,
			bool wait,
			string runpath)
		{
			return ProcessHelper.ExecuteProgram (cmdline, filepath, wndtype, wait, runpath);
		}
		public void RunAsync (
			string cmdline,
			string filepath,
			int wndtype,
			string runpath,
			object callback)
		{
			var worker = new ProcessWorker (this)
			{
				CmdLine = cmdline,
				FilePath = filepath,
				WndType = wndtype,
				RunPath = runpath,
				Callback = callback
			};
			var th = new Thread (worker.Work)
			{
				IsBackground = true
			};
			th.Start ();
		}
		public bool Kill (string filename, bool allproc, bool onlyname)
		{
			return ProcessHelper.KillProcessByFilePath (filename, allproc, onlyname);
		}
		private class ProcessWorker
		{
			private readonly _I_Process parent;
			public string CmdLine;
			public string FilePath;
			public int WndType;
			public string RunPath;
			public object Callback;
			public ProcessWorker (_I_Process parent)
			{
				this.parent = parent;
			}
			public void Work ()
			{
				int ret = parent.Run (CmdLine, FilePath, WndType, true, RunPath);

				if (Callback != null)
				{
					try
					{
						Callback.GetType ().InvokeMember (
							"call",
							System.Reflection.BindingFlags.InvokeMethod,
							null,
							Callback,
							new object [] { 1, ret }
						);
					}
					catch { }
				}
			}
		}
	}
}

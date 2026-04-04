using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace DataUtils
{
	public static class Utilities
	{
		// Format string with args (compatible with your C++ FormatString wrapper)
		public static string FormatString (string fmt, params object [] args)
		{
			if (fmt == null) return string.Empty;
			if (args == null || args.Length == 0) return fmt;
			try
			{
				return string.Format (System.Globalization.CultureInfo.InvariantCulture, fmt, args);
			}
			catch
			{
				// On format error, fallback to simple concat
				try
				{
					StringBuilder sb = new StringBuilder ();
					sb.Append (fmt);
					sb.Append (" : ");
					for (int i = 0; i < args.Length; i++)
					{
						if (i > 0) sb.Append (", ");
						sb.Append (args [i] != null ? args [i].ToString () : "null");
					}
					return sb.ToString ();
				}
				catch
				{
					return fmt;
				}
			}
		}

		// Escape string to be used as InnerXml content (returns XML-escaped content)
		public static string EscapeToInnerXml (string str)
		{
			if (str == null) return string.Empty;
			var doc = new XmlDocument ();
			// create a root element and use InnerText to perform escaping
			var root = doc.CreateElement ("body");
			root.InnerText = str;
			return root.InnerXml;
		}

		// Returns the current process full path (exe)
		public static string GetCurrentProgramPath ()
		{
			try
			{
				return System.Diagnostics.Process.GetCurrentProcess ().MainModule.FileName;
			}
			catch
			{
				try
				{
					return System.Reflection.Assembly.GetEntryAssembly ().Location;
				}
				catch
				{
					return AppDomain.CurrentDomain.BaseDirectory;
				}
			}
		}

		// JSON array builder using Newtonsoft.Json
		public static string StringArrayToJson (string [] values)
		{
			if (values == null) return "[]";
			try
			{
				return JsonConvert.SerializeObject (values);
			}
			catch
			{
				// Fallback to manual builder
				StringBuilder sb = new StringBuilder ();
				sb.Append ('[');
				for (int i = 0; i < values.Length; i++)
				{
					if (i > 0) sb.Append (',');
					sb.Append ('"');
					sb.Append (JsonEscape (values [i] ?? string.Empty));
					sb.Append ('"');
				}
				sb.Append (']');
				return sb.ToString ();
			}
		}

		public static string StringListToJson (System.Collections.Generic.List<string> list)
		{
			if (list == null) return "[]";
			return StringArrayToJson (list.ToArray ());
		}

		// Minimal JSON string escaper (fallback)
		private static string JsonEscape (string s)
		{
			if (string.IsNullOrEmpty (s)) return s ?? string.Empty;
			StringBuilder sb = new StringBuilder (s.Length + 8);
			foreach (char c in s)
			{
				switch (c)
				{
					case '"': sb.Append ("\\\""); break;
					case '\\': sb.Append ("\\\\"); break;
					case '\b': sb.Append ("\\b"); break;
					case '\f': sb.Append ("\\f"); break;
					case '\n': sb.Append ("\\n"); break;
					case '\r': sb.Append ("\\r"); break;
					case '\t': sb.Append ("\\t"); break;
					default:
						if (c < 32 || c == '\u2028' || c == '\u2029')
						{
							sb.AppendFormat ("\\u{0:X4}", (int)c);
						}
						else
						{
							sb.Append (c);
						}
						break;
				}
			}
			return sb.ToString ();
		}

		// Helper: combine multiple filters split by ';' or '\' (legacy)
		public static string [] SplitFilters (string filter)
		{
			if (string.IsNullOrEmpty (filter)) return new string [] { "*" };
			// Accept ';' or '\' or '|' as separators (common)
			string [] parts = filter.Split (new char [] { ';', '\\', '|' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				parts [i] = parts [i].Trim ();
				if (parts [i].Length == 0) parts [i] = "*";
			}
			if (parts.Length == 0) return new string [] { "*" };
			return parts;
		}

		// Normalize full path for comparisons
		public static string NormalizeFullPath (string path)
		{
			if (string.IsNullOrEmpty (path)) return string.Empty;
			try
			{
				return Path.GetFullPath (path).TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			}
			catch
			{
				return path.Trim ();
			}
		}
		// 忽略大小写和首尾空的比较
		public static bool NEquals (this string left, string right)
		{
			return (left ?? "")?.Trim ()?.ToLower ()?.Equals ((right ?? "")?.Trim ()?.ToLower ()) ?? false;
		}
		public static int NCompareTo (this string l, string r)
		{
			return (l ?? "")?.Trim ()?.ToLower ().CompareTo ((r ?? "")?.Trim ()?.ToLower ()) ?? 0;
		}
		public static bool NEmpty (this string l)
		{
			return (l ?? "")?.NEquals ("") ?? true;
		}
		public static int NLength (this string l)
		{
			return (l ?? "")?.Length ?? 0;
		}
		public static bool NNoEmpty (this string l) => !((l ?? "")?.NEmpty () ?? true);
		public static string NNormalize (this string l) => (l ?? "")?.Trim ()?.ToLower () ?? "";
		public interface IJsonBuild
		{
			object BuildJSON ();
		}
	}
	public static class JsUtils
	{
		/// <summary>
		/// 调用 JS 函数。第一个参数作为 this，其余作为 JS 函数参数。
		/// </summary>
		/// <param name="callback">JS 函数对象</param>
		/// <param name="args">JS 函数参数，可选</param>
		/// <returns>JS 函数返回值</returns>
		public static void Call (object jsFunc, params object [] args)
		{
			if (jsFunc == null) return;
			object [] invokeArgs = new object [(args?.Length ?? 0) + 1];
			invokeArgs [0] = jsFunc; // this
			if (args != null)
				for (int i = 0; i < args.Length; i++)
					invokeArgs [i + 1] = args [i];
			jsFunc.GetType ().InvokeMember (
				"call",
				BindingFlags.InvokeMethod,
				null,
				jsFunc,
				invokeArgs);
		}
	}
	[ComVisible (true)]
	[InterfaceType (ComInterfaceType.InterfaceIsDual)]
	public interface _I_IAsyncAction
	{
		_I_IAsyncAction Then (object resolve, object reject = null, object progress = null);
		void Done (object resolve, object reject = null);
		void Catch (object reject);
		bool IsCompleted { get; }
		bool IsCancelled { get; }
		bool IsError { get; }
		void Cancel ();
		void ReportProgress (object progress);
		object Error { get; }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Task: _I_IAsyncAction
	{
		private object _result;
		private Exception _error;
		private bool _completed;
		private bool _cancelled;
		private List<object> _resolveCallbacks = new List<object> ();
		private List<object> _rejectCallbacks = new List<object> ();
		private List<object> _progressCallbacks = new List<object> ();
		private CancellationTokenSource _cts = new CancellationTokenSource ();
		public _I_Task (Func<object> func)
		{
			ThreadPool.QueueUserWorkItem (_ => {
				try
				{
					if (_cts.Token.IsCancellationRequested)
					{
						_cancelled = true;
						return;
					}

					_result = func ();
					_completed = true;

					foreach (var cb in _resolveCallbacks)
						JsUtils.Call (cb, _result);
				}
				catch (Exception ex)
				{
					_error = ex;
					foreach (var cb in _rejectCallbacks)
						JsUtils.Call (cb, ex);
				}
			});
		}
		public _I_IAsyncAction Then (object resolve, object reject = null, object progress = null)
		{
			if (resolve != null) _resolveCallbacks.Add (resolve);
			if (reject != null) _rejectCallbacks.Add (reject);
			if (progress != null) _progressCallbacks.Add (progress);
			return this;
		}
		public void Done (object resolve, object reject = null)
		{
			if (resolve != null) _resolveCallbacks.Add (resolve);
			if (reject != null) _rejectCallbacks.Add (reject);
		}

		public void Catch (object reject)
		{
			if (reject != null) _rejectCallbacks.Add (reject);
		}

		public bool IsCompleted => _completed;
		public bool IsCancelled => _cancelled;
		public bool IsError => _error != null;

		public object Error => _error;

		public void Cancel ()
		{
			_cts.Cancel ();
			_cancelled = true;
		}

		public void ReportProgress (object progress)
		{
			foreach (var cb in _progressCallbacks)
				JsUtils.Call (cb, progress);
		}

		public object Result => _result;
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Thread: _I_IAsyncAction
	{
		private Thread _thread;
		private bool _completed;
		private bool _cancelled;
		private Exception _error;
		private List<object> _resolveCallbacks = new List<object> ();
		private List<object> _rejectCallbacks = new List<object> ();
		private List<object> _progressCallbacks = new List<object> ();
		private CancellationTokenSource _cts = new CancellationTokenSource ();

		public _I_Thread (Action action)
		{
			_thread = new Thread (() => {
				try
				{
					if (_cts.Token.IsCancellationRequested)
					{
						_cancelled = true;
						return;
					}

					action ();
					_completed = true;

					foreach (var cb in _resolveCallbacks)
						JsUtils.Call (cb);
				}
				catch (Exception ex)
				{
					_error = ex;
					foreach (var cb in _rejectCallbacks)
						JsUtils.Call (cb, ex);
				}
			});
			_thread.IsBackground = true;
			_thread.Start ();
		}

		public _I_IAsyncAction Then (object resolve, object reject = null, object progress = null)
		{
			if (resolve != null) _resolveCallbacks.Add (resolve);
			if (reject != null) _rejectCallbacks.Add (reject);
			if (progress != null) _progressCallbacks.Add (progress);
			return this;
		}

		public void Done (object resolve, object reject = null)
		{
			if (resolve != null) _resolveCallbacks.Add (resolve);
			if (reject != null) _rejectCallbacks.Add (reject);
		}

		public void Catch (object reject)
		{
			if (reject != null) _rejectCallbacks.Add (reject);
		}

		public bool IsCompleted => _completed;
		public bool IsCancelled => _cancelled;
		public bool IsError => _error != null;
		public object Error => _error;

		public void Cancel ()
		{
			_cts.Cancel ();
			_cancelled = true;
		}

		public void ReportProgress (object progress)
		{
			foreach (var cb in _progressCallbacks)
				JsUtils.Call (cb, progress);
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Exception: IExcep
}

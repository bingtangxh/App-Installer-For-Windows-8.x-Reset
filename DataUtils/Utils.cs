using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
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
	public class _I_Exception: Exception, IDisposable
	{
		private Exception bex = null;
		public _I_Exception (Exception ex) { bex = ex; }
		public _I_Exception (string message) { bex = new Exception (message); }
		public _I_Exception (string msg, Exception innerEx) { bex = new Exception (msg, innerEx); }
		public override IDictionary Data => bex.Data;
		public override Exception GetBaseException () => bex.GetBaseException ();
		public override void GetObjectData (SerializationInfo info, StreamingContext context) => bex.GetObjectData (info, context);
		public override string HelpLink
		{
			get { return bex.HelpLink; }
			set { bex.HelpLink = value; }
		}
		public override string Message => bex.Message;
		public override string Source
		{
			get { return bex.Source; }
			set { bex.Source = value; }
		}
		public override string StackTrace => bex.StackTrace;
		public override string ToString () => bex.ToString ();
		public override int GetHashCode () => bex.GetHashCode ();
		public override bool Equals (object obj) => bex.Equals (obj);
		public void Dispose () { bex = null; }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_KeyValuePair: IDisposable
	{
		object key = null;
		object value = null;
		public _I_KeyValuePair (object k, object v)
		{
			key = k;
			value = v;
		}
		public object Key { get { return key; } set { key = value; } }
		public object Value { get { return value; } set { this.value = value; } }
		public void Dispose ()
		{
			key = null;
			value = null;
		}
		~_I_KeyValuePair ()
		{
			key = null;
			value = null;
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_WwwFormUrlDecoder
	{
		private readonly Dictionary<string, string> _params = new Dictionary<string, string> ();
		public _I_WwwFormUrlDecoder (string query)
		{
			if (string.IsNullOrEmpty (query)) return;
			if (query.StartsWith ("?")) query = query.Substring (1);
			foreach (var pair in query.Split ('&'))
			{
				var kv = pair.Split ('=');
				if (kv.Length == 2)
					_params [Uri.UnescapeDataString (kv [0])] = Uri.UnescapeDataString (kv [1]);
			}
		}
		public string GetFirstValueByName (string name)
		{
			string value = null;
			return _params.TryGetValue (name, out value) ? value : null;
		}
		public int Size => _params.Count;
		public _I_KeyValuePair GetAt (uint index)
		{
			var pair = _params.ElementAt ((int)index);
			return new _I_KeyValuePair (pair.Key, pair.Value);
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Uri: Uri
	{
		public _I_Uri (string uri): base (uri) { }
		public _I_Uri (string baseUri, string relativeUri) : base (new Uri (baseUri), relativeUri) { }
		public string AbsoluteCanonicalUri => this.GetLeftPart (UriPartial.Authority) + this.PathAndQuery + this.Fragment;
		public string DisplayIri => Uri.UnescapeDataString (this.OriginalString);
		public string DisplayUri => Uri.UnescapeDataString (this.OriginalString);
		public string Domain => ExtractDomain (this.Host);
		public string Extension => System.IO.Path.GetExtension (this.AbsolutePath)?.TrimStart ('.') ?? "";
		public string Password => ExtractPassword (this.UserInfo);
		public string Path => this.AbsolutePath;
		public object QueryParsed => new _I_WwwFormUrlDecoder (this.Query);
		public string RawUri => this.OriginalString;
		public string SchemeName => this.Scheme;
		public bool Suspicious => !Uri.IsWellFormedUriString (this.OriginalString, UriKind.Absolute);
		public string UserName => ExtractUserName (this.UserInfo);
		public _I_Uri CombineUri (string relativeUri)
		{
			return new _I_Uri (this.AbsoluteUri, relativeUri);
		}
		public static string EscapeComponent (string component)
		{
			return Uri.EscapeDataString (component);
		}
		public static string UnescapeComponent (string component)
		{
			return Uri.UnescapeDataString (component);
		}
		private static string ExtractDomain (string host)
		{
			var parts = host.Split ('.');
			if (parts.Length >= 2)
				return string.Join (".", parts.Skip (1));
			return host;
		}
		private static string ExtractUserName (string userInfo)
		{
			if (string.IsNullOrEmpty (userInfo)) return "";
			var parts = userInfo.Split (':');
			return parts [0];
		}
		private static string ExtractPassword (string userInfo)
		{
			if (string.IsNullOrEmpty (userInfo)) return "";
			var parts = userInfo.Split (':');
			return parts.Length > 1 ? parts [1] : "";
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Utilities
	{
		public _I_Uri CreateUri (string uri) => new _I_Uri (uri);
		public _I_Uri CreateUri2 (string baseUri, string relaUri) => new _I_Uri (baseUri, relaUri);
		public _I_Exception CreateException (string message) => new _I_Exception (message);
		public _I_Exception CreateException2 (string message, _I_Exception innerEx) => new _I_Exception (message, innerEx);
		public _I_Calendar CreateCalendar () => new _I_Calendar ();
		public _I_Calendar CreateCalendar2 (object list) => new _I_Calendar (list);
		public _I_Calendar CreateCalendar3 (object list, string arg1, string arg2) => new _I_Calendar (list, arg1, arg2);
		public _I_Calendar CreateCalendar4 (object list, string arg1, string arg2, string arg3) => new _I_Calendar (list, arg1, arg2, arg3);
		public _I_DateTimeFormatter CreateDateTimeFormatterFromTemplate (string formatTemplate)
		{
			return new _I_DateTimeFormatter (formatTemplate);
		}
		public _I_DateTimeFormatter CreateDateTimeFormatterFromTemplateAndLanguages (string formatTemplate, object languagesArray)
		{
			List<string> languages = JsArrayToStringList (languagesArray);
			return new _I_DateTimeFormatter (formatTemplate, languages);
		}
		public _I_DateTimeFormatter CreateDateTimeFormatterFromTemplateFull (string formatTemplate, object languagesArray,
																			string geographicRegion, string calendar, string clock)
		{
			List<string> languages = JsArrayToStringList (languagesArray);
			return new _I_DateTimeFormatter (formatTemplate, languages, geographicRegion, calendar, clock);
		}
		public _I_DateTimeFormatter CreateDateTimeFormatterFromDateEnums (int year, int month, int day, int dayOfWeek)
		{
			return new _I_DateTimeFormatter (
				(YearFormat)year,
				(MonthFormat)month,
				(DayFormat)day,
				(DayOfWeekFormat)dayOfWeek
			);
		}
		public _I_DateTimeFormatter CreateDateTimeFormatterFromTimeEnums (int hour, int minute, int second)
		{
			return new _I_DateTimeFormatter (
				(HourFormat)hour,
				(MinuteFormat)minute,
				(SecondFormat)second
			);
		}
		public _I_DateTimeFormatter CreateDateTimeFormatterFromDateTimeEnums (int year, int month, int day, int dayOfWeek,
																			int hour, int minute, int second, object languagesArray)
		{
			List<string> languages = JsArrayToStringList (languagesArray);
			return new _I_DateTimeFormatter (
				(YearFormat)year,
				(MonthFormat)month,
				(DayFormat)day,
				(DayOfWeekFormat)dayOfWeek,
				(HourFormat)hour,
				(MinuteFormat)minute,
				(SecondFormat)second,
				languages
			);
		}
		public _I_DateTimeFormatter CreateDateTimeFormatterFromDateTimeEnumsFull (int year, int month, int day, int dayOfWeek,
																				int hour, int minute, int second, object languagesArray,
																				string geographicRegion, string calendar, string clock)
		{
			List<string> languages = JsArrayToStringList (languagesArray);
			return new _I_DateTimeFormatter (
				(YearFormat)year,
				(MonthFormat)month,
				(DayFormat)day,
				(DayOfWeekFormat)dayOfWeek,
				(HourFormat)hour,
				(MinuteFormat)minute,
				(SecondFormat)second,
				languages,
				geographicRegion,
				calendar,
				clock
			);
		}
		private List<string> JsArrayToStringList (object jsArray)
		{
			var result = new List<string> ();
			if (jsArray == null) return result;

			Type type = jsArray.GetType ();
			try
			{
				int length = (int)type.InvokeMember ("length", BindingFlags.GetProperty, null, jsArray, null);
				for (int i = 0; i < length; i++)
				{
					object value = type.InvokeMember (i.ToString (), BindingFlags.GetProperty, null, jsArray, null);
					if (value != null)
						result.Add (value.ToString ());
				}
			}
			catch
			{
				// 如果无法获取 length，则假设是单个字符串
				string single = jsArray.ToString ();
				if (!string.IsNullOrEmpty (single))
					result.Add (single);
			}
			return result;
		}
	}
}

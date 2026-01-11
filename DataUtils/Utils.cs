using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
	}
}

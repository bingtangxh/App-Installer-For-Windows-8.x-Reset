using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace InitFileHelper
{
	public static class IniFile
	{
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileStringA (string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileStringW (string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileSectionA (string lpAppName, byte [] lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileSectionW (string lpAppName, char [] lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileSectionNamesA (byte [] lpszReturnBuffer, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileSectionNamesW (char [] lpszReturnBuffer, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileIntA (string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileIntW (string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool WritePrivateProfileStringA (string lpAppName, string lpKeyName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool WritePrivateProfileStringW (string lpAppName, string lpKeyName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool WritePrivateProfileSectionA (string lpAppName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool WritePrivateProfileSectionW (string lpAppName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool GetPrivateProfileStructA (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetPrivateProfileStructW (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool WritePrivateProfileStructA (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool WritePrivateProfileStructW (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);

		public static string GetPrivateProfileStringA (string filePath, string section, string key, string defaultValue = "")
		{
			StringBuilder buf = new StringBuilder (32768);
			GetPrivateProfileStringA (section, key, defaultValue, buf, (uint)buf.Capacity, filePath);
			return buf.ToString ();
		}
		public static string GetPrivateProfileStringW (string filePath, string section, string key, string defaultValue = "")
		{
			StringBuilder buf = new StringBuilder (32768);
			GetPrivateProfileStringW (section, key, defaultValue, buf, (uint)buf.Capacity, filePath);
			return buf.ToString ();
		}
		public static uint GetPrivateProfileIntA (string filePath, string section, string key, int defaultValue = 0)
		{
			return GetPrivateProfileIntA (section, key, defaultValue, filePath);
		}
		public static uint GetPrivateProfileIntW (string filePath, string section, string key, int defaultValue = 0)
		{
			return GetPrivateProfileIntW (section, key, defaultValue, filePath);
		}
		public static bool WritePrivateProfileString (string filePath, string section, string key, string value)
		{
			return WritePrivateProfileStringW (section, key, value, filePath);
		}
		public static int GetPrivateProfileSectionA (string filePath, string section, List<string> output)
		{
			byte [] buf = new byte [32768];
			uint len = GetPrivateProfileSectionA (section, buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != 0) i++;
				if (i > start) output.Add (Encoding.Default.GetString (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static int GetPrivateProfileSectionW (string filePath, string section, List<string> output)
		{
			char [] buf = new char [32768];
			uint len = GetPrivateProfileSectionW (section, buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != '\0') i++;
				if (i > start) output.Add (new string (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static int GetPrivateProfileSectionNamesA (string filePath, List<string> output)
		{
			byte [] buf = new byte [32768];
			uint len = GetPrivateProfileSectionNamesA (buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != 0) i++;
				if (i > start) output.Add (Encoding.Default.GetString (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static int GetPrivateProfileSectionNamesW (string filePath, List<string> output)
		{
			char [] buf = new char [32768];
			uint len = GetPrivateProfileSectionNamesW (buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != '\0') i++;
				if (i > start) output.Add (new string (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static bool WritePrivateProfileSectionA (string filePath, string section, List<string> lines)
		{
			string buf = string.Join ("\0", lines) + "\0\0";
			return WritePrivateProfileSectionA (section, buf, filePath);
		}
		public static bool WritePrivateProfileSectionW (string filePath, string section, List<string> lines)
		{
			string buf = string.Join ("\0", lines) + "\0\0";
			return WritePrivateProfileSectionW (section, buf, filePath);
		}
		public static bool GetPrivateProfileStructA (string filePath, string section, string key, IntPtr output, uint size)
		{
			return GetPrivateProfileStructA (section, key, output, size, filePath);
		}
		public static bool GetPrivateProfileStructW (string filePath, string section, string key, IntPtr output, uint size)
		{
			return GetPrivateProfileStructW (section, key, output, size, filePath);
		}
		public static bool WritePrivateProfileStructA (string filePath, string section, string key, IntPtr data, uint size)
		{
			return WritePrivateProfileStructA (section, key, data, size, filePath);
		}
		public static bool WritePrivateProfileStructW (string filePath, string section, string key, IntPtr data, uint size)
		{
			return WritePrivateProfileStructW (section, key, data, size, filePath);
		}
		public static int GetPrivateProfileKeysA (string filePath, string section, List<string> keys)
		{
			List<string> lines = new List<string> ();
			int count = GetPrivateProfileSectionA (filePath, section, lines);
			keys.Clear ();
			foreach (var line in lines)
			{
				int pos = line.IndexOf ('=');
				if (pos != -1) keys.Add (line.Substring (0, pos));
			}
			return keys.Count;
		}
		public static int GetPrivateProfileKeysW (string filePath, string section, List<string> keys)
		{
			List<string> lines = new List<string> ();
			int count = GetPrivateProfileSectionW (filePath, section, lines);
			keys.Clear ();
			foreach (var line in lines)
			{
				int pos = line.IndexOf ('=');
				if (pos != -1) keys.Add (line.Substring (0, pos));
			}
			return keys.Count;
		}
		public static bool DeletePrivateProfileKeyA (string filePath, string section, string key)
		{
			return WritePrivateProfileStringA (section, key, null, filePath);
		}
		public static bool DeletePrivateProfileKeyW (string filePath, string section, string key)
		{
			return WritePrivateProfileStringW (section, key, null, filePath);
		}
		public static bool DeletePrivateProfileSectionA (string filePath, string section)
		{
			return WritePrivateProfileStringA (section, null, null, filePath);
		}
		public static bool DeletePrivateProfileSectionW (string filePath, string section)
		{
			return WritePrivateProfileStringW (section, null, null, filePath);
		}
	}
	[ComVisible (true)]
	public static class BoolHelper
	{
		public static readonly string [] trueValue =
		{
			"true", "zhen", "yes", "真", "1"
		};
		public static readonly string [] falseValue =
		{
			"false", "jia", "no", "假", "0"
		};
		public static bool ConvertToBool (string str)
		{
			if (str == null) throw new ArgumentNullException (nameof (str));
			str = str.Trim ().ToLowerInvariant ();
			if (trueValue.Any (s => s.ToLowerInvariant () == str)) return true;
			if (falseValue.Any (s => s.ToLowerInvariant () == str)) return false;
			throw new FormatException ($"Cannot convert '{str}' to bool.");
		}
		public static bool TryParseBool (string str, out bool result)
		{
			result = false;
			if (str == null) return false;
			str = str.Trim ().ToLowerInvariant ();
			if (trueValue.Any (s => s.ToLowerInvariant () == str))
			{
				result = true;
				return true;
			}
			if (falseValue.Any (s => s.ToLowerInvariant () == str))
			{
				result = false;
				return true;
			}
			return false;
		}
	}
}

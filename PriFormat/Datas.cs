using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PriFormat
{
	public struct SectionRef<T> where T : Section
	{
		internal int SectionIndex;
		internal SectionRef (int sectionIndex)
		{
			SectionIndex = sectionIndex;
		}
		public override string ToString ()
		{
			return "Section " + typeof (T).Name + " at index " + SectionIndex;
		}
	}
	public static class LocaleExt
	{
		// GetLocaleInfoW for LCID-based queries
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetLocaleInfoW (int Locale, int LCType, [Out] StringBuilder lpLCData, int cchData);
		// GetLocaleInfoEx for locale name based queries
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetLocaleInfoEx (string lpLocaleName, int LCType, [Out] StringBuilder lpLCData, int cchData);
		// LocaleNameToLCID - available on Vista+; fallback is to use CultureInfo
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int LocaleNameToLCID (string lpName, uint dwFlags);
		// LCIDToLocaleName (Vista+)
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int LCIDToLocaleName (int Locale, [Out] StringBuilder lpName, int cchName, uint dwFlags);
		// Current locale name like "en-US"
		public static string CurrentLocale
		{
			get
			{
				try
				{
					// prefer thread culture name which reflects user culture
					string name = CultureInfo.CurrentCulture.Name;
					if (string.IsNullOrEmpty (name))
						name = CultureInfo.InstalledUICulture.Name;
					return name ?? string.Empty;
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		// Current LCID (int)
		public static int CurrentLCID
		{
			get
			{
				try
				{
					return CultureInfo.CurrentCulture.LCID;
				}
				catch
				{
					return CultureInfo.InvariantCulture.LCID;
				}
			}
		}

		// Convert LCID -> locale name (e.g. 1033 -> "en-US")
		public static string ToLocaleName (int lcid)
		{
			try
			{
				// Try managed first
				var ci = new CultureInfo (lcid);
				if (!string.IsNullOrEmpty (ci.Name))
					return ci.Name;
			}
			catch
			{
				// try Win32 LCIDToLocaleName (Vista+)
				try
				{
					StringBuilder sb = new StringBuilder (LOCALE_NAME_MAX_LENGTH);
					int res = LCIDToLocaleName (lcid, sb, sb.Capacity, 0);
					if (res > 0) return sb.ToString ();
				}
				catch { }
			}
			return string.Empty;
		}

		// Convert locale name -> LCID
		public static int ToLCID (string localeName)
		{
			if (string.IsNullOrEmpty (localeName)) return CultureInfo.InvariantCulture.LCID;
			try
			{
				// prefer managed creation
				var ci = new CultureInfo (localeName);
				return ci.LCID;
			}
			catch
			{
				// try Win32 LocaleNameToLCID (Vista+)
				try
				{
					int lcid = LocaleNameToLCID (localeName, 0);
					if (lcid != 0) return lcid;
				}
				catch { }
			}
			// fallback: invariant culture
			return CultureInfo.InvariantCulture.LCID;
		}

		// Return a locale info string for given LCID and LCTYPE. LCTYPE is the Win32 LOCALE_* constant.
		// Returns a string (or empty string on failure).
		public static object LocaleInfo (int lcid, int lctype)
		{
			try
			{
				// First try mapping common LCTYPE values to managed properties for better correctness
				// Some common LCTYPE values:
				// LOCALE_SISO639LANGNAME = 0x59 (89)  -> Two-letter ISO language name
				// LOCALE_SISO3166CTRYNAME = 0x5A (90) -> Two-letter country/region name
				// LOCALE_SNAME = 0x5c (92) -> locale name like "en-US" (Vista+)
				// But we cannot rely on all values, so we fallback to native GetLocaleInfoW.
				if (lctype == 0x59) // LOCALE_SISO639LANGNAME
				{
					try
					{
						var ci = new CultureInfo (lcid);
						return ci.TwoLetterISOLanguageName ?? string.Empty;
					}
					catch { }
				}
				else if (lctype == 0x5A) // LOCALE_SISO3166CTRYNAME
				{
					try
					{
						var ci = new CultureInfo (lcid);
						try
						{
							var ri = new RegionInfo (ci.Name);
							return ri.TwoLetterISORegionName ?? string.Empty;
						}
						catch
						{
							// some cultures have no region; fallback to parsing name
							var name = ci.Name;
							if (!string.IsNullOrEmpty (name) && name.IndexOf ('-') >= 0)
							{
								return name.Split ('-') [1];
							}
						}
					}
					catch { }
				}
				else if (lctype == 0x5c) // LOCALE_SNAME
				{
					try
					{
						var ci = new CultureInfo (lcid);
						return ci.Name ?? string.Empty;
					}
					catch { }
				}

				// Fallback to native GetLocaleInfoW
				StringBuilder sb = new StringBuilder (256);
				int ret = GetLocaleInfoW (lcid, lctype, sb, sb.Capacity);
				if (ret > 0)
					return sb.ToString ();
				return string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		// LocaleInfoEx: query by locale name string and LCTYPE
		// Returns string if available; otherwise returns the integer result code (as int) if string empty (mimic C++ behavior).
		public static object LocaleInfoEx (string localeName, int lctype)
		{
			if (string.IsNullOrEmpty (localeName))
			{
				// fall back to current culture name
				localeName = CurrentLocale;
				if (string.IsNullOrEmpty (localeName)) return 0;
			}

			try
			{
				// Try managed shortcuts for common types
				if (lctype == 0x59) // LOCALE_SISO639LANGNAME
				{
					try
					{
						var ci = new CultureInfo (localeName);
						return ci.TwoLetterISOLanguageName ?? string.Empty;
					}
					catch { }
				}
				else if (lctype == 0x5A) // LOCALE_SISO3166CTRYNAME
				{
					try
					{
						var ci = new CultureInfo (localeName);
						var ri = new RegionInfo (ci.Name);
						return ri.TwoLetterISORegionName ?? string.Empty;
					}
					catch
					{
						// try to split
						var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length >= 2) return parts [1];
					}
				}
				else if (lctype == 0x5c) // LOCALE_SNAME
				{
					// localeName is probably already the name
					return localeName;
				}

				// Fallback to GetLocaleInfoEx
				StringBuilder sb = new StringBuilder (LOCALE_NAME_MAX_LENGTH);
				int res = GetLocaleInfoEx (localeName, lctype, sb, sb.Capacity);
				if (res > 0)
				{
					string outStr = sb.ToString ();
					if (!string.IsNullOrEmpty (outStr))
						return outStr;
				}
				// if nothing returned, return the result code
				return res;
			}
			catch
			{
				return 0;
			}
		}

		// Helpers similar to the C++: restricted (language) and elaborated (region) codes
		public static string GetLocaleRestrictedCode (string localeName)
		{
			if (string.IsNullOrEmpty (localeName)) localeName = CurrentLocale;
			try
			{
				var ci = new CultureInfo (localeName);
				return ci.TwoLetterISOLanguageName ?? string.Empty;
			}
			catch
			{
				// fallback: parse name
				var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 1) return parts [0];
				return string.Empty;
			}
		}

		public static string GetLocaleElaboratedCode (string localeName)
		{
			if (string.IsNullOrEmpty (localeName)) localeName = CurrentLocale;
			try
			{
				var ci = new CultureInfo (localeName);
				// Region part from RegionInfo
				try
				{
					var ri = new RegionInfo (ci.Name);
					return ri.TwoLetterISORegionName ?? string.Empty;
				}
				catch
				{
					// fallback: parse
					var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length >= 2) return parts [1];
				}
			}
			catch
			{
				var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2) return parts [1];
			}
			return string.Empty;
		}

		// LCID -> combined code like "en-US" (with configurable separator)
		public static string LcidToLocaleCode (int lcid)
		{
			try
			{
				var ci = new CultureInfo (lcid);
				if (!string.IsNullOrEmpty (ci.Name)) return ci.Name;
			}
			catch
			{
				try
				{
					var name = ToLocaleName (lcid);
					if (!string.IsNullOrEmpty (name)) return name;
				}
				catch { }
			}
			return string.Empty;
		}

		// Get the user default locale name
		public static string GetUserDefaultLocaleName ()
		{
			try
			{
				// In .NET, CurrentCulture corresponds to user default
				string name = CultureInfo.CurrentCulture.Name;
				if (!string.IsNullOrEmpty (name)) return name;
			}
			catch { }
			return LcidToLocaleCode (CultureInfo.CurrentCulture.LCID);
		}

		// Get system default locale name (machine)
		public static string GetSystemDefaultLocaleName ()
		{
			try
			{
				// InstalledUICulture / Invariant fallback
				string name = CultureInfo.InstalledUICulture.Name;
				if (!string.IsNullOrEmpty (name)) return name;
			}
			catch { }
			return LcidToLocaleCode (CultureInfo.InstalledUICulture.LCID);
		}

		// Get computer locale code similar to C++ approach
		public static string GetComputerLocaleCode ()
		{
			try
			{
				// Thread culture -> user -> system
				string threadName = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
				if (!string.IsNullOrEmpty (threadName)) return threadName;

				string user = GetUserDefaultLocaleName ();
				if (!string.IsNullOrEmpty (user)) return user;

				string system = GetSystemDefaultLocaleName ();
				if (!string.IsNullOrEmpty (system)) return system;
			}
			catch { }
			// fallback to invariant
			return CultureInfo.InvariantCulture.Name ?? string.Empty;
		}

		// Compare two locale names; returns true if equal by name or LCID
		public static bool LocaleNameCompare (string left, string right)
		{
			if (string.Equals (left, right, StringComparison.OrdinalIgnoreCase)) return true;
			try
			{
				int l = ToLCID (left);
				int r = ToLCID (right);
				return l == r && l != CultureInfo.InvariantCulture.LCID;
			}
			catch
			{
				return false;
			}
		}

		// Constants
		private const int LOCALE_NAME_MAX_LENGTH = 85; // defined by Windows
	}
	public static class UIExt
	{
		// GetDeviceCaps index for DPI X
		private const int LOGPIXELSX = 88;

		[DllImport ("user32.dll")]
		private static extern IntPtr GetDC (IntPtr hWnd);

		[DllImport ("user32.dll")]
		private static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);

		[DllImport ("gdi32.dll")]
		private static extern int GetDeviceCaps (IntPtr hdc, int nIndex);

		/// <summary>
		/// Gets system DPI as percentage (100 = 96 DPI, 125 = 120 DPI, etc.)
		/// </summary>
		public static int DPI
		{
			get { return GetDPI (); }
		}

		/// <summary>
		/// Gets system DPI as scale factor (1.0 = 100%, 1.25 = 125%)
		/// </summary>
		public static double DPIScale
		{
			get { return DPI * 0.01; }
		}

		/// <summary>
		/// Gets system DPI percentage based on 96 DPI baseline.
		/// </summary>
		public static int GetDPI ()
		{
			IntPtr hdc = IntPtr.Zero;
			try
			{
				hdc = GetDC (IntPtr.Zero);
				if (hdc == IntPtr.Zero)
					return 100; // safe default

				int dpiX = GetDeviceCaps (hdc, LOGPIXELSX);
				if (dpiX <= 0)
					return 100;

				// 96 DPI == 100%
				return (int)Math.Round (dpiX * 100.0 / 96.0);
			}
			catch
			{
				return 100;
			}
			finally
			{
				if (hdc != IntPtr.Zero)
					ReleaseDC (IntPtr.Zero, hdc);
			}
		}
	}
	public static class MSRUriHelper
	{
		public const string MsResScheme = "ms-resource:";
		public static readonly int MsResSchemeLength = MsResScheme.Length;
		/// <summary>
		/// Converts ms-resource URI or file path to path segments.
		/// </summary>
		public static int KeyToPath (string key, IList<string> output)
		{
			output.Clear ();
			if (string.IsNullOrEmpty (key))
				return 0;
			key = key.Trim ();
			try
			{
				// URI
				if (IsMsResourceUri (key))
				{
					Uri uri = new Uri (key, UriKind.RelativeOrAbsolute);
					return UriToPath (uri, output);
				}

				// File path
				SplitPath (key, '\\', output);
			}
			catch
			{
				// fallback: treat as file path
				SplitPath (key, '\\', output);
			}

			return output.Count;
		}
		public static List<string> KeyToPath (string key)
		{
			List<string> ret = new List<string> ();
			KeyToPath (key, ret);
			return ret;
		}
		/// <summary>
		/// Converts System.Uri to path segments.
		/// </summary>
		public static int UriToPath (Uri uri, IList<string> output)
		{
			output.Clear ();
			if (uri == null)
				return 0;
			try
			{
				string path = uri.AbsolutePath;
				string [] parts = path.Split (new [] { '/' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (string p in parts)
					output.Add (p);
			}
			catch
			{
				// ignored
			}
			return output.Count;
		}
		public static int UriToPath (string uristr, IList<string> output)
		{
			var uri = new Uri (uristr);
			return UriToPath (uri, output);
		}
		public static List<string> UriToPath (Uri uri)
		{
			List<string> ret = new List<string> ();
			UriToPath (uri, ret);
			return ret;
		}
		public static List<string> UriToPath (string uristr)
		{
			var uri = new Uri (uristr);
			return UriToPath (uri);
		}
		/// <summary>
		/// Checks whether key starts with ms-resource:
		/// </summary>
		public static bool IsMsResourceUri (string key)
		{
			if (string.IsNullOrEmpty (key))
				return false;

			return key.TrimStart ().StartsWith (MsResScheme, StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// ms-resource://... (full uri)
		/// </summary>
		public static bool IsFullMsResourceUri (string key)
		{
			if (!IsMsResourceUri (key))
				return false;

			return key.TrimStart ().StartsWith (
				MsResScheme + "//",
				StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// ms-resource:foo/bar (relative uri)
		/// </summary>
		public static bool IsRelativeMsResourceUri (string key)
		{
			return IsMsResourceUri (key) && !IsFullMsResourceUri (key);
		}
		private static void SplitPath (string value, char sep, IList<string> output)
		{
			string [] parts = value.Split (new [] { sep }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string p in parts)
				output.Add (p);
		}
	}
	public enum PriPathSeparator
	{
		Backslash,  // "\"
		Slash       // "/"
	}

	public sealed class PriPath: IList<string>, IEquatable<PriPath>
	{
		private readonly List<string> _segments;

		public bool IgnoreCase { get; }

		public PriPath (bool ignoreCase = true)
		{
			_segments = new List<string> ();
			IgnoreCase = ignoreCase;
		}

		public PriPath (IEnumerable<string> segments, bool ignoreCase = true)
		{
			_segments = new List<string> (segments ?? Enumerable.Empty<string> ());
			IgnoreCase = ignoreCase;
		}
		public PriPath (Uri resuri, bool ignoreCase = true) :
			this (MSRUriHelper.UriToPath (resuri), ignoreCase)
		{ }
		public PriPath (string resname, bool ignoreCase = true) :
			this (MSRUriHelper.KeyToPath (resname), ignoreCase)
		{ }
		public int Count => _segments.Count;
		public bool IsReadOnly => false;

		public string this [int index]
		{
			get { return _segments [index]; }
			set { _segments [index] = value; }
		}
		public void Add (string item) => _segments.Add (item);
		public void Clear () => _segments.Clear ();
		public bool Contains (string item) =>
			_segments.Contains (item, IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

		public void CopyTo (string [] array, int arrayIndex) => _segments.CopyTo (array, arrayIndex);
		public IEnumerator<string> GetEnumerator () => _segments.GetEnumerator ();
		IEnumerator IEnumerable.GetEnumerator () => _segments.GetEnumerator ();

		public int IndexOf (string item) =>
			_segments.FindIndex (x =>
				 string.Equals (x, item,
					 IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));

		public void Insert (int index, string item) => _segments.Insert (index, item);
		public bool Remove (string item) => _segments.Remove (item);
		public void RemoveAt (int index) => _segments.RemoveAt (index);

		public override string ToString ()
		{
			return ToString (PriPathSeparator.Backslash);
		}

		public string ToString (PriPathSeparator sep)
		{
			string s = sep == PriPathSeparator.Backslash ? "\\" : "/";
			return string.Join (s, _segments);
		}

		public string ToUriString ()
		{
			// ms-resource: URI style (relative)
			return "ms-resource:" + ToString (PriPathSeparator.Slash);
		}

		public static PriPath FromString (string path, bool ignoreCase = true)
		{
			if (path == null) return new PriPath (ignoreCase: ignoreCase);

			// detect URI
			if (path.StartsWith ("ms-resource:", StringComparison.OrdinalIgnoreCase))
			{
				string rest = path.Substring ("ms-resource:".Length);
				rest = rest.TrimStart ('/');
				var segs = rest.Split (new [] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				return new PriPath (segs, ignoreCase);
			}

			// file path
			var parts = path.Split (new [] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
			return new PriPath (parts, ignoreCase);
		}

		public override bool Equals (object obj)
		{
			return Equals (obj as PriPath);
		}

		public bool Equals (PriPath other)
		{
			if (ReferenceEquals (other, null)) return false;
			if (ReferenceEquals (this, other)) return true;
			if (Count != other.Count) return false;

			var comparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			for (int i = 0; i < Count; i++)
			{
				if (!comparer.Equals (_segments [i], other._segments [i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode ()
		{
			var comparer = IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			int hash = 17;
			foreach (var seg in _segments)
			{
				hash = hash * 31 + comparer.GetHashCode (seg?.Trim () ?? "");
			}
			return hash;
		}

		// Operators
		public static bool operator == (PriPath a, PriPath b)
		{
			if (ReferenceEquals (a, b)) return true;
			if (ReferenceEquals (a, null) || ReferenceEquals (b, null)) return false;
			return a.Equals (b);
		}

		public static bool operator != (PriPath a, PriPath b) => !(a == b);

		// Concat with another path
		public static PriPath operator + (PriPath a, PriPath b)
		{
			if (a == null) return b == null ? null : new PriPath (b, ignoreCase: true);
			if (b == null) return new PriPath (a, ignoreCase: a.IgnoreCase);

			var result = new PriPath (a, a.IgnoreCase);
			result._segments.AddRange (b._segments);
			return result;
		}

		// Append segment
		public static PriPath operator / (PriPath a, string segment)
		{
			if (a == null) return new PriPath (new [] { segment });
			var result = new PriPath (a, a.IgnoreCase);
			result._segments.Add (segment);
			return result;
		}
	}
	public sealed class PriResourceIdentifier: IEquatable<PriResourceIdentifier>
	{
		public string Key { get; private set; }
		public int TaskType { get; private set; } // 0: string (ms-resource), 1: file path
		public PriPath Path { get; private set; }
		public PriResourceIdentifier ()
		{
			Key = string.Empty;
			TaskType = 1;
			Path = new PriPath ();
		}
		public PriResourceIdentifier (string key, int type = -1)
		{
			SetKey (key, type);
		}
		public PriResourceIdentifier (Uri uri, int type = -1)
		{
			if (uri == null)
			{
				SetKey (string.Empty, type);
				return;
			}

			SetKey (uri.ToString (), type);
		}
		public void SetKey (string value, int type = -1)
		{
			Key = value ?? string.Empty;

			if (type < 0 || type > 1)
			{
				TaskType = MSRUriHelper.IsMsResourceUri (Key) ? 0 : 1;
			}
			else
			{
				TaskType = type;
			}
			var arr = MSRUriHelper.KeyToPath (Key);
			if (TaskType == 1) arr.Insert (0, "Files");
			else if (TaskType == 0)
			{
				if (MSRUriHelper.IsRelativeMsResourceUri (Key)) arr.Insert (0, "resources");
			}
			// build path segments
			Path = new PriPath (arr, ignoreCase: true);
		}
		public bool IsUri ()
		{
			return TaskType == 0;
		}
		public bool IsFilePath ()
		{
			return TaskType == 1;
		}
		public bool IsRelativeUri ()
		{
			return MSRUriHelper.IsRelativeMsResourceUri (Key);
		}
		public bool IsFullUri ()
		{
			return MSRUriHelper.IsFullMsResourceUri (Key);
		}
		public int GetPath (IList<string> output)
		{
			if (output == null)
				throw new ArgumentNullException ("output");

			output.Clear ();
			if (Path != null)
			{
				foreach (var seg in Path)
					output.Add (seg);
			}
			return output.Count;
		}
		public override string ToString ()
		{
			return Key;
		}
		// Equals / HashCode
		public override bool Equals (object obj)
		{
			return Equals (obj as PriResourceIdentifier);
		}
		public bool Equals (PriResourceIdentifier other)
		{
			if (ReferenceEquals (other, null))
				return false;
			if (ReferenceEquals (this, other))
				return true;

			// Key and Path should be equivalent
			return string.Equals (Key, other.Key, StringComparison.OrdinalIgnoreCase)
				&& ((Path == null && other.Path == null) ||
					(Path != null && Path.Equals (other.Path)));
		}
		public override int GetHashCode ()
		{
			int hash = 17;
			hash = hash * 31 + (Key ?? "").ToLowerInvariant ().GetHashCode ();
			hash = hash * 31 + (Path != null ? Path.GetHashCode () : 0);
			return hash;
		}
		public static bool operator == (PriResourceIdentifier a, PriResourceIdentifier b)
		{
			if (ReferenceEquals (a, b))
				return true;
			if (ReferenceEquals (a, null) || ReferenceEquals (b, null))
				return false;
			return a.Equals (b);
		}
		public static bool operator != (PriResourceIdentifier a, PriResourceIdentifier b)
		{
			return !(a == b);
		}
	}
}

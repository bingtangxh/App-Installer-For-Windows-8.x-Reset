using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	internal static class NativeMethods
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
	}

	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Locale
	{
		// Current locale name like "en-US"
		public string CurrentLocale
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
		public int CurrentLCID
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
		public string ToLocaleName (int lcid)
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
					int res = NativeMethods.LCIDToLocaleName (lcid, sb, sb.Capacity, 0);
					if (res > 0) return sb.ToString ();
				}
				catch { }
			}
			return string.Empty;
		}

		// Convert locale name -> LCID
		public int ToLCID (string localeName)
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
					int lcid = NativeMethods.LocaleNameToLCID (localeName, 0);
					if (lcid != 0) return lcid;
				}
				catch { }
			}
			// fallback: invariant culture
			return CultureInfo.InvariantCulture.LCID;
		}

		// Return a locale info string for given LCID and LCTYPE. LCTYPE is the Win32 LOCALE_* constant.
		// Returns a string (or empty string on failure).
		public object LocaleInfo (int lcid, int lctype)
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
				int ret = NativeMethods.GetLocaleInfoW (lcid, lctype, sb, sb.Capacity);
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
		public object LocaleInfoEx (string localeName, int lctype)
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
				int res = NativeMethods.GetLocaleInfoEx (localeName, lctype, sb, sb.Capacity);
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
		public string GetLocaleRestrictedCode (string localeName)
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

		public string GetLocaleElaboratedCode (string localeName)
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
		public string LcidToLocaleCode (int lcid)
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
		public string GetUserDefaultLocaleName ()
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
		public string GetSystemDefaultLocaleName ()
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
		public string GetComputerLocaleCode ()
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
		public bool LocaleNameCompare (string left, string right)
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
}

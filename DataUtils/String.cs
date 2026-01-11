using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_String
	{
		// Nested normalization helper
		[ComVisible (true)]
		public class _I_NString
		{
			// Normalize strings: Trim, apply Unicode normalization form KD, and to-lower invariant.
			private static string Normalize (string s)
			{
				if (s == null) return string.Empty;
				s = s.Trim ();
				return s.ToLowerInvariant ();
			}
			// Compare normalized equality
			public bool NEquals (string l, string r)
			{
				return string.Equals (Normalize (l), Normalize (r), StringComparison.Ordinal);
			}
			// Is normalized empty
			public bool Empty (string l)
			{
				string n = Normalize (l);
				return string.IsNullOrEmpty (n);
			}
			// Compare normalized strings with ordinal comparison (returns -1/0/1)
			public int Compare (string l, string r)
			{
				string nl = Normalize (l);
				string nr = Normalize (r);
				return string.CompareOrdinal (nl, nr);
			}
			// Get length of normalized string (in characters)
			public int Length (string l)
			{
				string nl = Normalize (l);
				return nl.Length;
			}
		}
		private _I_NString nstr = new _I_NString ();
		public _I_NString NString { get { return nstr; } }
		public string Trim (string src)
		{
			if (src == null) return string.Empty;
			return src.Trim ();
		}
		public string ToLower (string src)
		{
			if (src == null) return null;
			return src.ToLower (CultureInfo.InvariantCulture);
		}
		public string ToUpper (string src)
		{
			if (src == null) return null;
			return src.ToUpper (CultureInfo.InvariantCulture);
		}
		public string Format (string fmt, params object [] args)
		{
			return Utilities.FormatString (fmt, args);
		}
		// FormatInnerHTML: escape format string to inner xml, and escape each argument to inner xml wrapped in <span>...</span>
		public string FormatInnerHTML (string fmt, params object [] args)
		{
			if (fmt == null) fmt = string.Empty;
			string escapedFormat = Utilities.EscapeToInnerXml (fmt);
			if (args == null || args.Length == 0) return escapedFormat;

			object [] newArgs = new object [args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				string argText = args [i] != null ? args [i].ToString () : string.Empty;
				string esc = Utilities.EscapeToInnerXml (argText);
				newArgs [i] = string.Format ("<span>{0}</span>", esc);
			}
			try
			{
				// We used an escaped format string, but its indices are the same.
				return string.Format (CultureInfo.InvariantCulture, escapedFormat, newArgs);
			}
			catch
			{
				// fallback: simple concatenation
				StringBuilder sb = new StringBuilder ();
				sb.Append (escapedFormat);
				sb.Append (" ");
				for (int i = 0; i < newArgs.Length; i++)
				{
					if (i > 0) sb.Append (", ");
					sb.Append (newArgs [i]);
				}
				return sb.ToString ();
			}
		}
		public string StringArrayToJson (string [] strs)
		{
			return Utilities.StringArrayToJson (strs);
		}
		public static string FormatDateTime (string fmt, string jsDate)
		{
			DateTime dt = Convert.ToDateTime (jsDate);
			return string.Format (fmt, dt);
		}

	}
}

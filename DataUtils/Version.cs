using System;
using System.Globalization;
using System.Runtime.InteropServices;
namespace DataUtils
{
	/// <summary>
	/// Compact version type that encodes 4 x 16-bit parts into a 64-bit value:
	/// bits 48..63 = major, 32..47 = minor, 16..31 = build, 0..15 = revision.
	/// </summary>
	[ComVisible (true)]
	public class Version: IComparable<Version>, IEquatable<Version>, Utilities.IJsonBuild
	{
		// Backing fields
		private ushort major;
		private ushort minor;
		private ushort build;
		private ushort revision;
		public ushort Major
		{
			get { return major; }
			set { major = value; }
		}
		public ushort Minor
		{
			get { return minor; }
			set { minor = value; }
		}
		public ushort Build
		{
			get { return build; }
			set { build = value; }
		}
		public ushort Revision
		{
			get { return revision; }
			set { revision = value; }
		}
		public Version ()
		{
			major = minor = build = revision = 0;
		}
		public Version (ushort major, ushort minor, ushort build, ushort revision)
		{
			this.major = major;
			this.minor = minor;
			this.build = build;
			this.revision = revision;
		}
		public Version (ushort major, ushort minor, ushort build) : this (major, minor, build, 0) { }
		public Version (ushort major, ushort minor) : this (major, minor, 0, 0) { }
		public Version (ushort major) : this (major, 0, 0, 0) { }
		public Version (ulong packed)
		{
			FromUInt64 (packed);
		}
		public Version (string versionString)
		{
			ParseInto (versionString);
		}
		public Version (Version other)
		{
			if (other == null) throw new ArgumentNullException ("other");
			major = other.major;
			minor = other.minor;
			build = other.build;
			revision = other.revision;
		}
		public ulong ToUInt64 ()
		{
			// cast to ulong before shifting
			return (((ulong)major) << 48) | (((ulong)minor) << 32) | (((ulong)build) << 16) | ((ulong)revision);
		}
		public Version FromUInt64 (ulong value)
		{
			major = (ushort)((value >> 48) & 0xFFFFUL);
			minor = (ushort)((value >> 32) & 0xFFFFUL);
			build = (ushort)((value >> 16) & 0xFFFFUL);
			revision = (ushort)(value & 0xFFFFUL);
			return this;
		}
		public ulong Data { get { return ToUInt64 (); } set { FromUInt64 (value); } }
		public override string ToString ()
		{
			// use string.Format to be compatible with older compilers
			return string.Format (CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", major, minor, build, revision);
		}
		public string ToShortString ()
		{
			// omit trailing zeros if desired: e.g. "1.2" or "1.2.3"
			if (revision != 0)
				return ToString ();
			if (build != 0)
				return string.Format (CultureInfo.InvariantCulture, "{0}.{1}.{2}", major, minor, build);
			if (minor != 0)
				return string.Format (CultureInfo.InvariantCulture, "{0}.{1}", major, minor);
			return string.Format (CultureInfo.InvariantCulture, "{0}", major);
		}
		private void ParseInto (string s)
		{
			if (string.IsNullOrEmpty (s))
			{
				major = minor = build = revision = 0;
				return;
			}
			char [] separators = new char [] { '.', ',' };
			string [] parts = s.Split (separators, StringSplitOptions.RemoveEmptyEntries);
			ushort [] values = new ushort [4];
			for (int i = 0; i < values.Length && i < parts.Length; i++)
			{
				ushort v = 0;
				try
				{
					int parsed = int.Parse (parts [i].Trim (), NumberStyles.Integer, CultureInfo.InvariantCulture);
					if (parsed < 0) parsed = 0;
					if (parsed > 0xFFFF) parsed = 0xFFFF;
					v = (ushort)parsed;
				}
				catch
				{
					v = 0;
				}
				values [i] = v;
			}
			major = values [0];
			minor = values [1];
			build = values [2];
			revision = values [3];
		}
		public string Expression { get { return this.ToString (); } set { this.ParseInto (value); } }
		public static Version Parse (string s)
		{
			if (s == null) throw new ArgumentNullException ("s");
			return new Version (s);
		}
		public static bool TryParse (string s, out Version result)
		{
			result = null;
			if (s == null) return false;
			try
			{
				result = new Version (s);
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}
		public bool IsEmpty
		{
			get { return (major == 0 && minor == 0 && build == 0 && revision == 0); }
		}
		public bool Equals (Version other)
		{
			if (object.ReferenceEquals (other, null)) return false;
			return (this.major == other.major
					&& this.minor == other.minor
					&& this.build == other.build
					&& this.revision == other.revision);
		}
		public bool Equals (ulong ver) { return this == new Version (ver); }
		public override bool Equals (object obj)
		{
			Version v = obj as Version;
			return Equals (v);
		}
		public override int GetHashCode ()
		{
			// derive from packed ulong but return int
			ulong packed = ToUInt64 ();
			// combine high and low 32 bits for a reasonable hash
			return ((int)(packed & 0xFFFFFFFF)) ^ ((int)((packed >> 32) & 0xFFFFFFFF));
		}
		public int CompareTo (Version other)
		{
			if (object.ReferenceEquals (other, null)) return 1;
			// Compare by packed value (same semantics as C++ compare())
			ulong a = this.ToUInt64 ();
			ulong b = other.ToUInt64 ();
			if (a < b) return -1;
			if (a > b) return 1;
			return 0;
		}
		public int CompareTo (ulong another) { return this.CompareTo (new Version (another)); }
		public long Compare (Version other)
		{
			if (other == null) throw new ArgumentNullException ("other");
			// return signed difference of packed values using long
			long diff = (long)this.ToUInt64 () - (long)other.ToUInt64 ();
			return diff;
		}
		public long Compare (ulong another) { return this.Compare (new Version (another)); }
		public static bool operator == (Version a, Version b)
		{
			if (object.ReferenceEquals (a, b)) return true;
			if (object.ReferenceEquals (a, null) || object.ReferenceEquals (b, null)) return false;
			return a.Equals (b);
		}
		public static bool operator != (Version a, Version b)
		{
			return !(a == b);
		}
		public static bool operator < (Version a, Version b)
		{
			if (object.ReferenceEquals (a, null))
				return !object.ReferenceEquals (b, null); // null < non-null
			return a.CompareTo (b) < 0;
		}
		public static bool operator > (Version a, Version b)
		{
			if (object.ReferenceEquals (a, null))
				return false;
			return a.CompareTo (b) > 0;
		}
		public static bool operator <= (Version a, Version b)
		{
			if (object.ReferenceEquals (a, b)) return true;
			if (object.ReferenceEquals (a, null)) return true; // null <= anything
			return a.CompareTo (b) <= 0;
		}
		public static bool operator >= (Version a, Version b)
		{
			if (object.ReferenceEquals (a, b)) return true;
			if (object.ReferenceEquals (a, null)) return false;
			return a.CompareTo (b) >= 0;
		}
		public static explicit operator ulong (Version v)
		{
			if (v == null) return 0UL;
			return v.ToUInt64 ();
		}
		public static explicit operator Version (ulong value)
		{
			return new Version (value);
		}
		public static Version Decode (ulong packed)
		{
			return new Version (packed);
		}
		public static ulong Encode (Version v)
		{
			if (v == null) return 0UL;
			return v.ToUInt64 ();
		}
		public object BuildJSON ()
		{
			return new
			{
				major = Major,
				minor = Minor,
				build = Build,
				revision = Revision
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Version
	{
		private Version inner;
		public _I_Version ()
		{
			inner = new Version ();
		}
		public _I_Version (ushort p_ma, ushort p_mi, ushort p_b, ushort p_r)
		{
			inner = new Version (p_ma, p_mi, p_b, p_r);
		}
		public _I_Version (ushort p_ma, ushort p_mi, ushort p_b) : this (p_ma, p_mi, p_b, 0) { }
		public _I_Version (ushort p_ma, ushort p_mi) : this (p_ma, p_mi, 0, 0) { }
		public _I_Version (ushort p_ma) : this (p_ma, 0, 0, 0) { }
		public ushort Major
		{
			get { return inner.Major; }
			set { inner.Major = value; }
		}
		public ushort Minor
		{
			get { return inner.Minor; }
			set { inner.Minor = value; }
		}
		public ushort Build
		{
			get { return inner.Build; }
			set { inner.Build = value; }
		}
		public ushort Revision
		{
			get { return inner.Revision; }
			set { inner.Revision = value; }
		}
		public ushort [] Data
		{
			get
			{
				return new ushort [] { inner.Major, inner.Minor, inner.Build, inner.Revision };
			}
			set
			{
				if (value == null)
				{
					inner.Major = inner.Minor = inner.Build = inner.Revision = 0;
					return;
				}
				if (value.Length > 0) inner.Major = value [0];
				else inner.Major = 0;
				if (value.Length > 1) inner.Minor = value [1];
				else inner.Minor = 0;
				if (value.Length > 2) inner.Build = value [2];
				else inner.Build = 0;
				if (value.Length > 3) inner.Revision = value [3];
				else inner.Revision = 0;
			}
		}
		public string DataStr
		{
			get { return Stringify (); }
			set { Parse (value); }
		}
		public _I_Version Parse (string ver)
		{
			if (string.IsNullOrEmpty (ver))
			{
				inner = new Version ();
				return this;
			}
			char [] separators = new char [] { '.', ',' };
			string [] parts = ver.Split (separators, StringSplitOptions.RemoveEmptyEntries);
			ushort [] vals = new ushort [4];
			for (int i = 0; i < vals.Length && i < parts.Length; i++)
			{
				ushort v = 0;
				try
				{
					int parsed = int.Parse (parts [i].Trim (), NumberStyles.Integer, CultureInfo.InvariantCulture);
					if (parsed < 0) parsed = 0;
					if (parsed > 0xFFFF) parsed = 0xFFFF;
					v = (ushort)parsed;
				}
				catch
				{
					v = 0;
				}
				vals [i] = v;
			}
			inner.Major = vals [0];
			inner.Minor = vals [1];
			inner.Build = vals [2];
			inner.Revision = vals [3];
			return this;
		}
		public string Stringify ()
		{
			return inner.ToString ();
		}
		public override string ToString ()
		{
			return Stringify ();
		}
		public bool Valid ()
		{
			return inner.Major != 0 && inner.Minor != 0 && inner.Build != 0 && inner.Revision != 0;
		}
		public ulong ToPackedUInt64 ()
		{
			return inner.ToUInt64 ();
		}
		public void FromPackedUInt64 (ulong packed)
		{
			inner.FromUInt64 (packed);
		}
		public Version InnerVersion
		{
			get { return inner; }
			set { inner = (value != null) ? new Version (value) : new Version (); }
		}
	}
}

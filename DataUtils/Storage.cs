using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Path
	{
		public string Current
		{
			get
			{
				try { return Directory.GetCurrentDirectory (); }
				catch { return string.Empty; }
			}
			set
			{
				try
				{
					if (!string.IsNullOrEmpty (value)) Directory.SetCurrentDirectory (value);
				}
				catch { /* ignore */ }
			}
		}
		public string Program
		{
			get { return Utilities.GetCurrentProgramPath (); }
		}
		public string Root
		{
			get
			{
				try
				{
					string prog = Utilities.GetCurrentProgramPath ();
					return Path.GetDirectoryName (prog) ?? string.Empty;
				}
				catch { return string.Empty; }
			}
		}
		public string Combine (string l, string r)
		{
			if (string.IsNullOrEmpty (l)) return r ?? string.Empty;
			if (string.IsNullOrEmpty (r)) return l ?? string.Empty;
			try { return Path.Combine (l, r); }
			catch { return l + Path.DirectorySeparatorChar + r; }
		}
		public string GetName (string path)
		{
			if (string.IsNullOrEmpty (path)) return string.Empty;
			try
			{
				return Path.GetFileName (path);
			}
			catch { return string.Empty; }
		}
		public string GetDirectory (string path)
		{
			if (string.IsNullOrEmpty (path)) return string.Empty;
			try
			{
				return Path.GetDirectoryName (path) ?? string.Empty;
			}
			catch { return string.Empty; }
		}
		public string GetDir (string path) { return GetDirectory (path); }
		public bool Exist (string path)
		{
			if (string.IsNullOrEmpty (path)) return false;
			return File.Exists (path) || Directory.Exists (path);
		}
		public bool FileExist (string filepath)
		{
			if (string.IsNullOrEmpty (filepath)) return false;
			return File.Exists (filepath);
		}
		public bool DirectoryExist (string dirpath)
		{
			if (string.IsNullOrEmpty (dirpath)) return false;
			return Directory.Exists (dirpath);
		}
		public bool DirExist (string dirpath) { return DirectoryExist (dirpath); }
		public string GetEnvironmentString (string str)
		{
			if (string.IsNullOrEmpty (str)) return string.Empty;
			try
			{
				return Environment.ExpandEnvironmentVariables (str);
			}
			catch { return str; }
		}
		// Valid Windows filename?
		public bool ValidName (string filename)
		{
			if (string.IsNullOrEmpty (filename)) return false;
			char [] invalid = Path.GetInvalidFileNameChars ();
			return filename.IndexOfAny (invalid) < 0;
		}
		// filter may be e.g. "*.txt;*.md" or using "\" separators per legacy code
		public string EnumFilesToJson (string dir, string filter, bool withpath, bool sort, bool includesub)
		{
			var arr = EnumFiles (dir, filter, withpath, sort, includesub);
			return Utilities.StringArrayToJson (arr);
		}
		public string EnumDirsToJson (string dir, bool withpath, bool sort, bool includesub)
		{
			var arr = EnumDirs (dir, withpath, sort, includesub);
			return Utilities.StringArrayToJson (arr);
		}
		public string EnumSubDirsToJson (string dir, bool withpath)
		{
			var arr = EnumSubDirs (dir, withpath);
			return Utilities.StringArrayToJson (arr);
		}
		public string [] EnumFiles (string dir, string filter, bool withpath, bool sort, bool includesub)
		{
			if (string.IsNullOrEmpty (dir)) return new string [0];
			var patterns = Utilities.SplitFilters (filter);
			var list = new List<string> (100);

			try
			{
				var searchOption = includesub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
				foreach (var pat in patterns)
				{
					try
					{
						foreach (var f in Directory.EnumerateFiles (dir, pat, searchOption))
						{
							list.Add (withpath ? f : Path.GetFileName (f));
						}
					}
					catch (UnauthorizedAccessException) { /* skip */ }
					catch (DirectoryNotFoundException) { /* skip */ }
					catch (IOException) { /* skip */ }
				}

				if (sort)
				{
					list.Sort (StringComparer.OrdinalIgnoreCase);
				}
			}
			catch
			{
				// fallback: empty
			}

			return list.ToArray ();
		}
		public string [] EnumDirs (string dir, bool withpath, bool sort, bool includesub)
		{
			if (string.IsNullOrEmpty (dir)) return new string [0];
			var list = new List<string> (100);
			try
			{
				var searchOption = includesub ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
				foreach (var d in Directory.EnumerateDirectories (dir, "*", searchOption))
				{
					list.Add (withpath ? d : Path.GetFileName (d));
				}
				if (sort) list.Sort (StringComparer.OrdinalIgnoreCase);
			}
			catch { }
			return list.ToArray ();
		}
		public string [] EnumSubDirs (string dir, bool withpath)
		{
			return EnumDirs (dir, withpath, true, true);
		}
		public string CommonPrefix (string path1, string path2)
		{
			if (string.IsNullOrEmpty (path1) || string.IsNullOrEmpty (path2)) return string.Empty;
			try
			{
				string a = Utilities.NormalizeFullPath (path1);
				string b = Utilities.NormalizeFullPath (path2);
				string [] asplit = a.Split (new char [] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
				string [] bsplit = b.Split (new char [] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
				int min = Math.Min (asplit.Length, bsplit.Length);
				var sb = new StringBuilder ();
				for (int i = 0; i < min; i++)
				{
					if (!string.Equals (asplit [i], bsplit [i], StringComparison.OrdinalIgnoreCase)) break;
					sb.Append (asplit [i]);
					sb.Append (Path.DirectorySeparatorChar);
				}
				return sb.ToString ().TrimEnd (Path.DirectorySeparatorChar);
			}
			catch { return string.Empty; }
		}
		public string EnsureDirSlash (string dir)
		{
			if (string.IsNullOrEmpty (dir)) return string.Empty;
			try
			{
				if (!dir.EndsWith (Path.DirectorySeparatorChar.ToString ()))
					dir += Path.DirectorySeparatorChar;
				return dir;
			}
			catch { return dir; }
		}
		public string Normalize (string path)
		{
			if (string.IsNullOrEmpty (path)) return string.Empty;
			try { return Path.GetFullPath (path); }
			catch { return path; }
		}
		public string FullPathName (string path) { return Normalize (path); }
		public string FullPath (string path) { return FullPathName (path); }
		public string Expand (string path) { return GetEnvironmentString (path); }
		// GetFolder via SHGetFolderPath (preserves the original csidl param usage)
		public string GetFolder (int csidl)
		{
			try
			{
				// try P/Invoke to SHGetFolderPath
				return ShellHelpers.GetFolderPath (csidl);
			}
			catch
			{
				return string.Empty;
			}
		}
		// KnownFolder by GUID string (wraps SHGetKnownFolderPath)
		public string KnownFolder (string guidString)
		{
			if (string.IsNullOrWhiteSpace (guidString)) return string.Empty;
			Guid guid;
			try
			{
				guid = new Guid (guidString);
			}
			catch
			{
				return string.Empty;
			}

			try
			{
				return ShellHelpers.GetKnownFolderPath (guid);
			}
			catch
			{
				return string.Empty;
			}
		}
		public bool PEquals (string l, string r)
		{
			if (l == null && r == null) return true;
			if (l == null || r == null) return false;
			string a = Utilities.NormalizeFullPath (l);
			string b = Utilities.NormalizeFullPath (r);
			return string.Equals (a, b, StringComparison.OrdinalIgnoreCase);
		}
	}
	// Basic entry object
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Entry
	{
		protected string path;
		public _I_Entry (string path)
		{
			this.path = path ?? string.Empty;
		}
		public _I_Entry ()
		{
			this.path = string.Empty;
		}
		public virtual string Path
		{
			get { return path; }
			set { path = value ?? string.Empty; }
		}
		public virtual string Name
		{
			get
			{
				try
				{
					return System.IO.Path.GetFileName (path) ?? string.Empty;
				}
				catch { return string.Empty; }
			}
		}
		public virtual string Directory
		{
			get
			{
				try { return System.IO.Path.GetDirectoryName (path) ?? string.Empty; }
				catch { return string.Empty; }
			}
		}
		public virtual string Root { get { return Directory; } }
		public virtual bool Exist
		{
			get
			{
				return File.Exists (path) || System.IO.Directory.Exists (path);
			}
		}
		public virtual string Uri
		{
			get
			{
				try
				{
					Uri uri;
					if (System.Uri.TryCreate (path, UriKind.Absolute, out uri))
					{
						return uri.AbsoluteUri;
					}
					else
					{
						Uri u = new Uri (System.IO.Path.GetFullPath (path));
						return u.AbsoluteUri;
					}
				}
				catch
				{
					return string.Empty;
				}
			}
		}
		public virtual string FullPath
		{
			get
			{
				try { return System.IO.Path.GetFullPath (path); }
				catch { return path; }
			}
		}

		// Return relative path from frontdir to this.Path; similar semantics to C++ code
		public string RelativePath (string frontdir)
		{
			if (string.IsNullOrEmpty (path) || string.IsNullOrEmpty (frontdir)) return string.Empty;
			try
			{
				string fullFile = System.IO.Path.GetFullPath (path);
				string fullDir = System.IO.Path.GetFullPath (frontdir);
				if (!fullDir.EndsWith (System.IO.Path.DirectorySeparatorChar.ToString ()))
					fullDir += System.IO.Path.DirectorySeparatorChar;

				if (!string.Equals (System.IO.Path.GetPathRoot (fullFile), System.IO.Path.GetPathRoot (fullDir), StringComparison.OrdinalIgnoreCase))
					return string.Empty;

				if (!fullFile.StartsWith (fullDir, StringComparison.OrdinalIgnoreCase)) return string.Empty;

				return fullFile.Substring (fullDir.Length);
			}
			catch
			{
				return string.Empty;
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_File: _I_Entry
	{
		// last encoding used when reading
		protected Encoding lastEncoding;
		public _I_File () : base (string.Empty) { }
		public _I_File (string filepath) : base (filepath) { }
		// Read file contents; detect BOM if present by using StreamReader with detectEncodingFromByteOrderMarks = true
		public string Get ()
		{
			if (string.IsNullOrEmpty (path)) return string.Empty;
			try
			{
				using (FileStream fs = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader sr = new StreamReader (fs, Encoding.UTF8, true))
				{
					string text = sr.ReadToEnd ();
					lastEncoding = sr.CurrentEncoding;
					return text;
				}
			}
			catch
			{
				return null;
			}
		}
		public void Set (string content)
		{
			if (string.IsNullOrEmpty (path)) return;
			try
			{
				string dir = System.IO.Path.GetDirectoryName (path);
				if (!string.IsNullOrEmpty (dir) && !System.IO.Directory.Exists (dir))
				{
					System.IO.Directory.CreateDirectory (dir);
				}
				Encoding enc = lastEncoding ?? Encoding.UTF8;
				using (FileStream fs = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.Read))
				using (StreamWriter sw = new StreamWriter (fs, enc))
				{
					sw.Write (content ?? string.Empty);
					sw.Flush ();
				}
			}
			catch
			{
				// ignore write errors
			}
		}
		public string Content
		{
			get { return Get (); }
			set { Set (value); }
		}
		public override bool Exist
		{
			get { return File.Exists (path); }
		}
		public string FilePath
		{
			get { return this.Path; }
			set { this.Path = value; }
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Directory: _I_Entry
	{
		public _I_Directory () : base (string.Empty) { }
		public _I_Directory (string dirpath) : base (dirpath) { }
		public _I_Directory (_I_Entry file) : base (file != null ? file.Directory : string.Empty) { }

		public string DirectoryPath
		{
			get { return this.Path; }
			set { this.Path = value; }
		}
		public string DirPath { get { return DirectoryPath; } set { DirectoryPath = value; } }
		public override bool Exist { get { return System.IO.Directory.Exists (path); } }
		public string EnumFilesToJson (string filter, bool withpath, bool sort, bool includesub)
		{
			_I_Path p = new _I_Path ();
			string [] arr = p.EnumFiles (DirPath, filter, withpath, sort, includesub);
			return Utilities.StringArrayToJson (arr);
		}
		public string EnumDirsToJson (bool withpath, bool sort, bool includesub)
		{
			_I_Path p = new _I_Path ();
			string [] arr = p.EnumDirs (DirPath, withpath, sort, includesub);
			return Utilities.StringArrayToJson (arr);
		}
		public string EnumSubDirsToJson (bool withpath)
		{
			_I_Path p = new _I_Path ();
			string [] arr = p.EnumSubDirs (DirPath, withpath);
			return Utilities.StringArrayToJson (arr);
		}
		public string [] EnumFiles (string filter, bool withpath, bool sort, bool includesub)
		{
			_I_Path p = new _I_Path ();
			return p.EnumFiles (DirPath, filter, withpath, sort, includesub);
		}
		public string [] EnumDirs (bool withpath, bool sort, bool includesub)
		{
			_I_Path p = new _I_Path ();
			return p.EnumDirs (DirPath, withpath, sort, includesub);
		}
		public string [] EnumSubDirs (bool withpath)
		{
			_I_Path p = new _I_Path ();
			return p.EnumSubDirs (DirPath, withpath);
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Storage
	{
		protected _I_Path path = new _I_Path ();
		public _I_Path Path { get { return path; } }
		public _I_File GetFile (string path) { return new _I_File (path); }
		public _I_Directory GetDirectory (string path) { return new _I_Directory (path); }
		public _I_Directory GetDir (string path) { return GetDirectory (path); }
	}
	// Small shell helpers that P/Invoke for folder retrieval using CSIDL or Known Folder GUIDs
	internal static class ShellHelpers
	{
		[System.Runtime.InteropServices.DllImport ("shell32.dll")]
		private static extern int SHGetFolderPathW (IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [System.Runtime.InteropServices.MarshalAs (System.Runtime.InteropServices.UnmanagedType.LPWStr)] StringBuilder pszPath);
		public static string GetFolderPath (int csidl)
		{
			StringBuilder sb = new StringBuilder (260);
			int hr = SHGetFolderPathW (IntPtr.Zero, csidl, IntPtr.Zero, 0, sb);
			if (hr == 0) return sb.ToString ();
			return string.Empty;
		}
		[System.Runtime.InteropServices.DllImport ("shell32.dll")]
		private static extern int SHGetKnownFolderPath ([System.Runtime.InteropServices.MarshalAs (System.Runtime.InteropServices.UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
		[System.Runtime.InteropServices.DllImport ("ole32.dll")]
		private static extern void CoTaskMemFree (IntPtr pv);
		public static string GetKnownFolderPath (Guid guid)
		{
			IntPtr pathPtr;
			int hr = SHGetKnownFolderPath (guid, 0, IntPtr.Zero, out pathPtr);
			if (hr != 0 || pathPtr == IntPtr.Zero) return string.Empty;
			try
			{
				string path = Marshal.PtrToStringUni (pathPtr);
				return path ?? string.Empty;
			}
			finally
			{
				if (pathPtr != IntPtr.Zero) CoTaskMemFree (pathPtr);
			}
		}
	}
}

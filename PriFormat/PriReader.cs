using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
namespace PriFormat
{
	internal enum SearchState
	{
		Pending,
		Searching,
		Found,
		NotFound
	}
	internal sealed class SearchTask
	{
		public PriPath Path { get; }
		public BaseResources Result { get; set; }
		public SearchState State { get; set; }
		public SearchTask (PriPath path)
		{
			Path = path;
			State = SearchState.Pending;
		}
	}
	public sealed class PriReader: IDisposable
	{
		private PriFile _pri;
		private Stream _stream;
		private readonly bool _fromFile;
		private readonly object _lock = new object ();
		private readonly Dictionary<PriPath, SearchTask> _tasks =
			new Dictionary<PriPath, SearchTask> ();
		private Thread _searchThread;
		private bool _searchRunning;
		private bool _disposed;
		private readonly AutoResetEvent _searchWakeup = new AutoResetEvent (false);
		private PriReader (string filePath)
		{
			_fromFile = true;
			_stream = new FileStream (filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			_pri = PriFile.Parse (_stream);
		}
		private PriReader (Stream stream)
		{
			_fromFile = false;
			_stream = stream;
			_pri = PriFile.Parse (stream);
		}
		public static PriReader Open (string filePath) => new PriReader (filePath);
		public static PriReader Open (Stream stream) => new PriReader (stream);
		public void AddSearch (IEnumerable<string> resourceNames)
		{
			if (resourceNames == null) return;

			bool added = false;

			lock (_lock)
			{
				foreach (var name in resourceNames)
				{
					if (string.IsNullOrEmpty (name)) continue;

					var path = new PriResourceIdentifier (name).Path;
					if (!_tasks.ContainsKey (path))
					{
						_tasks [path] = new SearchTask (path);
						added = true;
					}
				}
			}

			if (added)
				EnsureSearchThread ();
		}
		public void AddSearch (string resname) { AddSearch (new string [] { resname }); }
		private void EnsureSearchThread ()
		{
			lock (_lock)
			{
				if (_disposed) return;
				if (_searchRunning) return;

				_searchRunning = true;
				_searchThread = new Thread (SearchThreadProc);
				_searchThread.IsBackground = true;
				_searchThread.Start ();
			}
		}
		private void SearchThreadProc ()
		{
			try
			{
				while (true)
				{
					if (_disposed) return;

					bool hasPending = false;

					lock (_lock)
					{
						foreach (var task in _tasks.Values)
						{
							if (task.State == SearchState.Pending)
							{
								hasPending = true;
								break;
							}
						}
					}

					if (!hasPending)
					{
						// 没任务了，休眠等待唤醒
						_searchWakeup.WaitOne (200);
						continue;
					}

					// 真正跑一次搜索
					RunSearch (TimeSpan.FromSeconds (10));
				}
			}
			finally
			{
				lock (_lock)
				{
					_searchRunning = false;
					_searchThread = null;
				}
			}
		}
		public BaseResources GetValue (string resourceName)
		{
			if (string.IsNullOrEmpty (resourceName)) return null;

			var path = new PriResourceIdentifier (resourceName).Path;
			SearchTask task;

			lock (_lock)
			{
				if (!_tasks.TryGetValue (path, out task))
				{
					task = new SearchTask (path);
					_tasks [path] = task;
					EnsureSearchThread ();
				}
			}

			// 已有结果
			if (task.State == SearchState.Found)
				return task.Result;

			// 等待搜索完成
			while (true)
			{
				if (_disposed) return null;

				lock (_lock)
				{
					if (task.State == SearchState.Found)
						return task.Result;

					if (task.State == SearchState.NotFound)
						return null;
				}

				Thread.Sleep (50);
			}
		}
		public void RunSearch (TimeSpan timeout)
		{
			var begin = DateTime.Now;
			foreach (var rmsRef in _pri.PriDescriptorSection.ResourceMapSections)
			{
				var rms = _pri.GetSectionByRef (rmsRef);
				if (rms == null || rms.HierarchicalSchemaReference != null) continue;
				var decision = _pri.GetSectionByRef (rms.DecisionInfoSection);
				foreach (var candidateSet in rms.CandidateSets.Values)
				{
					if (DateTime.Now - begin > timeout) return;
					var item = _pri.GetResourceMapItemByRef (candidateSet.ResourceMapItem);
					var fullName = item.FullName.Trim ('\\');
					var parts = fullName.Split (new [] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
					var itemPath = new PriPath (fullName, true);
					SearchTask task;

					lock (_lock)
					{
						if (!_tasks.TryGetValue (itemPath, out task))
							continue;

						if (task.State != SearchState.Pending)
							continue;

						task.State = SearchState.Searching;
					}
					var result = ReadCandidate (candidateSet, decision);
					lock (_lock)
					{
						task.Result = result;
						task.State = result != null
							? SearchState.Found
							: SearchState.NotFound;
					}
				}
			}
			lock (_lock)
			{
				foreach (var kv in _tasks)
				{
					if (kv.Value.State == SearchState.Pending)
					{
						kv.Value.State = SearchState.NotFound;
					}
				}
			}
			if (DateTime.Now - begin > timeout) return;
		}
		private BaseResources ReadCandidate (
			CandidateSet candidateSet,
			DecisionInfoSection decisionInfo)
		{
			string value = System.String.Empty;
			int restype = 0; // 0 string, 1 file
			Dictionary<StringQualifier, string> strdict = new Dictionary<StringQualifier, string> ();
			Dictionary<FileQualifier, string> filedict = new Dictionary<FileQualifier, string> ();
			foreach (var candidate in candidateSet.Candidates)
			{
				if (candidate.SourceFile != null)
				{
				}
				else
				{
					var byteSpan = new ByteSpan ();
					if (candidate.DataItem != null) byteSpan = _pri.GetDataItemByRef (candidate.DataItem);
					else byteSpan = candidate.Data;
					_stream.Seek (byteSpan.Offset, SeekOrigin.Begin);
					var binaryReader = new BinaryReader (_stream, Encoding.Default);
					{
						var data = binaryReader.ReadBytes ((int)byteSpan.Length);
						switch (candidate.Type)
						{
							case ResourceValueType.AsciiPath:
							case ResourceValueType.AsciiString:
								value = Encoding.ASCII.GetString (data).TrimEnd ('\0'); break;
							case ResourceValueType.Utf8Path:
							case ResourceValueType.Utf8String:
								value = Encoding.UTF8.GetString (data).TrimEnd ('\0'); break;
							case ResourceValueType.Path:
							case ResourceValueType.String:
								value = Encoding.Unicode.GetString (data).TrimEnd ('\0'); break;
							case ResourceValueType.EmbeddedData:
								value = Convert.ToBase64String (data); break;
						}
					}
				}
				var qualifierSet = decisionInfo.QualifierSets [candidate.QualifierSet];
				var qualis = new Dictionary <QualifierType, object> ();
				foreach (var quali in qualifierSet.Qualifiers)
				{
					var qtype = quali.Type;
					var qvalue = quali.Value;
					qualis.Add (qtype, qvalue);
				}
				if (qualis.ContainsKey (QualifierType.Language))
				{
					restype = 0;
					strdict.Add (new StringQualifier (qualis [QualifierType.Language].ToString ()), value);
				}
				else
				{
					restype = 1;
					if (qualis.ContainsKey (QualifierType.Scale))
					{
						var cons = qualis.ContainsKey (QualifierType.Contrast) ? qualis [QualifierType.Contrast].ToString () : "None";
						Contrast cs = Contrast.None;
						switch (cons?.Trim ()?.ToLower ())
						{
							case "white": cs = Contrast.White; break;
							case "black": cs = Contrast.Black; break;
							case "high": cs = Contrast.High; break;
							case "low": cs = Contrast.Low; break;
							case "none": cs = Contrast.None; break;
						}
						filedict.Add (new FileQualifier (Convert.ToInt32 (qualis [QualifierType.Scale]), cs), value);
					}
				}
			}
			if (strdict.Count > 0 && filedict.Count > 0)
			{
				if (strdict.Count >= filedict.Count) return new StringResources (strdict, true);
				else return new FileResources (filedict, true);
			}
			else if (strdict.Count > 0) return new StringResources (strdict, true);
			else if (filedict.Count > 0) return new FileResources (filedict, true);
			return new StringResources ();
		}
		public void Dispose ()
		{
			_disposed = true;
			_searchWakeup.Set ();

			lock (_lock)
			{
				_tasks.Clear ();
			}

			_pri?.Dispose ();
			_pri = null;

			if (_fromFile)
				_stream?.Dispose ();

			_stream = null;
		}
		public string Resource (string resname) { return GetValue (resname)?.SuitableValue ?? ""; }
		public Dictionary<string, string> Resources (IEnumerable<string> list)
		{
			var ret = new Dictionary<string, string> ();
			AddSearch (list);
			foreach (var item in list) ret [item] = Resource (item);
			return ret;
		}
		public string Path (string resname) => Resource (resname);
		public Dictionary<string, string> Paths (IEnumerable<string> resnames) => Resources (resnames);
		public string String (string resname) => Resource (resname);
		public Dictionary<string, string> Strings (IEnumerable<string> resnames) => Resources (resnames);
	}
	public sealed class PriReaderBundle: IDisposable
	{
		private sealed class PriInst
		{
			// 0b01 = lang, 0b10 = scale, 0b11 = both
			public readonly byte Type;
			public PriReader Reader;

			public PriInst (byte type, Stream stream)
			{
				Type = (byte)(type & 0x03);
				Reader = PriReader.Open (stream);
			}

			public bool IsValid
			{
				get { return (Type & 0x03) != 0 && Reader != null; }
			}
		}

		private readonly List<PriInst> _priFiles = new List<PriInst> (3);
		private readonly Dictionary<byte, PriInst> _mapPri = new Dictionary<byte, PriInst> ();

		// -----------------------------
		// Set
		// -----------------------------
		// type: 1 = language, 2 = scale, 3 = both
		public bool Set (byte type, Stream priStream)
		{
			byte realType = (byte)(type & 0x03);
			if (realType == 0 || priStream == null)
				return false;

			PriInst inst;
			if (_mapPri.TryGetValue (realType, out inst))
			{
				inst.Reader.Dispose ();
				inst.Reader = PriReader.Open (priStream);
			}
			else
			{
				inst = new PriInst (realType, priStream);
				_priFiles.Add (inst);
				_mapPri [realType] = inst;
			}
			return true;
		}

		// 如果你外部仍然是 IStream / IntPtr，这里假定你已有封装成 Stream 的工具
		public bool Set (byte type, IntPtr priStream)
		{
			if (priStream == IntPtr.Zero)
				return false;

			Stream stream = StreamHelper.FromIStream (priStream);
			return Set (type, stream);
		}

		// -----------------------------
		// 内部路由
		// -----------------------------
		private PriReader Get (byte type, bool mustReturn)
		{
			type = (byte)(type & 0x03);

			PriInst inst;
			if (_mapPri.TryGetValue (type, out inst))
				return inst.Reader;

			// fallback: both
			if (type != 0x03 && _mapPri.TryGetValue (0x03, out inst))
				return inst.Reader;

			if (mustReturn && _priFiles.Count > 0)
				return _priFiles [0].Reader;

			return null;
		}

		private static bool IsMsResourcePrefix (string s)
		{
			try
			{
				return MSRUriHelper.IsMsResourceUri (s) || MSRUriHelper.IsFullMsResourceUri (s) || MSRUriHelper.IsRelativeMsResourceUri (s);
			}
			catch
			{
				return MSRUriHelper.IsMsResourceUri (s);
			}
		}

		// -----------------------------
		// AddSearch
		// -----------------------------
		public void AddSearch (IEnumerable<string> arr)
		{
			if (arr == null)
				return;

			List<string> langRes = new List<string> ();
			List<string> scaleRes = new List<string> ();

			foreach (string it in arr)
			{
				if (string.IsNullOrEmpty (it))
					continue;

				if (IsMsResourcePrefix (it))
					langRes.Add (it);
				else
					scaleRes.Add (it);
			}

			PriReader langPri = Get (1, true);
			PriReader scalePri = Get (2, true);

			if (langPri != null && langRes.Count > 0)
				langPri.AddSearch (langRes);

			if (scalePri != null && scaleRes.Count > 0)
				scalePri.AddSearch (scaleRes);
		}

		public void AddSearch (string resName)
		{
			if (string.IsNullOrEmpty (resName))
				return;

			if (IsMsResourcePrefix (resName))
			{
				PriReader langPri = Get (1, true);
				if (langPri != null)
					langPri.AddSearch (resName);
			}
			else
			{
				PriReader scalePri = Get (2, true);
				if (scalePri != null)
					scalePri.AddSearch (resName);
			}
		}

		// -----------------------------
		// Resource / Path / String
		// -----------------------------
		public string Resource (string resName)
		{
			if (string.IsNullOrEmpty (resName))
				return string.Empty;

			PriReader reader;

			if (IsMsResourcePrefix (resName))
				reader = Get (1, true);
			else
				reader = Get (2, true);

			if (reader == null)
				return string.Empty;

			var res = reader.GetValue (resName);
			return res != null ? res.ToString () : string.Empty;
		}

		public Dictionary<string, string> Resources (IEnumerable<string> resNames)
		{
			if (resNames == null)
				throw new ArgumentNullException ("resNames");

			AddSearch (resNames);

			Dictionary<string, string> result = new Dictionary<string, string> ();
			foreach (string name in resNames)
				result [name] = Resource (name);

			return result;
		}

		public string Path (string resName)
		{
			return Resource (resName);
		}
		public Dictionary<string, string> Paths (IEnumerable<string> resNames)
		{
			return Resources (resNames);
		}
		public string String (string resName)
		{
			return Resource (resName);
		}
		public Dictionary<string, string> Strings (IEnumerable<string> resNames)
		{
			return Resources (resNames);
		}
		public void Dispose ()
		{
			foreach (var inst in _priFiles)
			{
				inst.Reader.Dispose ();
			}

			_mapPri.Clear ();
			_priFiles.Clear ();
		}
	}
	public abstract class BaseQualifier { }
	public class StringQualifier: BaseQualifier, IEquatable<StringQualifier>, IEquatable<string>
	{
		public string LocaleName { get; private set; }
		public int LCID => CultureInfo.GetCultureInfo (LocaleName).LCID;
		public StringQualifier (string localeName) { LocaleName = localeName; }
		public StringQualifier (int lcid) { LocaleName = CultureInfo.GetCultureInfo (lcid).Name; }
		public override string ToString () { return $"String Qualifier: {LocaleName} ({LCID})"; }
		public bool Equals (StringQualifier other)
		{
			var ca = new CultureInfo (this.LocaleName);
			var cb = new CultureInfo (other.LocaleName);
			return string.Equals (ca.Name, cb.Name, StringComparison.OrdinalIgnoreCase);
		}
		public bool Equals (string other) { return this.Equals (new StringQualifier (other)); }
		public override int GetHashCode () => CultureInfo.GetCultureInfo (LocaleName).Name.GetHashCode ();
	}
	public enum Contrast
	{
		None,
		Black,
		White,
		High,
		Low
	};
	public class FileQualifier: BaseQualifier, IEquatable<FileQualifier>, IEquatable<int>, IEquatable<Tuple<int, Contrast>>
	{
		public Contrast Contrast { get; private set; } = Contrast.None;
		public int Scale { get; private set; } = 0;
		public FileQualifier (int scale, Contrast contrast = Contrast.None)
		{
			Scale = scale;
			this.Contrast = contrast;
		}
		public override string ToString () { return $"File Qualifier: Scale {Scale}, Contrast {this.Contrast}"; }
		public bool Equals (FileQualifier other)
		{
			return this.Contrast == other.Contrast && this.Scale == other.Scale;
		}
		public bool Equals (int other)
		{
			return this.Scale == other && this.Contrast == Contrast.None;
		}
		public bool Equals (Tuple<int, Contrast> other)
		{
			return this.Contrast == other.Item2 && this.Scale == other.Item1;
		}
		public override int GetHashCode ()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + Scale.GetHashCode ();
				hash = hash * 31 + Contrast.GetHashCode ();
				return hash;
			}
		}
	}
	public abstract class BaseResources: IDisposable
	{
		public virtual void Dispose () { }
		public virtual string SuitableValue { get; } = String.Empty;
		public virtual Dictionary<BaseQualifier, string> AllValue { get; } = new Dictionary<BaseQualifier, string> ();
		/// <summary>
		/// 表示是否寻找过，如果真则不用再次寻找。
		/// </summary>
		public bool IsFind { get; set; }
	}
	public class StringResources: BaseResources, IDictionary<StringQualifier, string>, IDictionary<string, string>
	{
		private Dictionary<StringQualifier, string> dict = new Dictionary<StringQualifier, string> ();
		public string this [string key]
		{
			get { return dict [new StringQualifier (key)]; }
			set { }
		}
		public string this [StringQualifier key]
		{
			get { return dict [key]; }
			set { }
		}
		public int Count => dict.Count;
		public bool IsReadOnly => true;
		public ICollection<StringQualifier> Keys => dict.Keys;
		public ICollection<string> Values => dict.Values;
		ICollection<string> IDictionary<string, string>.Keys => dict.Keys.Select (k => k.LocaleName).ToList ();
		public void Add (KeyValuePair<string, string> item) { }
		public void Add (KeyValuePair<StringQualifier, string> item) { }
		public void Add (string key, string value) { }
		public void Add (StringQualifier key, string value) { }
		public void Clear () { }
		public bool Contains (KeyValuePair<string, string> item)
		{
			string value;
			if (TryGetValue (item.Key, out value)) return value == item.Value;
			return false;
		}
		public bool Contains (KeyValuePair<StringQualifier, string> item) => dict.Contains (item);
		public bool ContainsKey (string key)
		{
			foreach (var kv in dict)
			{
				if (string.Equals (kv.Key.LocaleName, key, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}
		public bool ContainsKey (StringQualifier key) => dict.ContainsKey (key);
		public void CopyTo (KeyValuePair<string, string> [] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException ("array");
			if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException ("arrayIndex");
			if (array.Length - arrayIndex < dict.Count) throw new ArgumentException ("The destination array is not large enough.");
			foreach (var kv in dict)
			{
				array [arrayIndex++] = new KeyValuePair<string, string> (
					kv.Key.LocaleName, kv.Value);
			}
		}
		public void CopyTo (KeyValuePair<StringQualifier, string> [] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException ("array");
			if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException ("arrayIndex");
			if (array.Length - arrayIndex < dict.Count) throw new ArgumentException ("The destination array is not large enough.");
			foreach (var kv in dict)
			{
				array [arrayIndex++] = new KeyValuePair<StringQualifier, string> (
					kv.Key, kv.Value);
			}
		}
		public IEnumerator<KeyValuePair<StringQualifier, string>> GetEnumerator () => dict.GetEnumerator ();
		public bool Remove (KeyValuePair<StringQualifier, string> item) { return false; }
		public bool Remove (string key) => false;
		public bool Remove (KeyValuePair<string, string> item) => false;
		public bool Remove (StringQualifier key) { return false; }
		public bool TryGetValue (string key, out string value)
		{
			foreach (var kv in dict)
			{
				if (string.Equals (kv.Key.LocaleName, key, StringComparison.OrdinalIgnoreCase))
				{
					value = kv.Value;
					return true;
				}
			}
			value = null;
			return false;
		}
		public bool TryGetValue (StringQualifier key, out string value) => dict.TryGetValue (key, out value);
		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator ()
		{
			foreach (var kv in dict)
			{
				yield return new KeyValuePair<string, string> (kv.Key.LocaleName, kv.Value);
			}
		}
		IEnumerator IEnumerable.GetEnumerator () => dict.GetEnumerator ();
		internal static bool LocaleNameEqualsIgnoreRegion (string a, string b)
		{
			var ca = new CultureInfo (a);
			var cb = new CultureInfo (b);
			return string.Equals (ca.TwoLetterISOLanguageName, cb.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase);
		}
		public static string GetCoincidentValue (Dictionary<StringQualifier, string> d, string localeName)
		{
			if (d == null) return null;
			foreach (var kv in d)
			{
				if (kv.Key.LocaleName?.Trim ()?.ToLower () == localeName?.Trim ()?.ToLower ()) return kv.Value;
			}
			var targetLang = new StringQualifier (localeName);
			foreach (var kv in d)
			{
				if (kv.Key.LCID == targetLang.LCID) return kv.Value;
			}
			foreach (var kv in d)
			{
				if (LocaleNameEqualsIgnoreRegion (kv.Key.LocaleName, localeName)) return kv.Value;
			}
			return String.Empty;
		}
		public static string GetSuitableValue (Dictionary<StringQualifier, string> d)
		{
			var ret = GetCoincidentValue (d, LocaleExt.GetComputerLocaleCode ());
			if (String.IsNullOrEmpty (ret)) ret = GetCoincidentValue (d, "en-us");
			if (String.IsNullOrEmpty (ret) && d.Count > 0) ret = d.ElementAt (0).Value;
			return ret;
		}
		public override string SuitableValue => GetSuitableValue (dict);
		public override Dictionary<BaseQualifier, string> AllValue => dict.ToDictionary (kv => (BaseQualifier)kv.Key, kv => kv.Value);
		public override void Dispose ()
		{
			dict.Clear ();
			dict = null;
			base.Dispose ();
		}
		~StringResources () { Dispose (); }
		public StringResources (Dictionary<StringQualifier, string> _dict, bool isfind = true)
		{
			dict = _dict;
			IsFind = isfind;
		}
		public StringResources (Dictionary<string, string> _dict, bool isfind = true)
		{
			dict = _dict.ToDictionary (kv => new StringQualifier (kv.Key), kv => kv.Value);
			IsFind = isfind;
		}
		public StringResources () { IsFind = false; }
	}
	public class FileResources: BaseResources, IDictionary<FileQualifier, string>
	{
		private Dictionary<FileQualifier, string> dict = new Dictionary<FileQualifier, string> ();
		public string this [FileQualifier key]
		{
			get { return dict [key]; }
			set { }
		}
		public int Count => dict.Count;
		public bool IsReadOnly => false;
		public ICollection<FileQualifier> Keys => dict.Keys;
		public ICollection<string> Values => dict.Values;
		public void Add (KeyValuePair<FileQualifier, string> item) { }
		public void Add (FileQualifier key, string value) { }
		public void Clear () { }
		public bool Contains (KeyValuePair<FileQualifier, string> item) => dict.Contains (item);
		public bool ContainsKey (FileQualifier key) => dict.ContainsKey (key);
		public void CopyTo (KeyValuePair<FileQualifier, string> [] array, int arrayIndex) { }
		public IEnumerator<KeyValuePair<FileQualifier, string>> GetEnumerator () => dict.GetEnumerator ();
		public bool Remove (KeyValuePair<FileQualifier, string> item) => false;
		public bool Remove (FileQualifier key) => false;
		public bool TryGetValue (FileQualifier key, out string value) => dict.TryGetValue (key, out value);
		IEnumerator IEnumerable.GetEnumerator () => dict.GetEnumerator ();
		public static string GetCoincidentValue (Dictionary<FileQualifier, string> d, int scale, Contrast contrast = Contrast.None)
		{
			var td = d.OrderBy (k => k.Key.Contrast).ThenBy (k => k.Key.Scale);
			foreach (var kv in td)
			{
				if (kv.Key.Contrast == contrast)
				{
					if (kv.Key.Scale >= scale) return kv.Value;
				}
			}
			foreach (var kv in td)
			{
				if (kv.Key.Contrast == Contrast.None)
					if (kv.Key.Scale >= scale) return kv.Value;
			}
			foreach (var kv in td)
			{
				if (kv.Key.Contrast == Contrast.Black)
					if (kv.Key.Scale >= scale) return kv.Value;
			}
			foreach (var kv in td)
			{
				if (kv.Key.Scale >= scale) return kv.Value;
			}
			if (d.Count > 0) return d.ElementAt (0).Value;
			return String.Empty;
		}
		public static string GetSuitableValue (Dictionary<FileQualifier, string> d, Contrast contrast = Contrast.None) => GetCoincidentValue (d, UIExt.DPI, contrast);
		public override string SuitableValue => GetSuitableValue (dict);
		public override Dictionary<BaseQualifier, string> AllValue => dict.ToDictionary (kv => (BaseQualifier)kv.Key, kv => kv.Value);
		public override void Dispose ()
		{
			dict.Clear ();
			dict = null;
			base.Dispose ();
		}
		~FileResources () { Dispose (); }
		public FileResources (Dictionary<FileQualifier, string> _dict, bool isfind = true)
		{
			dict = _dict;
			IsFind = isfind;
		}
		public FileResources (Dictionary<Tuple<int, Contrast>, string> _dict, bool isfind = true)
		{
			dict = _dict.ToDictionary (kv => new FileQualifier (kv.Key.Item1, kv.Key.Item2), kv => kv.Value);
			IsFind = isfind;
		}
		public FileResources () { IsFind = false; }
	}
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AppxPackage
{
	public struct PriResourceKey
	{
		public enum PriResourceType: byte
		{
			Scale = 0,
			TargetSize = 1,
			String = 2
		}
		public enum PriContrast: byte
		{
			None = 0,
			White = 1,
			Black = 2,
			High = 3,
			Low = 4
		}
		public readonly PriResourceType Type;
		public readonly PriContrast Contrast;
		public readonly ushort Value;
		public uint Raw { get; }
		public PriResourceKey (uint raw)
		{
			Raw = raw;
			Type = (PriResourceType)((raw >> 28) & 0xF);
			Contrast = (PriContrast)((raw >> 24) & 0xF);
			Value = (ushort)(raw & 0xFFFF);
		}
		public bool IsScale => Type == PriResourceType.Scale;
		public bool IsTargetSize => Type == PriResourceType.TargetSize;
		public bool IsString => Type == PriResourceType.String;
		public override string ToString ()
		{
			switch (Type)
			{
				case PriResourceType.Scale:
					return $"Scale={Value}, Contrast={Contrast}";
				case PriResourceType.TargetSize:
					return $"TargetSize={Value}, Contrast={Contrast}";
				case PriResourceType.String:
					return $"LCID=0x{Value:X4}";
				default:
					return $"Unknown(0x{Raw:X8})";
			}
		}
	}
	public class PriReader: IDisposable
	{
		private IntPtr m_hPriFile = IntPtr.Zero;
		public bool Valid { get { return m_hPriFile != IntPtr.Zero; } }
		public void Dispose ()
		{
			if (Valid)
			{
				PriFileHelper.DestroyPriFileInstance (m_hPriFile);
				m_hPriFile = IntPtr.Zero;
			}
		}
		~PriReader () { Dispose (); }
		public bool Create (IStream isfile)
		{
			try
			{
				Dispose ();
				if (isfile == null) return false;
				var pStream = Marshal.GetComInterfaceForObject (isfile, typeof (IStream));
				m_hPriFile = PriFileHelper.CreatePriFileInstanceFromStream (pStream);
			}
			catch (Exception) { m_hPriFile = IntPtr.Zero; }
			return Valid;
		}
		public bool Create (IntPtr pStream)
		{
			try
			{
				Dispose ();
				if (pStream == IntPtr.Zero) return false;
				m_hPriFile = PriFileHelper.CreatePriFileInstanceFromStream (pStream);
			}
			catch (Exception) { m_hPriFile = IntPtr.Zero; }
			return Valid;
		}
		public bool Create ([MarshalAs (UnmanagedType.LPWStr)] string filePath)
		{
			try
			{
				Dispose ();
				if (string.IsNullOrWhiteSpace (filePath)) return false;
				m_hPriFile = PriFileHelper.CreatePriFileInstanceFromPath (filePath);
			}
			catch (Exception) { m_hPriFile = IntPtr.Zero; }
			return Valid;
		}
		public PriReader (IStream isfile) { Create (isfile); }
		public PriReader (IntPtr pStream) { Create (pStream); }
		public PriReader ([MarshalAs (UnmanagedType.LPWStr)] string fileName) { Create (fileName); }
		public PriReader () { }
		public void AddSearch (IEnumerable<string> arr)
		{
			IntPtr buf = IntPtr.Zero;
			try
			{
				if (arr == null) return;
				buf = LpcwstrListHelper.Create (arr);
				PriFileHelper.FindPriResource (m_hPriFile, buf);
			}
			finally
			{
				if (buf != IntPtr.Zero) LpcwstrListHelper.Destroy (buf);
			}
		}
		public void AddSearch (string uri) { AddSearch (new string [] { uri }); }
		public string Resource (string resName)
		{
			var task = Task.Factory.StartNew (() => {
				IntPtr ret = IntPtr.Zero;
				try
				{
					ret = PriFileHelper.GetPriResource (m_hPriFile, resName);
					if (ret == IntPtr.Zero) return string.Empty;
					return PriFileHelper.PtrToString (ret);
				}
				finally
				{
					//if (ret != IntPtr.Zero) PriFileHelper.FreePriString(ret);
				}
			});
			return task.Result;
		}
		public Dictionary<string, string> Resources (IEnumerable<string> resnames)
		{
			if (resnames == null) throw new ArgumentNullException (nameof (resnames));
			var result = new Dictionary<string, string> ();
			AddSearch (resnames);
			foreach (var name in resnames) result [name] = Resource (name);
			return result;
		}
		public static string LastError { get { return PriFileHelper.PriFileGetLastError (); } }
		public string Path (string resName) { return Resource (resName); }
		public Dictionary<string, string> Paths (IEnumerable<string> resNames) { return Resources (resNames); }
		public string String (string resName) { return Resource (resName); }
		public Dictionary<string, string> Strings (IEnumerable<string> resNames) { return Resources (resNames); }
		public Dictionary<PriResourceKey, string> ResourceAllValues (string resName)
		{
			var task = Task.Factory.StartNew (() => {
				IntPtr ptr = IntPtr.Zero;

				try
				{
					ptr = PriFileHelper.GetPriResourceAllValueList (
						m_hPriFile, resName);

					if (ptr == IntPtr.Zero)
						return new Dictionary<PriResourceKey, string> ();

					var raw =
						PriFileHelper.ParseDWSPAIRLIST (ptr);

					var result =
						new Dictionary<PriResourceKey, string> ();

					foreach (var kv in raw)
					{
						var key = new PriResourceKey (kv.Key);
						result [key] = kv.Value;
					}
					return result;
				}
				finally
				{
					if (ptr != IntPtr.Zero) PriFileHelper.DestroyPriResourceAllValueList (ptr);
				}
			});
			return task.Result;
		}
		public Dictionary<string, Dictionary<PriResourceKey, string>> ResourcesAllValues (IEnumerable<string> resNames)
		{
			var task = Task.Factory.StartNew (() => {
				IntPtr ptr = IntPtr.Zero;

				try
				{
					var list = resNames?.ToList ();

					if (list == null || list.Count == 0)
						return new Dictionary<string,
							Dictionary<PriResourceKey, string>> ();

					ptr = PriFileHelper.GetPriResourcesAllValuesList (
						m_hPriFile,
						list.ToArray (),
						(uint)list.Count);

					if (ptr == IntPtr.Zero)
						return new Dictionary<string,
							Dictionary<PriResourceKey, string>> ();

					var raw =
						PriFileHelper.ParseWSDSPAIRLIST (ptr);

					var result =
						new Dictionary<string,
							Dictionary<PriResourceKey, string>> ();

					foreach (var outer in raw)
					{
						var innerDict =
							new Dictionary<PriResourceKey, string> ();

						foreach (var inner in outer.Value)
						{
							var key =
								new PriResourceKey (inner.Key);

							innerDict [key] = inner.Value;
						}

						result [outer.Key] = innerDict;
					}

					return result;
				}
				finally
				{
					if (ptr != IntPtr.Zero)
						PriFileHelper.DestroyResourcesAllValuesList (ptr);
				}
			});

			return task.Result;
		}
		/// <summary>
		/// 获取指定字符串资源的所有语言变体（语言代码 → 字符串值）
		/// </summary>
		public Dictionary<string, string> LocaleResourceAllValue (string resName)
		{
			var task = Task.Factory.StartNew (() =>
			{
				IntPtr ptr = IntPtr.Zero;
				try
				{
					// 修正：第一个参数应为 m_hPriFile，而不是 ptr
					ptr = PriFileHelper.GetPriLocaleResourceAllValuesList (m_hPriFile, resName);
					if (ptr == IntPtr.Zero)
						return new Dictionary<string, string> ();

					// 解析 HWSWSPAIRLIST 为 Dictionary<string, string>
					var raw = PriFileHelper.ParseWSWSPAIRLIST (ptr);
					return raw ?? new Dictionary<string, string> ();
				}
				finally
				{
					if (ptr != IntPtr.Zero)
						PriFileHelper.DestroyLocaleResourceAllValuesList (ptr);
				}
			});
			return task.Result;
		}
		/// <summary>
		/// 获取指定文件资源的所有缩放/对比度/目标大小变体（PriResourceKey → 文件路径）
		/// </summary>
		public Dictionary<PriResourceKey, string> PathResourceAllValue (string resName)
		{
			var task = Task.Factory.StartNew (() =>
			{
				IntPtr ptr = IntPtr.Zero;
				try
				{
					ptr = PriFileHelper.GetPriFileResourceAllValuesList (m_hPriFile, resName);
					if (ptr == IntPtr.Zero)
						return new Dictionary<PriResourceKey, string> ();

					// 解析 HDWSPAIRLIST 为 Dictionary<uint, string>
					var raw = PriFileHelper.ParseDWSPAIRLIST (ptr);
					if (raw == null)
						return new Dictionary<PriResourceKey, string> ();

					// 将 uint 键转换为 PriResourceKey
					var result = new Dictionary<PriResourceKey, string> ();
					foreach (var kv in raw)
					{
						var key = new PriResourceKey (kv.Key);
						result [key] = kv.Value;
					}
					return result;
				}
				finally
				{
					if (ptr != IntPtr.Zero)
						PriFileHelper.DestroyPriResourceAllValueList (ptr); // 注意：使用对应的释放函数
				}
			});
			return task.Result;
		}
	}
	public class PriReaderBundle: IDisposable
	{
		private class PriInst
		{
			public byte Type;      // 0b01 lang, 0b10 scale, 0b11 both
			public PriReader Reader;
			public PriInst (byte type, IStream stream)
			{
				Type = (byte)(type & 0x03);
				Reader = new PriReader (stream);
			}
			public PriInst (byte type, IntPtr stream)
			{
				Type = (byte)(type & 0x03);
				Reader = new PriReader (stream);
			}
			public bool IsValid
			{
				get { return (Type & 0x03) != 0; }
			}
		}
		private readonly List<PriInst> _priFiles = new List<PriInst> (3);
		private readonly Dictionary<byte, PriInst> _mapPri = new Dictionary<byte, PriInst> ();
		// type: 1 language, 2 scale, 3 both
		public bool Set (byte type, IStream priStream)
		{
			byte realType = (byte)(type & 0x03);
			if (realType == 0) return false;
			PriInst inst;
			if (_mapPri.TryGetValue (realType, out inst))
			{
				inst.Reader.Dispose ();
				if (priStream != null) inst.Reader.Create (priStream);
			}
			else
			{
				if (priStream == null) return false;
				inst = new PriInst (realType, priStream);
				_priFiles.Add (inst);
				_mapPri [realType] = inst;
			}
			return true;
		}
		public bool Set (byte type, IntPtr priStream)
		{
			byte realType = (byte)(type & 0x03);
			if (realType == 0) return false;
			PriInst inst;
			if (_mapPri.TryGetValue (realType, out inst))
			{
				inst.Reader.Dispose ();
				if (priStream != IntPtr.Zero) inst.Reader.Create (priStream);
			}
			else
			{
				if (priStream == IntPtr.Zero) return false;
				inst = new PriInst (realType, priStream);
				_priFiles.Add (inst);
				_mapPri [realType] = inst;
			}
			return true;
		}
		private PriReader Get (byte type, bool mustReturn)
		{
			type = (byte)(type & 0x03);
			PriInst inst;
			if (_mapPri.TryGetValue (type, out inst)) return inst.Reader;
			if (type != 0x03 && _mapPri.TryGetValue (0x03, out inst)) return inst.Reader;
			if (_priFiles.Count > 0 && mustReturn) return _priFiles [0].Reader;
			return null;
		}
		private static bool IsMsResourcePrefix (string s)
		{
			return PriFileHelper.IsMsResourcePrefix (s);
		}
		public void AddSearch (IEnumerable<string> arr)
		{
			if (arr == null) return;
			List<string> strRes = new List<string> ();
			List<string> pathRes = new List<string> ();
			foreach (string it in arr)
			{
				if (IsMsResourcePrefix (it)) strRes.Add (it);
				else pathRes.Add (it);
			}
			PriReader langPri = Get (1, true);
			PriReader scalePri = Get (2, true);
			if (langPri != null && strRes.Count > 0) langPri.AddSearch (strRes);
			if (scalePri != null && pathRes.Count > 0) scalePri.AddSearch (pathRes);
		}
		public void AddSearch (string resName)
		{
			if (IsMsResourcePrefix (resName))
			{
				PriReader langPri = Get (1, true);
				if (langPri != null) langPri.AddSearch (resName);
			}
			else
			{
				PriReader scalePri = Get (2, true);
				if (scalePri != null) scalePri.AddSearch (resName);
			}
		}
		public string Resource (string resName)
		{
			if (IsMsResourcePrefix (resName))
			{
				PriReader langPri = Get (1, true);
				return langPri != null ? langPri.Resource (resName) : string.Empty;
			}
			else
			{
				PriReader scalePri = Get (2, true);
				return scalePri != null ? scalePri.Resource (resName) : string.Empty;
			}
		}
		public Dictionary<string, string> Resources (IEnumerable<string> resNames)
		{
			if (resNames == null) throw new ArgumentNullException ("resNames");
			Dictionary<string, string> result = new Dictionary<string, string> ();
			AddSearch (resNames);
			foreach (string name in resNames) result [name] = Resource (name);
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
			foreach (PriInst it in _priFiles) it.Reader.Dispose ();
			_mapPri.Clear ();
			_priFiles.Clear ();
		}
	}
}

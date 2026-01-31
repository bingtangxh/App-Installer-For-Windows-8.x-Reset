using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AppxPackage.Info;
using NativeWrappers;
using System.IO;
using System.Threading;
//using PriFormat;
namespace AppxPackage
{
	public static class DataUrlHelper
	{
		[DllImport ("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern int FindMimeFromData (
			IntPtr pBC,
			string pwzUrl,
			byte [] pBuffer,
			int cbSize,
			string pwzMimeProposed,
			int dwMimeFlags,
			out IntPtr ppwzMimeOut,
			int dwReserved);
		private const int FMFD_RETURNUPDATEDIMGMIMES = 0x00000001;
		private const int FMFD_IGNOREMIMETEXTPLAIN = 0x00000010;
		private const int FMFD_URLASFILENAME = 0x00000020;
		private static string GetMimeTypeFromStream (Stream stream)
		{
			if (stream == null) return string.Empty;
			long originalPos = 0;
			try
			{
				if (stream.CanSeek)
				{
					originalPos = stream.Position;
					stream.Seek (0, SeekOrigin.Begin);
				}
				byte [] buffer = new byte [256];
				int bytesRead = stream.Read (buffer, 0, buffer.Length);
				if (stream.CanSeek) stream.Seek (originalPos, SeekOrigin.Begin);
				if (bytesRead == 0) return string.Empty;
				IntPtr mimePtr;
				int hr = FindMimeFromData (
					IntPtr.Zero,
					null,
					buffer,
					bytesRead,
					null,
					FMFD_RETURNUPDATEDIMGMIMES | FMFD_IGNOREMIMETEXTPLAIN | FMFD_URLASFILENAME,
					out mimePtr,
					0);
				string mime = string.Empty;
				if (hr == 0 && mimePtr != IntPtr.Zero)
				{
					mime = Marshal.PtrToStringUni (mimePtr);
					Marshal.FreeCoTaskMem (mimePtr);
				}
				if (string.IsNullOrEmpty (mime))
				{
					// fallback by magic bytes
					if (bytesRead >= 8 && buffer [0] == 0x89 && buffer [1] == 0x50 && buffer [2] == 0x4E && buffer [3] == 0x47 &&
						buffer [4] == 0x0D && buffer [5] == 0x0A && buffer [6] == 0x1A && buffer [7] == 0x0A)
						mime = "image/png";
					else if (bytesRead >= 3 && buffer [0] == 0xFF && buffer [1] == 0xD8)
						mime = "image/jpeg";
					else if (bytesRead >= 6 && Encoding.ASCII.GetString (buffer, 0, 6) == "GIF89a")
						mime = "image/gif";
					else if (bytesRead >= 2 && buffer [0] == 'B' && buffer [1] == 'M')
						mime = "image/bmp";
					else if (bytesRead >= 12 && Encoding.ASCII.GetString (buffer, 0, 4) == "RIFF" &&
							 Encoding.ASCII.GetString (buffer, 8, 4) == "WEBP")
						mime = "image/webp";
					else if (bytesRead >= 4 && buffer [0] == 0x00 && buffer [1] == 0x00 && buffer [2] == 0x01 && buffer [3] == 0x00)
						mime = "image/x-icon";
					else
						mime = "application/octet-stream";
				}
				return mime;
			}
			catch
			{
				return string.Empty;
			}
		}
		public static string FileToDataUrl (string filePath)
		{
			if (string.IsNullOrEmpty (filePath)) return string.Empty;
			try
			{
				using (FileStream fs = new FileStream (filePath, FileMode.Open, FileAccess.Read))
				{
					if (fs.Length == 0) return string.Empty;
					string mime = GetMimeTypeFromStream (fs);
					if (string.IsNullOrEmpty (mime)) return string.Empty;
					byte [] bytes = new byte [fs.Length];
					fs.Seek (0, SeekOrigin.Begin);
					int read = fs.Read (bytes, 0, bytes.Length);
					if (read != bytes.Length) return string.Empty;
					string base64 = Convert.ToBase64String (bytes);
					return $"data:{mime};base64,{base64}";
				}
			}
			catch
			{
				return string.Empty;
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class BaseInfoSectWithPRISingle: BaseInfoSection
	{
		protected Ref<ManifestReader> m_reader = new Ref<ManifestReader> (null);
		protected Ref<PriReader> m_pri = new Ref<PriReader> (null);
		protected Ref<bool> m_usePri = new Ref<bool> (false);
		protected Ref<bool> m_enablePri = new Ref<bool> (false);
		public BaseInfoSectWithPRISingle (ref IntPtr hReader, ManifestReader reader, ref PriReader pri, ref bool usePri, ref bool enablePri) : base (ref hReader)
		{
			m_reader.Set (reader);
			m_pri.Set (pri);
			m_usePri.Set (usePri);
			m_enablePri.Set (enablePri);
		}
		public new void Dispose ()
		{
			try { m_reader.Set (null); } catch (Exception) { }
			try { m_pri.Set (null); } catch (Exception) { }
			m_usePri = null;
			m_enablePri = null;
			m_hReader = null;
		}
		~BaseInfoSectWithPRISingle () { Dispose (); }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRIdentity: BaseInfoSection, Info.IIdentity
	{
		public MRIdentity (ref IntPtr hReader) : base (ref hReader) { }
		protected string StringValue (uint name)
		{
			var ptr = PackageReadHelper.GetManifestIdentityStringValue (m_hReader, name);
			return PackageReadHelper.GetStringAndFreeFromPkgRead (ptr) ?? "";
		}
		public string FamilyName { get { return StringValue (2); } }
		public string FullName { get { return StringValue (3); } }
		public string Name { get { return StringValue (0); } }
		public List<Architecture> ProcessArchitecture
		{
			get
			{
				var list = new List<Architecture> ();
				uint uarch = 0;
				PackageReadHelper.GetManifestIdentityArchitecture (m_hReader, out uarch);
				var t = PackageReadHelper.GetManifestType (m_hReader);
				switch (t)
				{
					case 1:
						switch (uarch)
						{
							case 0x1: list.Add (Architecture.x86); break;
							case 0x2: list.Add (Architecture.x64); break;
							case 0x4: list.Add (Architecture.ARM); break;
							case 0x8: list.Add (Architecture.ARM64); break;
							case 0xE: list.Add (Architecture.Neutral); break;
						}
						break;
					case 2:
						if ((uarch & 0x1) != 0) list.Add (Architecture.x86);
						if ((uarch & 0x2) != 0) list.Add (Architecture.x64);
						if ((uarch & 0x4) != 0) list.Add (Architecture.ARM);
						if ((uarch & 0x8) != 0) list.Add (Architecture.ARM64);
						break;
				}
				return list;
			}
		}
		public string Publisher { get { return StringValue (1); } }
		public string ResourceId { get { return StringValue (4); } }
		public DataUtils.Version Version
		{
			get
			{
				PackageReadHelper.VERSION ver = new PackageReadHelper.VERSION ();
				PackageReadHelper.GetManifestIdentityVersion (m_hReader, out ver);
				return new DataUtils.Version (ver.major, ver.minor, ver.build, ver.revision);
			}
		}
		public override object BuildJSON ()
		{
			return new
			{
				name = Name,
				package_full_name = FullName,
				package_family_name = FamilyName,
				publisher = Publisher,
				resource_id = ResourceId,
				architecture = ProcessArchitecture.Select (e => (int)e).ToList (),
				version = Version.BuildJSON ()
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRProperties: BaseInfoSectWithPRISingle, Info.IProperties
	{
		public MRProperties (ref IntPtr hReader, ManifestReader reader, ref PriReader priBundle, ref bool usePri, ref bool enablePri) : base (ref hReader, reader, ref priBundle, ref usePri, ref enablePri) { }
		protected string StringValue (string attr)
		{
			var ptr = PackageReadHelper.GetManifestPropertiesStringValue (m_hReader, attr);
			return PackageReadHelper.GetStringAndFreeFromPkgRead (ptr) ?? "";
		}
		protected bool BoolValue (string attr, bool defaultValue = false)
		{
			int ret = 0;
			HRESULT hr = PackageReadHelper.GetManifestPropertiesBoolValue (m_hReader, attr, out ret);
			if (hr.Succeeded) return ret != 0;
			else return defaultValue;
		}
		protected string StringResValue (string attr)
		{
			var res = StringValue (attr);
			try
			{
				if (m_usePri && m_enablePri)
				{
					if (PriFileHelper.IsMsResourcePrefix (res))
						return m_pri.Value.String (res) ?? res;
				}
			}
			catch (Exception) { }
			return res;
		}
		protected string PathResValue (string attr)
		{
			var res = StringValue (attr);
			try
			{
				if (m_usePri && m_enablePri)
				{
					var resvalue = m_pri.Value.String (res);
					if (!string.IsNullOrEmpty (resvalue)) return resvalue;
					else return res;
				}
			}
			catch (Exception) { }
			return res;
		}
		public string Description { get { return StringResValue ("Description"); } }
		public string DisplayName { get { return StringResValue ("DisplayName"); } }
		public bool Framework { get { return BoolValue ("Framework"); } }
		public string Logo { get { return PathResValue ("Logo"); } }
		public string LogoBase64
		{
			get
			{
				var root = m_reader.Value.FileRoot;
				var logopath = Path.Combine (root, Logo);
				var ret = DataUrlHelper.FileToDataUrl (logopath);
				if (!string.IsNullOrWhiteSpace (ret)) return ret;
				logopath = Path.Combine (root, StringValue ("Logo"));
				ret = DataUrlHelper.FileToDataUrl (logopath);
				if (!string.IsNullOrWhiteSpace (ret)) return ret;
				return String.Empty;
			}
		}
		public string Publisher { get { return StringResValue ("PublisherDisplayName"); } }
		public bool ResourcePackage { get { return BoolValue ("ResourcePackage"); } }
		public override object BuildJSON ()
		{
			return new
			{
				display_name = DisplayName,
				description = Description,
				publisher_display_name = Publisher,
				Framework = Framework,
				resource_package = ResourcePackage,
				logo = Logo,
				logo_base64 = LogoBase64
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRApplication: Dictionary<string, string>
	{
		protected Ref<IntPtr> m_hReader = IntPtr.Zero;
		protected Ref<PriReader> m_pri = null;
		protected Ref<bool> m_usePri = false;
		protected Ref<bool> m_enablePri = false;
		protected string m_root = String.Empty;
		public MRApplication (ref IntPtr hReader, ref PriReader priBundle, ref bool usePri, ref bool enablePri, string rootDir) : base (StringComparer.OrdinalIgnoreCase)
		{
			m_hReader = hReader;
			m_pri = priBundle;
			m_usePri = usePri;
			m_enablePri = enablePri;
			m_root = rootDir;
		}
		public MRApplication (ref Ref<IntPtr> m_hReader, ref Ref<PriReader> m_priBundle, ref Ref<bool> m_usePri, ref Ref<bool> m_enablePri, string rootDir)
		{
			this.m_hReader = m_hReader;
			this.m_pri = m_priBundle;
			this.m_usePri = m_usePri;
			this.m_enablePri = m_enablePri;
			this.m_root = rootDir;
		}
		public string UserModelID { get { return this ["AppUserModelID"]; } }
		protected bool EnablePri ()
		{
			if (m_pri == null || m_pri.Value == null) return false;
			if (!m_usePri.Value) return false;
			return m_enablePri.Value;
		}
		protected string StringResValue (string attr)
		{
			try
			{
				var res = this [attr];
				try
				{
					if (m_usePri && m_enablePri)
					{
						if (PriFileHelper.IsMsResourcePrefix (res))
							return m_pri.Value.String (res) ?? res;
					}
				}
				catch (Exception) { }
				return res;
			}
			catch (Exception) { return String.Empty; }
		}
		protected string PriGetRes (string resName)
		{
			if (string.IsNullOrEmpty (resName)) return string.Empty;
			if (m_pri == null || m_pri.Value == null) return string.Empty;
			return m_pri.Value.Resource (resName);
		}
		protected bool IsFilePathKey (string key)
		{
			foreach (var i in ConstData.FilePathItems)
				if ((i?.Trim ()?.ToLower () ?? "") == (key?.Trim ()?.ToLower () ?? ""))
					return true;
			return false;
		}
		public new string this [string key]
		{
			get
			{
				string value;
				if (!TryGetValue (key, out value))
				{
					value = string.Empty;
					base [key] = value;
				}
				if (!EnablePri ()) return value;
				if (PriFileHelper.IsMsResourcePrefix (value))
				{
					string pri = PriGetRes (value);
					return string.IsNullOrEmpty (pri) ? value : pri;
				}
				if (IsFilePathKey (key) && !string.IsNullOrEmpty (value))
				{
					string pri = PriGetRes (value);
					return string.IsNullOrEmpty (pri) ? value : pri;
				}
				return value;
			}
		}
		public string At (string key)
		{
			string value;
			if (!TryGetValue (key, out value)) throw new KeyNotFoundException ($"PRBaseApplication.At: key \"{key}\" not found");
			if (!EnablePri ()) return value;
			if (PriFileHelper.IsMsResourcePrefix (value))
			{
				string pri = PriGetRes (value);
				if (!string.IsNullOrEmpty (pri))
					return pri;
			}
			return value;
		}
		public string NewAt (string key, bool toPriString)
		{
			string value;
			if (!TryGetValue (key, out value))
			{
				value = string.Empty;
				base [key] = value;
			}
			if (!EnablePri () && toPriString) return value;
			if (PriFileHelper.IsMsResourcePrefix (value))
			{
				string pri = PriGetRes (value);
				return string.IsNullOrEmpty (pri) ? value : pri;
			}
			if (IsFilePathKey (key) && !string.IsNullOrEmpty (value))
			{
				string pri = PriGetRes (value);
				return string.IsNullOrEmpty (pri) ? value : pri;
			}
			return value;
		}
		public string NewAtBase64 (string key)
		{
			string value = NewAt (key, true);
			if (!IsFilePathKey (key) || string.IsNullOrEmpty (value)) return "";
			switch (PackageReadHelper.GetPackageType (m_hReader))
			{
				case 1: // PKGTYPE_APPX
					{
						var root = m_root;
						var logopath = Path.Combine (root, NewAt (key, true));
						var ret = DataUrlHelper.FileToDataUrl (logopath);
						if (!string.IsNullOrWhiteSpace (ret)) return ret;
						logopath = Path.Combine (root, NewAt (key, false));
						ret = DataUrlHelper.FileToDataUrl (logopath);
						if (!string.IsNullOrWhiteSpace (ret)) return ret;
						return String.Empty;
					} break;
			}
			return "";
		}
		public static bool operator == (MRApplication a, MRApplication b)
		{
			if (ReferenceEquals (a, b)) return true;
			if ((object)a == null || (object)b == null) return false;
			return string.Equals (a.UserModelID, b.UserModelID, StringComparison.OrdinalIgnoreCase);
		}
		public static bool operator != (MRApplication a, MRApplication b)
		{
			return !(a == b);
		}
		public override bool Equals (object obj)
		{
			var other = obj as MRApplication;
			if (other == null) return false;
			return this == other;
		}
		public override int GetHashCode ()
		{
			return (UserModelID ?? "").ToLowerInvariant ().GetHashCode ();
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRApplications: BaseInfoSectWithPRISingle, IEnumerable<MRApplication>
	{
		private IntPtr _hList = IntPtr.Zero;
		private List<MRApplication> _apps;
		public MRApplications (
			ref IntPtr hReader,
			ManifestReader reader,
			ref PriReader priBundle,
			ref bool usePri,
			ref bool enablePri)
			: base (ref hReader, reader, ref priBundle, ref usePri, ref enablePri)
		{
			if (IsValid)
			{
				_hList = PackageReadHelper.GetManifestApplications (m_hReader.Value);
			}
		}
		#region Dispose
		public new void Dispose ()
		{
			if (_hList != IntPtr.Zero)
			{
				PackageReadHelper.DestroyManifestApplications (_hList);
				_hList = IntPtr.Zero;
			}
			base.Dispose ();
		}
		~MRApplications ()
		{
			Dispose ();
		}
		#endregion
		#region 属性：Applications
		public List<MRApplication> Applications
		{
			get
			{
				if (_apps == null)
					_apps = ReadApplications ();
				return _apps;
			}
		}
		#endregion
		#region 索引器
		public MRApplication this [int index]
		{
			get { return Applications [index]; }
		}
		public MRApplication this [string key]
		{
			get
			{
				foreach (var app in Applications)
				{
					if (string.Equals (app.UserModelID, key, StringComparison.OrdinalIgnoreCase))
					{
						return app;
					}
				}
				return null;
			}
		}
		#endregion
		#region IEnumerable
		public IEnumerator<MRApplication> GetEnumerator ()
		{
			return Applications.GetEnumerator ();
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion
		#region 内部解析逻辑（核心等价 C++ get）
		private List<MRApplication> ReadApplications ()
		{
			var list = new List<MRApplication> ();
			if (_hList == IntPtr.Zero) return list;
			IntPtr hMapList = PackageReadHelper.ApplicationsToMap (_hList);
			if (hMapList == IntPtr.Zero) return list;
			try
			{
				uint count = (uint)Marshal.ReadInt32 (hMapList);
				int baseOffset = Marshal.SizeOf (typeof (uint));
				for (int i = 0; i < count; i++)
				{
					IntPtr hKeyValues = Marshal.ReadIntPtr (hMapList, baseOffset + i * IntPtr.Size);
					if (hKeyValues == IntPtr.Zero) continue;
					list.Add (ReadSingleApplication (hKeyValues));
				}
			}
			finally
			{
				PackageReadHelper.DestroyApplicationsMap (hMapList);
			}
			return list;
		}
		private MRApplication ReadSingleApplication (IntPtr hKeyValues)
		{
			var app = new MRApplication (ref m_hReader, ref m_pri, ref m_usePri, ref m_enablePri, m_reader.Value.FileRoot);
			int pairCount = Marshal.ReadInt32 (hKeyValues);
			IntPtr arrayBase = IntPtr.Add (hKeyValues, sizeof (uint));
			for (int i = 0; i < pairCount; i++)
			{
				IntPtr pPairPtr = Marshal.ReadIntPtr (arrayBase, i * IntPtr.Size);
				if (pPairPtr == IntPtr.Zero) continue;
				PackageReadHelper.PAIR_PVOID pair =
					(PackageReadHelper.PAIR_PVOID)Marshal.PtrToStructure (pPairPtr, typeof (PackageReadHelper.PAIR_PVOID));
				if (pair.lpKey == IntPtr.Zero) continue;
				string key = Marshal.PtrToStringUni (pair.lpKey);
				if (string.IsNullOrEmpty (key)) continue;
				string value = pair.lpValue != IntPtr.Zero
					? Marshal.PtrToStringUni (pair.lpValue)
					: string.Empty;
				app.Add (key, value);
			}
			return app;
		}
		#endregion
		public override object BuildJSON ()
		{
			using (var apps = this)
			{
				return apps.Select (app => {
					var dict = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
					foreach (var kv in app)
					{
						dict [kv.Key] = kv.Value;
						if (ConstData.IsFilePathKey (kv.Key))
						{
							dict [(kv.Key?.Trim () ?? "") + "_Base64"] = app.NewAtBase64 (kv.Key);
						}
						else
						{
							dict [kv.Key] = app.NewAt (kv.Key, m_usePri.Value && m_enablePri.Value) ?? kv.Value;
						}
					}
					dict ["AppUserModelID"] = app.UserModelID;
					return dict;
				}).ToList ();
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRCapabilities: BaseInfoSection, ICapabilities
	{
		public MRCapabilities (ref IntPtr hReader) : base (ref hReader) { }
		public List<string> Capabilities
		{
			get
			{
				var ret = new List<string> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetManifestCapabilitiesList (m_hReader.Value);
				if (hList == IntPtr.Zero) return ret;
				try
				{
					uint count = (uint)Marshal.ReadInt32 (hList);
					int baseOffset = Marshal.SizeOf (typeof (uint)); // dwSize 后
					for (int i = 0; i < count; i++)
					{
						IntPtr pStr = Marshal.ReadIntPtr (hList, baseOffset + i * IntPtr.Size);
						if (pStr == IntPtr.Zero) continue;
						string s = Marshal.PtrToStringUni (pStr);
						if (!string.IsNullOrEmpty (s)) ret.Add (s);
					}
				}
				finally
				{
					PackageReadHelper.DestroyWStringList (hList);
				}
				return ret;
			}
		}
		public List<string> DeviceCapabilities
		{
			get
			{
				var ret = new List<string> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetManifestDeviceCapabilitiesList (m_hReader.Value);
				if (hList == IntPtr.Zero) return ret;
				try
				{
					uint count = (uint)Marshal.ReadInt32 (hList);
					int baseOffset = Marshal.SizeOf (typeof (uint)); // dwSize 后
					for (int i = 0; i < count; i++)
					{
						IntPtr pStr = Marshal.ReadIntPtr (hList, baseOffset + i * IntPtr.Size);
						if (pStr == IntPtr.Zero) continue;
						string s = Marshal.PtrToStringUni (pStr);
						if (!string.IsNullOrEmpty (s)) ret.Add (s);
					}
				}
				finally
				{
					PackageReadHelper.DestroyWStringList (hList);
				}
				return ret;
			}
		}
		public List<string> CapabilityDisplayNames
		{
			get
			{
				var caps = Capabilities;
				var dev = DeviceCapabilities;
				var ret = new List<string> ();
				foreach (var c in caps)
				{
					var capname = CacheData.GetPackageCapabilityDisplayName (c);
					if (String.IsNullOrWhiteSpace (capname)) ret.Add (c);
					else ret.Add (capname);
				}
				foreach (var d in dev)
				{
					var dcapname = CacheData.GetPackageCapabilityDisplayName (d);
					if (!String.IsNullOrWhiteSpace (dcapname)) ret.Add (dcapname);
				}
				return ret;
			}
		}
		public override object BuildJSON ()
		{
			return new
			{
				capabilities_name = Capabilities,
				device_capabilities = DeviceCapabilities,
				scales = CapabilityDisplayNames
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRDependencies: BaseInfoSection, IEnumerable<DependencyInfo>
	{
		public MRDependencies (ref IntPtr hReader) : base (ref hReader) { }
		public List<DependencyInfo> Dependencies
		{
			get
			{
				var output = new List<DependencyInfo> ();
				if (!IsValid) return output;
				IntPtr hList = PackageReadHelper.GetManifestDependencesInfoList (m_hReader);
				if (hList == IntPtr.Zero) return output;
				try
				{
					var deps = PackageReadHelper.ReadDependencyInfoList (hList);
					foreach (var dep in deps)
					{
						// dep.lpName / dep.lpPublisher 是 IntPtr
						string name = Marshal.PtrToStringUni (dep.lpName) ?? "";
						string publisher = Marshal.PtrToStringUni (dep.lpPublisher) ?? "";
						// VERSION 直接映射为 System.Version
						var ver = new DataUtils.Version (dep.verMin.major, dep.verMin.minor, dep.verMin.build, dep.verMin.revision);
						output.Add (new DependencyInfo (name, publisher, ver));
					}
				}
				finally
				{
					PackageReadHelper.DestroyDependencesInfoList (hList);
				}
				return output;
			}
		}
		public DependencyInfo this [int index] => Dependencies [index];
		public IEnumerator<DependencyInfo> GetEnumerator ()
		{
			return Dependencies.GetEnumerator ();
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		public override object BuildJSON ()
		{
			return this.Select (d => new {
				name = d.Name,
				publisher = d.Publisher,
				vermin = d.Version.BuildJSON ()
			}).ToList ();
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class MRResources: BaseInfoSection, IResources
	{
		public MRResources (ref IntPtr hReader) : base (ref hReader) { }
		public List<DXFeatureLevel> DXFeatures
		{
			get
			{
				var ret = new List<DXFeatureLevel> ();
				try
				{
					var dw = PackageReadHelper.GetManifestResourcesDxFeatureLevels (m_hReader);
					if ((dw & 0x1) != 0) ret.Add (DXFeatureLevel.Level9);
					if ((dw & 0x2) != 0) ret.Add (DXFeatureLevel.Level10);
					if ((dw & 0x4) != 0) ret.Add (DXFeatureLevel.Level11);
					if ((dw & 0x8) != 0) ret.Add (DXFeatureLevel.Level12);
				}
				catch (Exception) { }
				return ret;
			}
		}
		public List<string> Languages
		{
			get
			{
				var ret = new List<string> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetManifestResourcesLanguages (m_hReader.Value);
				if (hList == IntPtr.Zero) return ret;
				try
				{
					ret = PackageReadHelper.ReadWStringList (hList).ToList ();
				}
				finally
				{
					PackageReadHelper.DestroyWStringList (hList);
				}
				return ret;
			}
		}
		public List<int> Languages_LCID
		{
			get
			{
				var ret = new List<int> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetManifestResourcesLanguagesToLcid (m_hReader.Value);
				if (hList == IntPtr.Zero) return ret;
				try
				{
					ret = PackageReadHelper.ReadLcidList (hList).ToList ();
				}
				finally
				{
					PackageReadHelper.DestroyResourcesLanguagesLcidList (hList);
				}
				return ret;
			}
		}
		public List<int> Scales
		{
			get
			{
				var ret = new List<int> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetManifestResourcesLanguagesToLcid (m_hReader.Value);
				if (hList == IntPtr.Zero) return ret;
				try
				{
					ret = PackageReadHelper.ReadUInt32List (hList).Select (e => (int)e).ToList ();
				}
				finally
				{
					PackageReadHelper.DestroyUInt32List (hList);
				}
				return ret;
			}
		}
		public override object BuildJSON ()
		{
			return new
			{
				dx_feature_levels = DXFeatures.Select (e => (int)e).ToList (),
				languages = Languages,
				scales = Scales
			};
		}
	}
	public class MRPrerequisites: BaseInfoSection, IPrerequisites
	{
		public MRPrerequisites (ref IntPtr hReader) : base (ref hReader) { }
		protected DataUtils.Version GetVersion (string name)
		{
			PackageReadHelper.VERSION ver;
			bool res = PackageReadHelper.GetManifestPrerequisite (m_hReader, name, out ver);
			if (res) return new DataUtils.Version (ver.major, ver.minor, ver.build, ver.revision);
			else return new DataUtils.Version ();
		}
		protected string GetVersionDescription (string name)
		{
			var ptr = PackageReadHelper.GetManifestPrerequistieSystemVersionName (m_hReader, name);
			return PackageReadHelper.GetStringAndFreeFromPkgRead (ptr) ?? "";
		}
		public string OSMaxVersionDescription { get { return GetVersionDescription ("OSMaxVersionTested"); } }
		public DataUtils.Version OSMaxVersionTested { get { return GetVersion ("OSMaxVersionTested"); } }
		public DataUtils.Version OSMinVersion { get { return GetVersion ("OSMinVersion"); } }
		public string OSMinVersionDescription { get { return GetVersionDescription ("OSMinVersion"); } }
		public override object BuildJSON ()
		{
			return new
			{
				os_min_version = OSMinVersion.BuildJSON (),
				os_min_version_description = OSMinVersionDescription,
				os_max_version_tested = OSMaxVersionTested.BuildJSON (),
				os_max_version_tested_description = OSMaxVersionDescription
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class ManifestReader: IDisposable
	{
		private IntPtr m_hReader = PackageReadHelper.CreateManifestReader ();
		private string m_filePath = string.Empty;
		private bool m_usePRI = false;
		private bool m_enablePRI = false;
		private PriReader m_pri = null;
		public IntPtr Instance => m_hReader;
		public string FileRoot{ get { return Path.GetDirectoryName (m_filePath); } }
		private void InitPri ()
		{
			m_pri?.Dispose ();
			if (!m_usePRI) return;
			#region Get PRI IStream
			switch (Type)
			{
				case PackageType.Appx:
					{
						var pripath = Path.Combine (FileRoot, "resources.pri");
						m_pri = new PriReader (pripath);
					}
					break;
			}
			#endregion
			return;
		}
		public PackageType Type
		{
			get
			{
				var value = PackageReadHelper.GetManifestType (m_hReader);
				switch (value)
				{
					case 0: return PackageType.Unknown;
					case 1: return PackageType.Appx;
					case 2: return PackageType.Bundle;
				}
				return PackageType.Unknown;
			}
		}
		public PackageRole Role
		{
			get
			{
				var value = PackageReadHelper.GetManifestRole (m_hReader);
				switch (value)
				{
					case 0: return PackageRole.Unknown;
					case 1: return PackageRole.Application;
					case 2: return PackageRole.Framework;
					case 3: return PackageRole.Resource;
				}
				return PackageRole.Unknown;
			}
		}
		public bool IsApplicationManifest { get { return Role == PackageRole.Application; } }
		public bool IsValid { get { return m_hReader != IntPtr.Zero && Type != PackageType.Unknown; } }
		/// <summary>使用 PRI，启用后会预先处理 PRI 文件。</summary>
		public bool UsePri { get { return m_usePRI; } set { m_usePRI = value; InitPri (); } }
		/// <summary>允许 PRI，启用后会返回读取的 PRI 文件结果，需保证 UsePri 开启。</summary>
		public bool EnablePri { get { return m_enablePRI; } set { m_enablePRI = value; } }
		public MRIdentity Identity { get { return new MRIdentity (ref m_hReader); } }
		public MRProperties Properties { get { return new MRProperties (ref m_hReader, this, ref m_pri, ref m_usePRI, ref m_enablePRI); } }
		public MRPrerequisites Prerequisites { get { return new MRPrerequisites (ref m_hReader); } }
		public MRResources Resources { get { return new MRResources (ref m_hReader); } }
		public MRApplications Applications { get { return new MRApplications (ref m_hReader, this, ref m_pri, ref m_usePRI, ref m_enablePRI); } }
		public MRCapabilities Capabilities { get { return new MRCapabilities (ref m_hReader); } }
		public MRDependencies Dependencies { get { return new MRDependencies (ref m_hReader); } }
		public void Dispose ()
		{
			var lastvalue = m_usePRI;
			m_usePRI = false;
			InitPri ();
			m_usePRI = lastvalue;
			if (m_hReader != IntPtr.Zero)
			{
				PackageReadHelper.DestroyManifestReader (m_hReader);
				m_hReader = IntPtr.Zero;
			}
		}
		~ManifestReader () { Dispose (); }
		public string FilePath
		{
			get
			{
				return m_filePath;
			}
			set
			{
				PackageReadHelper.LoadManifestFromFile (m_hReader, m_filePath = value);
			}
		}
		public ManifestReader (string filePath) { FilePath = filePath; }
		public ManifestReader () { }
		public string JSONText { get { return BuildJsonText (); } }
		public string BuildJsonText ()
		{
			var obj = BuildJsonObject ();
			return Newtonsoft.Json.JsonConvert.SerializeObject (
				obj,
				Newtonsoft.Json.Formatting.Indented
			);
		}
		public void BuildJsonTextAsync (object callback)
		{
			if (callback == null) return;
			Thread thread = new Thread (() => {
				string json = string.Empty;
				try
				{
					json = BuildJsonText ();
				}
				catch
				{
					json = string.Empty;
				}
				JSHelper.CallJS (callback, json);
			});
			thread.SetApartmentState (ApartmentState.MTA);
			thread.IsBackground = true;
			thread.Start ();
		}
		private object BuildJsonObject ()
		{
			return new
			{
				valid = IsValid,
				filepath = FilePath,
				type = (int)Type,
				role = (int)Role,
				identity = Identity.BuildJSON (),
				properties = Properties.BuildJSON (),
				prerequisites = Prerequisites.BuildJSON (),
				resources = Resources.BuildJSON (),
				capabilities = Capabilities.BuildJSON (),
				dependencies = Dependencies.BuildJSON (),
				applications = Applications.BuildJSON ()
			};
		}
		public static bool AddApplicationItem (string itemName) => PackageReadHelper.AddPackageApplicationItemGetName (itemName);
		public static bool RemoveApplicationItem (string itemName) => PackageReadHelper.RemovePackageApplicationItemGetName (itemName);
	}
}

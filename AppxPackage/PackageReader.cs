using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AppxPackage.Info;
using NativeWrappers;
namespace AppxPackage
{
	internal static partial class ConstData
	{
		public static readonly string [] FilePathItems = new string []
		{
				"LockScreenLogo",
				"Logo",
				"SmallLogo",
				"Square150x150Logo",
				"Square30x30Logo",
				"Square310x310Logo",
				"Square44x44Logo",
				"Square70x70Logo",
				"Square71x71Logo",
				"StartPage",
				"Tall150x310Logo",
				"VisualGroup",
				"WideLogo",
				"Wide310x150Logo",
				"Executable"
		};
		public static bool IsFilePathKey (string key)
		{
			foreach (var i in FilePathItems)
				if ((i?.Trim ()?.ToLower () ?? "") == (key?.Trim ()?.ToLower () ?? ""))
					return true;
			return false;
		}
	}
	internal static partial class CacheData
	{
		private static readonly Dictionary<string, string> g_capnamemap = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// 获取 Capability 的显示名称，带缓存
		/// </summary>
		/// <param name="capName">Capability 名称</param>
		/// <returns>显示名称字符串</returns>
		public static string GetPackageCapabilityDisplayName (string capName)
		{
			if (string.IsNullOrEmpty (capName)) return string.Empty;
			string cached;
			lock (g_capnamemap)
			{
				if (g_capnamemap.TryGetValue (capName, out cached) && !string.IsNullOrEmpty (cached))
					return cached;
			}
			IntPtr ptr = IntPtr.Zero;
			string ret = string.Empty;
			try
			{
				ptr = PackageReadHelper.GetPackageCapabilityDisplayName (capName);
				ret = ptr != IntPtr.Zero ? PackageReadHelper.GetStringAndFreeFromPkgRead (ptr) : string.Empty;
				ptr = IntPtr.Zero; // 已由 GetStringAndFreeFromPkgRead 释放
				lock (g_capnamemap)
				{
					g_capnamemap [capName] = ret;
				}
			}
			catch
			{
				// 再次尝试，不抛异常
				try
				{
					ptr = PackageReadHelper.GetPackageCapabilityDisplayName (capName);
					ret = ptr != IntPtr.Zero ? PackageReadHelper.GetStringAndFreeFromPkgRead (ptr) : string.Empty;
					ptr = IntPtr.Zero;
					lock (g_capnamemap)
					{
						g_capnamemap [capName] = ret;
					}
				}
				catch
				{
					ret = string.Empty;
				}
			}
			return ret;
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class BaseInfoSection: IDisposable, DataUtils.Utilities.IJsonBuild
	{
		protected Ref<IntPtr> m_hReader = IntPtr.Zero;
		public BaseInfoSection (ref IntPtr hReader) { m_hReader = hReader; }
		public bool IsValid { get { return m_hReader != null && m_hReader != IntPtr.Zero; } }
		public void Dispose ()
		{
			try { m_hReader.Set (IntPtr.Zero); } catch (Exception) { }
		}
		public virtual object BuildJSON () { return this; }
		~BaseInfoSection () { Dispose (); }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class BaseInfoSectWithPRI: BaseInfoSection
	{
		protected Ref<PackageReader> m_reader = new Ref<PackageReader> (null);
		protected Ref<PriReaderBundle> m_priBundle = new Ref<PriReaderBundle> (null);
		protected Ref<bool> m_usePri = new Ref<bool> (false);
		protected Ref<bool> m_enablePri = new Ref<bool> (false);
		public BaseInfoSectWithPRI (ref IntPtr hReader, PackageReader reader, ref PriReaderBundle priBundle, ref bool usePri, ref bool enablePri) : base (ref hReader)
		{
			m_reader.Set (reader);
			m_priBundle.Set (priBundle);
			m_usePri.Set (usePri);
			m_enablePri.Set (enablePri);
		}
		public new void Dispose ()
		{
			try { m_reader.Set (null); } catch (Exception) { }
			try { m_priBundle.Set (null); } catch (Exception) { }
			m_usePri = null;
			m_enablePri = null;
			m_hReader = null;
		}
		~BaseInfoSectWithPRI () { Dispose (); }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PRIdentity: BaseInfoSection, Info.IIdentity
	{
		public PRIdentity (ref IntPtr hReader) : base (ref hReader) { }
		protected string StringValue (uint name)
		{
			var ptr = PackageReadHelper.GetPackageIdentityStringValue (m_hReader, name);
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
				PackageReadHelper.GetPackageIdentityArchitecture (m_hReader, out uarch);
				var t = PackageReadHelper.GetPackageType (m_hReader);
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
				PackageReadHelper.GetPackageIdentityVersion (m_hReader, out ver, false);
				return new DataUtils.Version (ver.major, ver.minor, ver.build, ver.revision);
			}
		}
		public DataUtils.Version RealVersion
		{
			get
			{
				PackageReadHelper.VERSION ver = new PackageReadHelper.VERSION ();
				PackageReadHelper.GetPackageIdentityVersion (m_hReader, out ver, true);
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
				version = Version.BuildJSON (),
				realver = RealVersion.BuildJSON ()
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PRProperties: BaseInfoSectWithPRI, Info.IProperties
	{
		public PRProperties (ref IntPtr hReader, PackageReader reader, ref PriReaderBundle priBundle, ref bool usePri, ref bool enablePri) : base (ref hReader, reader, ref priBundle, ref usePri, ref enablePri) { }
		protected string StringValue (string attr)
		{
			var ptr = PackageReadHelper.GetPackagePropertiesStringValue (m_hReader, attr);
			return PackageReadHelper.GetStringAndFreeFromPkgRead (ptr) ?? "";
		}
		protected bool BoolValue (string attr, bool defaultValue = false)
		{
			int ret = 0;
			HRESULT hr = PackageReadHelper.GetPackagePropertiesBoolValue (m_hReader, attr, out ret);
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
						return m_priBundle.Value.String (res) ?? res;
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
					var resvalue = m_priBundle.Value.String (res);
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
				var logopath = Logo;
				switch (PackageReadHelper.GetPackageType (m_hReader))
				{
					case 1:
						{
							IntPtr pic = PackageReadHelper.GetAppxFileFromAppxPackage (m_hReader, logopath);
							try
							{
								IntPtr base64Head = IntPtr.Zero;
								var base64s = PackageReadHelper.StreamToBase64W (pic, null, 0, out base64Head);
								if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
								return PackageReadHelper.GetStringAndFreeFromPkgRead (base64s);
							}
							catch (Exception) { return ""; }
							finally
							{
								if (pic != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (pic);
							}
						}
						break;
					case 2:
						{
							IntPtr pkg = IntPtr.Zero, pic = IntPtr.Zero, pkglang = IntPtr.Zero;
							try
							{
								PackageReadHelper.GetSuitablePackageFromBundle (m_hReader, out pkglang, out pkg);
								if (pkglang != IntPtr.Zero && pkglang != pkg) PackageReadHelper.DestroyAppxFileStream (pkglang);
								pkglang = IntPtr.Zero;
								pic = PackageReadHelper.GetFileFromPayloadPackage (pkg, logopath);
								IntPtr base64Head = IntPtr.Zero;
								var lpstr = PackageReadHelper.StreamToBase64W (pic, null, 0, out base64Head);
								if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
								if (!(lpstr != IntPtr.Zero && !string.IsNullOrEmpty (PackageReadHelper.GetStringFromPkgRead (lpstr))))
								{
									if (lpstr != IntPtr.Zero) PackageReadHelper.GetStringAndFreeFromPkgRead (lpstr);
									IntPtr pkg1 = IntPtr.Zero, pic1 = IntPtr.Zero;
									try
									{
										pkg1 = PackageReadHelper.GetAppxBundleApplicationPackageFile (m_hReader);
										if (pkg1 != IntPtr.Zero)
										{
											pic1 = PackageReadHelper.GetFileFromPayloadPackage (pkg1, logopath);
											lpstr = PackageReadHelper.StreamToBase64W (pic1, null, 0, out base64Head);
											if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
										}
									}
									catch (Exception) { }
									finally
									{
										if (pic1 == pkg1) pkg1 = IntPtr.Zero;
										if (pic1 != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (pic1);
										pic1 = IntPtr.Zero;
										if (pkg1 != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (pkg1);
										pkg1 = IntPtr.Zero;
									}
								}
								return PackageReadHelper.GetStringAndFreeFromPkgRead (lpstr) ?? "";
							}
							catch (Exception) { return ""; }
							finally
							{
								if (pic == pkg) pkg = IntPtr.Zero;
								if (pic != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (pic);
								pic = IntPtr.Zero;
								if (pkg != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (pkg);
								pkg = IntPtr.Zero;
							}
						}
						break;
				}
				return "";
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
	public class PRApplication: Dictionary<string, string>
	{
		protected Ref<IntPtr> m_hReader = IntPtr.Zero;
		protected Ref<PriReaderBundle> m_priBundle = null;
		protected Ref<bool> m_usePri = false;
		protected Ref<bool> m_enablePri = false;
		public PRApplication (ref IntPtr hReader, ref PriReaderBundle priBundle, ref bool usePri, ref bool enablePri) : base (StringComparer.OrdinalIgnoreCase)
		{
			m_hReader = hReader;
			m_priBundle = priBundle;
			m_usePri = usePri;
			m_enablePri = enablePri;
		}
		public PRApplication (ref Ref<IntPtr> m_hReader, ref Ref<PriReaderBundle> m_priBundle, ref Ref<bool> m_usePri, ref Ref<bool> m_enablePri)
		{
			this.m_hReader = m_hReader;
			this.m_priBundle = m_priBundle;
			this.m_usePri = m_usePri;
			this.m_enablePri = m_enablePri;
		}
		public string UserModelID { get { return this ["AppUserModelID"]; } }
		protected bool EnablePri ()
		{
			if (m_priBundle == null || m_priBundle.Value == null) return false;
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
							return m_priBundle.Value.String (res) ?? res;
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
			if (m_priBundle == null || m_priBundle.Value == null) return string.Empty;
			return m_priBundle.Value.Resource (resName);
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
						IntPtr pic = IntPtr.Zero;
						try
						{
							pic = PackageReadHelper.GetAppxFileFromAppxPackage (m_hReader, value);
							IntPtr base64Head = IntPtr.Zero;
							IntPtr lpstr = PackageReadHelper.StreamToBase64W (pic, null, 0, out base64Head);
							if (base64Head != IntPtr.Zero)
							{
								PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head);
								base64Head = IntPtr.Zero;
							}
							return lpstr != IntPtr.Zero
								? PackageReadHelper.GetStringAndFreeFromPkgRead (lpstr)
								: "";
						}
						catch (Exception)
						{
							return "";
						}
						finally
						{
							if (pic != IntPtr.Zero)
							{
								PackageReadHelper.DestroyAppxFileStream (pic);
								pic = IntPtr.Zero;
							}
						}
					}
				case 2: // PKGTYPE_BUNDLE
					{
						IntPtr pkg = IntPtr.Zero;
						IntPtr pic = IntPtr.Zero;
						try
						{
							IntPtr header = IntPtr.Zero;
							PackageReadHelper.GetSuitablePackageFromBundle (m_hReader, out header, out pkg);
							if (header != IntPtr.Zero) PackageReadHelper.GetStringAndFreeFromPkgRead (header);
							header = IntPtr.Zero;
							pic = PackageReadHelper.GetFileFromPayloadPackage (pkg, value);
							IntPtr base64Head = IntPtr.Zero;
							IntPtr lpstr = PackageReadHelper.StreamToBase64W (pic, null, 0, out base64Head);
							if (base64Head != IntPtr.Zero)
							{
								PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head);
								base64Head = IntPtr.Zero;
							}
							return lpstr != IntPtr.Zero
								? PackageReadHelper.GetStringAndFreeFromPkgRead (lpstr)
								: "";
						}
						catch (Exception)
						{
							return "";
						}
						finally
						{
							if (pic == pkg) pkg = IntPtr.Zero;
							if (pic != IntPtr.Zero)
							{
								PackageReadHelper.DestroyAppxFileStream (pic);
								pic = IntPtr.Zero;
							}
							if (pkg != IntPtr.Zero)
							{
								PackageReadHelper.DestroyAppxFileStream (pkg);
								pkg = IntPtr.Zero;
							}
						}
					}
			}
			return "";
		}
		public static bool operator == (PRApplication a, PRApplication b)
		{
			if (ReferenceEquals (a, b)) return true;
			if ((object)a == null || (object)b == null) return false;
			return string.Equals (a.UserModelID, b.UserModelID, StringComparison.OrdinalIgnoreCase);
		}
		public static bool operator != (PRApplication a, PRApplication b)
		{
			return !(a == b);
		}
		public override bool Equals (object obj)
		{
			PRApplication other = obj as PRApplication;
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
	public class PRApplications: BaseInfoSectWithPRI, IEnumerable<PRApplication>
	{
		private IntPtr _hList = IntPtr.Zero;
		private List<PRApplication> _apps;
		public PRApplications (
			ref IntPtr hReader,
			PackageReader reader,
			ref PriReaderBundle priBundle,
			ref bool usePri,
			ref bool enablePri)
			: base (ref hReader, reader, ref priBundle, ref usePri, ref enablePri)
		{
			if (IsValid)
			{
				_hList = PackageReadHelper.GetPackageApplications (m_hReader.Value);
			}
		}
		#region Dispose
		public new void Dispose ()
		{
			if (_hList != IntPtr.Zero)
			{
				PackageReadHelper.DestroyPackageApplications (_hList);
				_hList = IntPtr.Zero;
			}
			base.Dispose ();
		}
		~PRApplications ()
		{
			Dispose ();
		}
		#endregion
		#region 属性：Applications
		public List<PRApplication> Applications
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
		public PRApplication this [int index]
		{
			get { return Applications [index]; }
		}
		public PRApplication this [string key]
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
		public IEnumerator<PRApplication> GetEnumerator ()
		{
			return Applications.GetEnumerator ();
		}
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion
		#region 内部解析逻辑（核心等价 C++ get）
		private List<PRApplication> ReadApplications ()
		{
			var list = new List<PRApplication> ();
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
		private PRApplication ReadSingleApplication (IntPtr hKeyValues)
		{
			var app = new PRApplication (ref m_hReader, ref m_priBundle, ref m_usePri, ref m_enablePri);
			uint pairCount = (uint)Marshal.ReadInt32 (hKeyValues);
			int baseOffset = Marshal.SizeOf (typeof (uint));
			int pairSize = Marshal.SizeOf (typeof (PackageReadHelper.PAIR_PVOID));
			for (int j = 0; j < pairCount; j++)
			{
				IntPtr pPair = IntPtr.Add (hKeyValues, baseOffset + j * pairSize);
				var pair = (PackageReadHelper.PAIR_PVOID) Marshal.PtrToStructure (pPair, typeof (PackageReadHelper.PAIR_PVOID));
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
				return apps.Select (app =>
				{
					var dict = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
					foreach (var kv in app)
					{
						dict [kv.Key] = kv.Value;
						if (ConstData.IsFilePathKey (kv.Key))
						{
							dict [(kv.Key?.Trim () ?? "") + "_Base64"] = app.NewAtBase64 (kv.Key);
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
	public class PRCapabilities: BaseInfoSection, ICapabilities
	{
		public PRCapabilities (ref IntPtr hReader) : base (ref hReader) {}
		public List<string> Capabilities
		{
			get
			{
				var ret = new List<string> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetCapabilitiesList (m_hReader.Value);
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
				IntPtr hList = PackageReadHelper.GetDeviceCapabilitiesList (m_hReader.Value);
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
	public class PRDependencies: BaseInfoSection, IEnumerable<DependencyInfo>
	{
		public PRDependencies (ref IntPtr hReader) : base (ref hReader) { }
		public List<DependencyInfo> Dependencies
		{
			get
			{
				var output = new List<DependencyInfo> ();
				if (!IsValid) return output;
				IntPtr hList = PackageReadHelper.GetDependencesInfoList (m_hReader);
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
			return this.Select (d => new
			{
				name = d.Name,
				publisher = d.Publisher,
				vermin = d.Version.BuildJSON ()
			}).ToList ();
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PRResources: BaseInfoSection, IResources
	{
		public PRResources (ref IntPtr hReader) : base (ref hReader) {}
		public List<DXFeatureLevel> DXFeatures
		{
			get
			{
				var ret = new List<DXFeatureLevel> ();
				try
				{
					var dw = PackageReadHelper.GetResourcesDxFeatureLevels (m_hReader);
					if ((dw & 0x1) != 0) ret.Add (DXFeatureLevel.Level9);
					if ((dw & 0x2) != 0) ret.Add (DXFeatureLevel.Level10);
					if ((dw & 0x4) != 0) ret.Add (DXFeatureLevel.Level11);
					if ((dw & 0x8) != 0) ret.Add (DXFeatureLevel.Level12);
				}
				catch (Exception) { }
				return ret;
			}
		}
		public List <string> Languages
		{
			get
			{
				var ret = new List<string> ();
				if (!IsValid) return ret;
				IntPtr hList = PackageReadHelper.GetResourcesLanguages (m_hReader.Value);
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
				IntPtr hList = PackageReadHelper.GetResourcesLanguagesToLcid (m_hReader.Value);
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
				IntPtr hList = PackageReadHelper.GetResourcesLanguagesToLcid (m_hReader.Value);
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
	public class PRPrerequisites: BaseInfoSection, IPrerequisites
	{
		public PRPrerequisites (ref IntPtr hReader) : base (ref hReader) {}
		protected DataUtils.Version GetVersion (string name)
		{
			PackageReadHelper.VERSION ver;
			bool res = PackageReadHelper.GetPackagePrerequisite (m_hReader, name, out ver);
			if (res) return new DataUtils.Version (ver.major, ver.minor, ver.build, ver.revision);
			else return new DataUtils.Version ();
		}
		protected string GetVersionDescription (string name)
		{
			var ptr = PackageReadHelper.GetPackagePrerequistieSystemVersionName (m_hReader, name);
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
	public class PackageReader: IDisposable
	{
		private IntPtr m_hReader = PackageReadHelper.CreatePackageReader ();
		private string m_filePath = string.Empty;
		private bool m_usePRI = false;
		private bool m_enablePRI = false;
		private PriReaderBundle m_priBundle = new PriReaderBundle ();
		private HashSet<IntPtr> m_priStreams = new HashSet<IntPtr> ();
		public IntPtr Instance => m_hReader;
		private void InitPri ()
		{
			m_priBundle.Dispose ();
			foreach (var i in m_priStreams)
				if (i != IntPtr.Zero)
				{
					PackageReadHelper.DestroyAppxFileStream (i);
				}
			m_priStreams.Clear ();
			if (!m_usePRI) return;
			#region Get PRI IStream
			switch (Type)
			{
				case PackageType.Appx:
					{
						var istream = PackageReadHelper.GetAppxPriFileStream (m_hReader);
						if (istream != IntPtr.Zero)
						{
							m_priStreams.Add (istream);
							m_priBundle.Set (3, istream);
						}
					} break;
				case PackageType.Bundle:
					{
						IntPtr hls = IntPtr.Zero, hss = IntPtr.Zero;
						try
						{
							PackageReadHelper.GetSuitablePackageFromBundle (m_hReader, out hls, out hss);
							IntPtr hlpri = IntPtr.Zero, hspri = IntPtr.Zero;
							try
							{
								hlpri = PackageReadHelper.GetPriFileFromPayloadPackage (hls);
								hspri = PackageReadHelper.GetPriFileFromPayloadPackage (hss);
								IntPtr ls = hls, ss = hss;
								if (ls != IntPtr.Zero && ss != IntPtr.Zero)
								{
									if (ls != IntPtr.Zero) { m_priBundle.Set (1, hlpri); m_priStreams.Add (hlpri); }
									if (ss != IntPtr.Zero) { m_priBundle.Set (2, hspri); m_priStreams.Add (hspri); }
								}
								else if (ls != IntPtr.Zero || ss != IntPtr.Zero)
								{
									if (hlpri != IntPtr.Zero) { m_priBundle.Set (1, hlpri); m_priStreams.Add (hlpri); }
									if (hspri != IntPtr.Zero) { m_priBundle.Set (2, hspri); m_priStreams.Add (hspri); }
									IntPtr hd = IntPtr.Zero;
									try
									{
										hd = PackageReadHelper.GetAppxBundleApplicationPackageFile (m_hReader);
										IntPtr hdpri = PackageReadHelper.GetPriFileFromPayloadPackage (hd);
										if (hd != IntPtr.Zero) { m_priBundle.Set (3, hd); m_priStreams.Add (hd); }
									}
									finally
									{
										if (hd != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (hd);
									}
								}
								else
								{
									IntPtr pkgstream = IntPtr.Zero;
									try
									{
										pkgstream = PackageReadHelper.GetAppxBundleApplicationPackageFile (m_hReader);
										IntPtr pristream = PackageReadHelper.GetPriFileFromPayloadPackage (pkgstream);
										if (pristream != IntPtr.Zero)
										{
											m_priStreams.Add (pristream);
											m_priBundle.Set (3, pristream);
										}
									}
									finally
									{
										if (pkgstream != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (pkgstream);
									}
								}
							}
							finally {}
						}
						finally
						{
							if (hls == hss) hss = IntPtr.Zero;
							if (hls != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (hls);
							if (hss != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (hss);
							IntPtr hlpri = IntPtr.Zero, hspri = IntPtr.Zero;
						}
					} break;
			}
			#endregion
			try
			{
				var resnames = new HashSet<string> ();
				using (var prop = Properties)
				{
					var temp = prop.Description;
					if (PriFileHelper.IsMsResourcePrefix (temp)) resnames.Add (temp);
					temp = prop.DisplayName;
					if (PriFileHelper.IsMsResourcePrefix (temp)) resnames.Add (temp);
					temp = prop.Publisher;
					if (PriFileHelper.IsMsResourcePrefix (temp)) resnames.Add (temp);
					resnames.Add (prop.Logo);
				}
				using (var apps = Applications)
				{
					foreach (var app in apps)
					{
						foreach (var pair in app)
						{
							foreach (var pathres in ConstData.FilePathItems)
							{
								if ((pathres?.Trim ()?.ToLower () ?? "") == (pair.Key?.Trim ()?.ToLower ()))
								{
									resnames.Add (pair.Value);
								}
								else if (PriFileHelper.IsMsResourcePrefix (pair.Value))
									resnames.Add (pair.Value);
							}
						}
					}
				}
				m_priBundle.AddSearch (resnames);
			}
			catch (Exception) { }
		}
		public PackageType Type
		{
			get
			{
				var value = PackageReadHelper.GetPackageType (m_hReader);
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
				var value = PackageReadHelper.GetPackageRole (m_hReader);
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
		public bool IsApplicationPackage { get { return Role == PackageRole.Application; } }
		public bool IsValid { get { return m_hReader != IntPtr.Zero && Type != PackageType.Unknown; } }
		/// <summary>使用 PRI，启用后会预先处理 PRI 文件。</summary>
		public bool UsePri { get { return m_usePRI; } set { m_usePRI = value; InitPri (); } }
		/// <summary>允许 PRI，启用后会返回读取的 PRI 文件结果，需保证 UsePri 开启。</summary>
		public bool EnablePri { get { return m_enablePRI; } set { m_enablePRI = value; } }
		public PRIdentity Identity { get { return new PRIdentity (ref m_hReader); } }
		public PRProperties Properties { get { return new PRProperties (ref m_hReader, this, ref m_priBundle, ref m_usePRI, ref m_enablePRI); } }
		public PRPrerequisites Prerequisites { get { return new PRPrerequisites (ref m_hReader); } }
		public PRResources Resources { get { return new PRResources (ref m_hReader); } }
		public PRApplications Applications { get { return new PRApplications (ref m_hReader, this, ref m_priBundle, ref m_usePRI, ref m_enablePRI); } }
		public PRCapabilities Capabilities { get { return new PRCapabilities (ref m_hReader); } }
		public PRDependencies Dependencies { get { return new PRDependencies (ref m_hReader); } }
		public void Dispose ()
		{
			if (m_hReader != IntPtr.Zero)
			{
				PackageReadHelper.DestroyPackageReader (m_hReader);
				m_hReader = IntPtr.Zero;
			}
			var lastvalue = m_usePRI;
			m_usePRI = false;
			InitPri ();
			m_usePRI = lastvalue;
		}
		~PackageReader () { Dispose (); }
		public string FilePath
		{
			get
			{
				return m_filePath;
			}
			set
			{
				PackageReadHelper.LoadPackageFromFile (m_hReader, m_filePath = value);
			}
		}
		public PackageReader (string filePath) { FilePath = filePath; }
		public PackageReader () { }
		public string JSONText { get { return BuildJsonText (); } }
		public string BuildJsonText ()
		{
			var obj = BuildJsonObject ();
			return Newtonsoft.Json.JsonConvert.SerializeObject (
				obj,
				Newtonsoft.Json.Formatting.Indented
			);
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
	}
}

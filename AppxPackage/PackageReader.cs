using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Dynamic;
using AppxPackage.Info;
using NativeWrappers;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib;
using System.IO;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
//using PriFormat;
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
				"WideLogo",
				"Wide310x150Logo",
				"Executable"
		};
		public static readonly string [] ImageFilePathItems = new string []
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
				"Tall150x310Logo",
				"WideLogo",
				"Wide310x150Logo"
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
	internal class PriAllValuesReader: IDisposable
	{
		public PackageReader pr = null;
		private Dictionary<IntPtr, IntPtr> pkgAppDict = new Dictionary<IntPtr, IntPtr> ();
		private Dictionary<IntPtr, IntPtr> pkgLocDict = new Dictionary<IntPtr, IntPtr> ();
		private Dictionary<IntPtr, IntPtr> pkgScaDict = new Dictionary<IntPtr, IntPtr> ();
		private Dictionary<IntPtr, PriReader> priAppPkg = new Dictionary<IntPtr, PriReader> ();
		private Dictionary<IntPtr, PriReader> priLocPkg = new Dictionary<IntPtr, PriReader> ();
		private Dictionary<IntPtr, PriReader> priScaPkg = new Dictionary<IntPtr, PriReader> ();
		public PriAllValuesReader (PackageReader p_pr)
		{
			pr = p_pr;
		}
		public void Init ()
		{
			var hPkg = pr.Instance;
			var pkgType = pr.Type;
			if (pkgType == PackageType.Appx)
			{
				var priinst = PackageReadHelper.GetAppxFileFromAppxPackage (hPkg, "resources.pri");
				pkgAppDict [IntPtr.Zero] = priinst;
				priAppPkg [priinst] = (new PriReader (priinst));
			}
			else if (pkgType == PackageType.Bundle)
			{
				var appPkg = PackageReadHelper.GetAppxBundleApplicationPackageFile (hPkg);
				var appPkgPri = PackageReadHelper.GetFileFromPayloadPackage (appPkg, "resources.pri");
				pkgAppDict [appPkg] = appPkgPri;
				priAppPkg [appPkgPri] = new PriReader (appPkgPri);
				var localeResPkgList = PackageReadHelper.GetAppxBundleAllLocaleResourcePackageFileNameList (hPkg);
				foreach (var filename in localeResPkgList)
				{
					var localePkg = PackageReadHelper.GetAppxBundlePayloadPackageFile (hPkg, filename);
					var localePkgPri = PackageReadHelper.GetFileFromPayloadPackage (localePkg, "resources.pri");
					pkgLocDict [localePkg] = localePkgPri;
					priLocPkg [localePkg] = (new PriReader (localePkgPri));
				}
				var fileResPkgList = PackageReadHelper.GetAppxBundleAllFileResourcePackageFileNameList (hPkg);
				foreach (var filename in fileResPkgList)
				{
					var filePkg = PackageReadHelper.GetAppxBundlePayloadPackageFile (hPkg, filename);
					var filePkgPri = PackageReadHelper.GetFileFromPayloadPackage (filePkg, "resources.pri");
					pkgScaDict [filePkg] = filePkgPri;
					priScaPkg [filePkgPri] = (new PriReader (filePkgPri));
				}
			}
		}
		public void Dispose ()
		{
			if (pr == null) return;
			foreach (var i in priAppPkg) if (i.Value != null) i.Value.Dispose ();
			foreach (var i in priLocPkg) if (i.Value != null) i.Value.Dispose ();
			foreach (var i in priScaPkg) if (i.Value != null) i.Value.Dispose ();
			priAppPkg.Clear ();
			priLocPkg.Clear ();
			priScaPkg.Clear ();
			priAppPkg = null;
			priLocPkg = null;
			priScaPkg = null;
			foreach (var kv in pkgAppDict)
			{
				if (kv.Value != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (kv.Value);
				if (kv.Key != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (kv.Key);
			}
			foreach (var kv in pkgLocDict)
			{
				if (kv.Value != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (kv.Value);
				if (kv.Key != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (kv.Key);
			}
			foreach (var kv in pkgScaDict)
			{
				if (kv.Value != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (kv.Value);
				if (kv.Key != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (kv.Key);
			}
			pkgAppDict.Clear ();
			pkgLocDict.Clear ();
			pkgScaDict.Clear ();
			pkgAppDict = null;
			pkgLocDict = null;
			pkgScaDict = null;
			pr = null;
		}
		/// <summary>
		/// 获取某个本地化字符串资源的所有语言资源
		/// </summary>
		/// <param name="resid">资源 ID，一般为“ms-resource:”开头</param>
		/// <returns>返回以区域代码为键，本地化字符串为值的字典</returns>
		public Dictionary<string, string> LocaleResourceAllValues (string resid)
		{
			var dict = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			foreach (var pri in priAppPkg)
			{
				foreach (var kv in pri.Value.LocaleResourceAllValue (resid))
				{
					dict [kv.Key] = kv.Value;
				}
			}
			foreach (var pri in priLocPkg)
			{
				foreach (var kv in pri.Value.LocaleResourceAllValue (resid))
				{
					dict [kv.Key] = kv.Value;
				}
			}
			return dict;
		}
		/// <summary>
		/// 获取某个涉及缩放资源的文件名及其文件的 Base64。
		/// </summary>
		/// <param name="resid">资源 ID，一般为文件名，如“Assert\LargeTile.png”</param>
		/// <returns>返回以缩放比和高对比度为键，一个前者为文件名，后者为 data url 组成的元组为值的字典</returns>
		public Dictionary<PriResourceKey, Tuple<string, string>> FileResourceAllValues (string resid)
		{
			var dict = new Dictionary<PriResourceKey, Tuple<string, string>> ();
			var rePkgDict = new Dictionary<IntPtr, IntPtr> ();
			foreach (var kv in pkgAppDict)
				rePkgDict [kv.Value] = kv.Key;
			foreach (var pri in priAppPkg)
			{
				var pkg = rePkgDict [pri.Key];
				#region appx
				if (pr.Type == PackageType.Appx)
				{
					foreach (var kv in pri.Value.PathResourceAllValue (resid))
					{
						var imgInst = PackageReadHelper.GetAppxFileFromAppxPackage (pr.Instance, kv.Value);
						IntPtr b64ph;
						var b64p = PackageReadHelper.StreamToBase64W (imgInst, null, 0, out b64ph);
						var imgB64 = "";
						if (b64p != IntPtr.Zero) imgB64 = Marshal.PtrToStringUni (b64p);
						if (b64p != IntPtr.Zero) PackageReadHelper.PackageReaderFreeString (b64p);
						dict [kv.Key] = new Tuple<string, string> (kv.Value, imgB64);
					}
				}
				#endregion
				#region bundle
				else if (pr.Type == PackageType.Bundle)
				{
					foreach (var kv in pri.Value.PathResourceAllValue (resid))
					{
						var imgInst = PackageReadHelper.GetFileFromPayloadPackage (pkg, kv.Value);
						IntPtr b64ph;
						var b64p = PackageReadHelper.StreamToBase64W (imgInst, null, 0, out b64ph);
						var imgB64 = "";
						if (b64p != IntPtr.Zero) imgB64 = Marshal.PtrToStringUni (b64p);
						if (b64p != IntPtr.Zero) PackageReadHelper.PackageReaderFreeString (b64p);
						dict [kv.Key] = new Tuple<string, string> (kv.Value, imgB64);
					}
				}
				#endregion
			}
			rePkgDict.Clear ();
			foreach (var kv in pkgScaDict) rePkgDict [kv.Value] = kv.Key;
			foreach (var pri in priScaPkg)
			{
				var pkg = rePkgDict [pri.Key];
				foreach (var kv in pri.Value.PathResourceAllValue (resid))
				{
					var imgInst = PackageReadHelper.GetFileFromPayloadPackage (pkg, kv.Value);
					IntPtr b64ph;
					var b64p = PackageReadHelper.StreamToBase64W (imgInst, null, 0, out b64ph);
					var imgB64 = "";
					if (b64p != IntPtr.Zero) imgB64 = Marshal.PtrToStringUni (b64p);
					if (b64p != IntPtr.Zero) PackageReadHelper.PackageReaderFreeString (b64p);
					dict [kv.Key] = new Tuple<string, string> (kv.Value, imgB64);
				}
			}
			return dict;
		}
		private static byte [] Base64ToBytes (string base64)
		{
			int commaIndex = base64.IndexOf (',');
			if (commaIndex >= 0)
			{
				base64 = base64.Substring (commaIndex + 1);
			}
			try
			{
				return Convert.FromBase64String (base64);
			}
			catch
			{
				return new byte [0];
			}
		}
		internal class TrimIgnoreCaseComparer: IEqualityComparer<string>
		{
			public bool Equals (string x, string y)
			{
				if (x == null && y == null) return true;
				if (x == null || y == null) return false;
				return string.Equals (x.Trim (), y.Trim (), StringComparison.OrdinalIgnoreCase);
			}
			public int GetHashCode (string obj)
			{
				if (obj == null) return 0;
				return obj.Trim ().ToLowerInvariant ().GetHashCode ();
			}
		}
		private HashSet<string> set = new HashSet<string> (new TrimIgnoreCaseComparer ());
		public Dictionary<PriResourceKey, string> FileResourceAllValues (string resid, ZipOutputStream zos)
		{
			var comparer = new TrimIgnoreCaseComparer ();
			var dict = new Dictionary<PriResourceKey, string> ();
			var rePkgDict = new Dictionary<IntPtr, IntPtr> ();
			foreach (var kv in pkgAppDict)
				rePkgDict [kv.Value] = kv.Key;
			foreach (var pri in priAppPkg)
			{
				var pkg = rePkgDict [pri.Key];
				#region appx
				if (pr.Type == PackageType.Appx)
				{
					foreach (var kv in pri.Value.PathResourceAllValue (resid))
					{
						dict [kv.Key] = kv.Value;
						if (set.Contains (kv.Value, comparer)) continue;
						else set.Add (kv.Value);
						var imgInst = PackageReadHelper.GetAppxFileFromAppxPackage (pr.Instance, kv.Value);
						IntPtr b64ph;
						var b64p = PackageReadHelper.StreamToBase64W (imgInst, null, 0, out b64ph);
						var imgB64 = "";
						if (b64p != IntPtr.Zero) imgB64 = Marshal.PtrToStringUni (b64p);
						if (b64p != IntPtr.Zero) PackageReadHelper.PackageReaderFreeString (b64p);
						if (zos != null)
						{
							var entryPath = kv.Value.Replace ('\\', '/');
							var ze = new ZipEntry (entryPath);
							ze.DateTime = DateTime.Now;
							zos.PutNextEntry (ze);
							var bytes = Base64ToBytes (imgB64);
							zos.Write (bytes, 0, bytes.Length);
							zos.CloseEntry ();
						}
					}
				}
				#endregion
				#region bundle
				else if (pr.Type == PackageType.Bundle)
				{
					foreach (var kv in pri.Value.PathResourceAllValue (resid))
					{
						dict [kv.Key] = kv.Value;
						if (set.Contains (kv.Value, comparer)) continue;
						else set.Add (kv.Value);
						var imgInst = PackageReadHelper.GetFileFromPayloadPackage (pkg, kv.Value);
						IntPtr b64ph;
						var b64p = PackageReadHelper.StreamToBase64W (imgInst, null, 0, out b64ph);
						var imgB64 = "";
						if (b64p != IntPtr.Zero) imgB64 = Marshal.PtrToStringUni (b64p);
						if (b64p != IntPtr.Zero) PackageReadHelper.PackageReaderFreeString (b64p);
						if (zos != null)
						{
							var ze = new ZipEntry (kv.Value.Replace ('\\', '/'));
							ze.DateTime = DateTime.Now;
							zos.PutNextEntry (ze);
							var bytes = Base64ToBytes (imgB64);
							zos.Write (bytes, 0, bytes.Length);
							zos.CloseEntry ();
						}
					}
				}
				#endregion
			}
			rePkgDict.Clear ();
			foreach (var kv in pkgScaDict) rePkgDict [kv.Value] = kv.Key;
			foreach (var pri in priScaPkg)
			{
				var pkg = rePkgDict [pri.Key];
				foreach (var kv in pri.Value.PathResourceAllValue (resid))
				{
					dict [kv.Key] = kv.Value;
					if (set.Contains (kv.Value, comparer)) continue;
					else set.Add (kv.Value);
					var imgInst = PackageReadHelper.GetFileFromPayloadPackage (pkg, kv.Value);
					IntPtr b64ph;
					var b64p = PackageReadHelper.StreamToBase64W (imgInst, null, 0, out b64ph);
					var imgB64 = "";
					if (b64p != IntPtr.Zero) imgB64 = Marshal.PtrToStringUni (b64p);
					if (b64p != IntPtr.Zero) PackageReadHelper.PackageReaderFreeString (b64p);
					if (zos != null)
					{
						var ze = new ZipEntry (kv.Value.Replace ('\\', '/'));
						ze.DateTime = DateTime.Now;
						zos.PutNextEntry (ze);
						var bytes = Base64ToBytes (imgB64);
						zos.Write (bytes, 0, bytes.Length);
						zos.CloseEntry ();
					}
				}
			}
			rePkgDict.Clear ();
			return dict;
		}
		~PriAllValuesReader ()
		{
			set?.Clear ();
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
			return new {
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
#if DEBUG
		public string Debug_StringValue (string attr) => StringValue (attr);
#endif
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
			//var id = new PriFormat.PriResourceIdentifier (res);
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
								//if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
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
								//if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
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
											//if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
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
			return new {
				display_name = DisplayName,
				description = Description,
				publisher_display_name = Publisher,
				Framework = Framework,
				framework = Framework,
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
		protected bool IsImageFilePathKey (string key)
		{
			foreach (var i in ConstData.ImageFilePathItems)
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
			if (IsImageFilePathKey (key) && !string.IsNullOrEmpty (value))
			{
				string pri = PriGetRes (value);
				return string.IsNullOrEmpty (pri) ? value : pri;
			}
			return value;
		}
		public string NewAtBase64 (string key)
		{
			string value = NewAt (key, true);
			if (!IsImageFilePathKey (key) || string.IsNullOrEmpty (value)) return "";
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
							//if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
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
							//if (header != IntPtr.Zero) PackageReadHelper.GetStringAndFreeFromPkgRead (header);
							header = IntPtr.Zero;
							pic = PackageReadHelper.GetFileFromPayloadPackage (pkg, value);
							IntPtr base64Head = IntPtr.Zero;
							IntPtr lpstr = PackageReadHelper.StreamToBase64W (pic, null, 0, out base64Head);
							//if (base64Head != IntPtr.Zero) { PackageReadHelper.GetStringAndFreeFromPkgRead (base64Head); base64Head = IntPtr.Zero; }
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
	public class PRCapabilities: BaseInfoSection, ICapabilities
	{
		public PRCapabilities (ref IntPtr hReader) : base (ref hReader) { }
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
			return new {
				capabilities_name = Capabilities,
				device_capabilities = DeviceCapabilities,
				display_capabilities_name = CapabilityDisplayNames
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
			return this.Select (d => new {
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
		public PRResources (ref IntPtr hReader) : base (ref hReader) { }
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
		public List<string> Languages
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
				IntPtr hList = PackageReadHelper.GetResourcesScales (m_hReader.Value);
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
			return new {
				dx_feature_levels = DXFeatures.Select (e => (int)e).ToList (),
				languages = Languages,
				scales = Scales
			};
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PRPrerequisites: BaseInfoSection, IPrerequisites
	{
		public PRPrerequisites (ref IntPtr hReader) : base (ref hReader) { }
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
			return new {
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
					}
					break;
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
							finally { }
						}
						finally
						{
							if (hls == hss) hss = IntPtr.Zero;
							if (hls != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (hls);
							if (hss != IntPtr.Zero) PackageReadHelper.DestroyAppxFileStream (hss);
							IntPtr hlpri = IntPtr.Zero, hspri = IntPtr.Zero;
						}
					}
					break;
			}
			#endregion
			return;
		}
#if DEBUG
		public PriReaderBundle PriInstance => m_priBundle;
#endif
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
			return new {
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
		public static string [] GetApplicationItems () => PackageReadHelper.GetApplicationItemNames ();
		public static void UpdateApplicationItems (IEnumerable<string> items) => PackageReadHelper.SetApplicationItemNames (items);
		public string BuildJsonForUse ()
		{
			bool parsePri = UsePri;
			bool usePri = EnablePri;
			try
			{
				UsePri = false;
				EnablePri = false;
				object packageInfo = null;
				#region file
				var typestr = "unknown";
				switch (Type)
				{
					case PackageType.Appx: typestr = "appx"; break;
					case PackageType.Bundle: typestr = "bundle"; break;
					default:
					case PackageType.Unknown: typestr = "unknown"; break;
				}
				var rolestr = "unknown";
				switch (Role)
				{
					case PackageRole.Application: rolestr = "application"; break;
					case PackageRole.Framework: rolestr = "framework"; break;
					case PackageRole.Resource: rolestr = "resource"; break;
					default:
					case PackageRole.Unknown: rolestr = "unknown"; break;
				}
				var pkgfile = new {
					path = FilePath,
					valid = IsValid,
					type = typestr,
					role = rolestr
				};
				#endregion
				#region id
				var id = Identity;
				var pkgid = new {
					name = id.Name,
					publisher = id.Publisher,
					version = id.Version.ToString (),
					realVersion = id.RealVersion.ToString (),
					architecture = id.ProcessArchitecture.Select (e => {
						switch (e)
						{
							case Architecture.ARM: return "arm";
							case Architecture.ARM64: return "arm64";
							case Architecture.Neutral: return "neutral";
							case Architecture.x64: return "x64";
							case Architecture.x86: return "x86";
							default:
							case Architecture.Unknown: return "unknown";
						}
					}).ToList (),
					familyName = id.FamilyName,
					fullName = id.FullName
				};
				#endregion
				#region prerequistes
				var preq = Prerequisites;
				var pkgpreq = new {
					osMinVersion = preq.OSMinVersion.ToString (),
					osMaxVersionTested = preq.OSMaxVersionTested.ToString (),
					osMinVersionDescription = preq.OSMinVersionDescription,
					osMaxVersionTestedDescription = preq.OSMaxVersionDescription
				};
				#endregion
				#region resources
				var res = Resources;
				var pkgres = new {
					languages = res.Languages,
					scales = res.Scales,
					dxFeatureLevels = res.DXFeatures.Select (d => {
						switch (d)
						{
							case DXFeatureLevel.Level9: return 9;
							case DXFeatureLevel.Level10: return 10;
							case DXFeatureLevel.Level11: return 11;
							case DXFeatureLevel.Level12: return 12;
							default:
							case DXFeatureLevel.Unspecified: return -1;
						}
					}).ToList ()
				};
				#endregion
				#region capabilities
				var caps = Capabilities;
				var pkgcaps = new {
					capabilities = caps.Capabilities,
					deviceCapabilities = caps.DeviceCapabilities
				};
				#endregion
				#region dependencies
				var deps = Dependencies;
				var pkgdeps = Dependencies.Select (d => new {
					name = d.Name,
					publisher = d.Publisher,
					minVersion = d.Version.ToString ()
				}).ToList ();
				#endregion
				using (var priAllRes = new PriAllValuesReader (this))
				{
					priAllRes.Init ();
					#region prop
					var prop = Properties;
					var dispname = prop.DisplayName;
					var dispNameDict = new Dictionary<string, string> ();
					if (PriFileHelper.IsMsResourcePrefix (dispname))
						dispNameDict = priAllRes.LocaleResourceAllValues (dispname);
					dispNameDict ["root"] = dispname;
					var desc = prop.Description;
					var descDict = new Dictionary<string, string> ();
					if (PriFileHelper.IsMsResourcePrefix (desc))
						descDict = priAllRes.LocaleResourceAllValues (desc);
					descDict ["root"] = desc;
					var disppub = prop.Publisher;
					var dispPubDict = new Dictionary<string, string> ();
					if (PriFileHelper.IsMsResourcePrefix (disppub))
						dispPubDict = priAllRes.LocaleResourceAllValues (disppub);
					dispPubDict ["root"] = disppub;
					var logoList = priAllRes.FileResourceAllValues (prop.Logo)
						.Select (kv => {
							string contrast = "";
							switch (kv.Key.Contrast)
							{
								case PriResourceKey.PriContrast.None: contrast = ""; break;
								case PriResourceKey.PriContrast.Black: contrast = "black"; break;
								case PriResourceKey.PriContrast.White: contrast = "white"; break;
								case PriResourceKey.PriContrast.High: contrast = "high"; break;
								case PriResourceKey.PriContrast.Low: contrast = "low"; break;
							}

							if (kv.Key.IsTargetSize)
							{
								return new {
									targetSize = kv.Key.Value,
									contrast = contrast,
									name = kv.Value.Item1,
									base64 = kv.Value.Item2
								};
							}
							else if (kv.Key.IsScale)
							{
								return new {
									scale = kv.Key.Value,
									contrast = contrast,
									name = kv.Value.Item1,
									base64 = kv.Value.Item2
								};
							}
							else if (kv.Key.IsString)
							{
								return new {
									language = kv.Key.Value,
									name = kv.Value.Item1,
									base64 = kv.Value.Item2
								};
							}
							else
							{
								return (dynamic)null;   // 不满足条件时返回 null
							}
						})
						.Where (item => item != null)   // 过滤掉 null
						.ToList ();
					var pkgprop = new {
						displayName = dispNameDict,
						publisherDisplayName = dispPubDict,
						description = descDict,
						logo = logoList,
						framework = prop.Framework,
						resourcePackage = prop.ResourcePackage
					};
					#endregion
					#region apps
					var apps = Applications;
					var pkgapps = new List<object> ();
					foreach (var app in apps)
					{
						dynamic obj = new ExpandoObject ();
						var dict = (IDictionary<string, object>)obj;
						foreach (var kv in app)
						{
							if (Utils.AppFileProperties.Contains (kv.Key, StringComparer.OrdinalIgnoreCase))
							{
								dict [Utils.PascalToCamel (kv.Key)] = priAllRes.FileResourceAllValues (kv.Value)
									.Select (skv => {
										string contrast = "";
										switch (skv.Key.Contrast)
										{
											case PriResourceKey.PriContrast.None: contrast = ""; break;
											case PriResourceKey.PriContrast.Black: contrast = "black"; break;
											case PriResourceKey.PriContrast.White: contrast = "white"; break;
											case PriResourceKey.PriContrast.High: contrast = "high"; break;
											case PriResourceKey.PriContrast.Low: contrast = "low"; break;
										}

										if (skv.Key.IsTargetSize)
										{
											return new {
												targetSize = skv.Key.Value,
												contrast = contrast,
												name = skv.Value.Item1,
												base64 = skv.Value.Item2
											};
										}
										else if (skv.Key.IsScale)
										{
											return new {
												scale = skv.Key.Value,
												contrast = contrast,
												name = skv.Value.Item1,
												base64 = skv.Value.Item2
											};
										}
										else if (skv.Key.IsString)
										{
											return new {
												language = skv.Key.Value,
												name = skv.Value.Item1,
												base64 = skv.Value.Item2
											};
										}
										else
										{
											return (dynamic)null;   // 不满足条件时返回 null
										}
									}).Where (item => item != null).ToList ();
							}
							else
							{
								if (PriFileHelper.IsMsResourcePrefix (kv.Value))
								{
									var itemDict = new Dictionary<string, string> ();
									itemDict = priAllRes.LocaleResourceAllValues (kv.Value);
									itemDict ["root"] = kv.Value;
									dict [Utils.PascalToCamel (kv.Key)] = itemDict;
								}
								else
								{
									dict [Utils.PascalToCamel (kv.Key)] = kv.Value;
								}
							}
						}
						pkgapps.Add (obj);
					}
					#endregion
					packageInfo = new {
						file = pkgfile,
						identity = pkgid,
						properties = pkgprop,
						resources = pkgres,
						prerequisites = pkgpreq,
						applications = pkgapps,
						capabilities = pkgcaps,
						dependencies = pkgdeps
					};
				}
				return Newtonsoft.Json.JsonConvert.SerializeObject (
					packageInfo,
					Newtonsoft.Json.Formatting.Indented
				);
			}
			finally
			{
				EnablePri = usePri;
				UsePri = parsePri;
			}
		}
		public void SaveJsonFile (string savefilepath, object resolve, object reject)
		{
			try
			{
				#region
				using (var fs = File.Create (savefilepath))
				{
					using (var zos = new ZipOutputStream (fs))
					{
						zos.SetLevel (9);
						bool parsePri = UsePri;
						bool usePri = EnablePri;
						try
						{
							UsePri = false;
							EnablePri = false;
							object packageInfo = null;
							#region file
							var typestr = "unknown";
							switch (Type)
							{
								case PackageType.Appx: typestr = "appx"; break;
								case PackageType.Bundle: typestr = "bundle"; break;
								default:
								case PackageType.Unknown: typestr = "unknown"; break;
							}
							var rolestr = "unknown";
							switch (Role)
							{
								case PackageRole.Application: rolestr = "application"; break;
								case PackageRole.Framework: rolestr = "framework"; break;
								case PackageRole.Resource: rolestr = "resource"; break;
								default:
								case PackageRole.Unknown: rolestr = "unknown"; break;
							}
							var pkgfile = new {
								path = FilePath,
								valid = IsValid,
								type = typestr,
								role = rolestr
							};
							#endregion
							#region id
							var id = Identity;
							var pkgid = new {
								name = id.Name,
								publisher = id.Publisher,
								version = id.Version.ToString (),
								realVersion = id.RealVersion.ToString (),
								architecture = id.ProcessArchitecture.Select (e => {
									switch (e)
									{
										case Architecture.ARM: return "arm";
										case Architecture.ARM64: return "arm64";
										case Architecture.Neutral: return "neutral";
										case Architecture.x64: return "x64";
										case Architecture.x86: return "x86";
										default:
										case Architecture.Unknown: return "unknown";
									}
								}).ToList (),
								familyName = id.FamilyName,
								fullName = id.FullName,
								resourceId = id.ResourceId
							};
							#endregion
							#region prerequistes
							var preq = Prerequisites;
							var pkgpreq = new {
								osMinVersion = preq.OSMinVersion.ToString (),
								osMaxVersionTested = preq.OSMaxVersionTested.ToString (),
								osMinVersionDescription = preq.OSMinVersionDescription,
								osMaxVersionTestedDescription = preq.OSMaxVersionDescription
							};
							#endregion
							#region resources
							var res = Resources;
							var pkgres = new {
								languages = res.Languages,
								scales = res.Scales,
								dxFeatureLevels = res.DXFeatures.Select (d => {
									switch (d)
									{
										case DXFeatureLevel.Level9: return 9;
										case DXFeatureLevel.Level10: return 10;
										case DXFeatureLevel.Level11: return 11;
										case DXFeatureLevel.Level12: return 12;
										default:
										case DXFeatureLevel.Unspecified: return -1;
									}
								}).ToList ()
							};
							#endregion
							#region capabilities
							var caps = Capabilities;
							var pkgcaps = new {
								capabilities = caps.Capabilities,
								deviceCapabilities = caps.DeviceCapabilities
							};
							#endregion
							#region dependencies
							var deps = Dependencies;
							var pkgdeps = Dependencies.Select (d => new {
								name = d.Name,
								publisher = d.Publisher,
								minVersion = d.Version.ToString ()
							}).ToList ();
							#endregion
							using (var priAllRes = new PriAllValuesReader (this))
							{
								priAllRes.Init ();
								#region prop
								var prop = Properties;
								var dispname = prop.DisplayName;
								var dispNameDict = new Dictionary<string, string> ();
								if (PriFileHelper.IsMsResourcePrefix (dispname))
									dispNameDict = priAllRes.LocaleResourceAllValues (dispname);
								dispNameDict ["root"] = dispname;
								var desc = prop.Description;
								var descDict = new Dictionary<string, string> ();
								if (PriFileHelper.IsMsResourcePrefix (desc))
									descDict = priAllRes.LocaleResourceAllValues (desc);
								descDict ["root"] = desc;
								var disppub = prop.Publisher;
								var dispPubDict = new Dictionary<string, string> ();
								if (PriFileHelper.IsMsResourcePrefix (disppub))
									dispPubDict = priAllRes.LocaleResourceAllValues (disppub);
								dispPubDict ["root"] = disppub;
								var logoList = priAllRes.FileResourceAllValues (prop.Logo, zos)
									.Select (kv => {
										string contrast = "";
										switch (kv.Key.Contrast)
										{
											case PriResourceKey.PriContrast.None: contrast = ""; break;
											case PriResourceKey.PriContrast.Black: contrast = "black"; break;
											case PriResourceKey.PriContrast.White: contrast = "white"; break;
											case PriResourceKey.PriContrast.High: contrast = "high"; break;
											case PriResourceKey.PriContrast.Low: contrast = "low"; break;
										}

										if (kv.Key.IsTargetSize)
										{
											return new {
												targetSize = kv.Key.Value,
												contrast = contrast,
												name = kv.Value
											};
										}
										else if (kv.Key.IsScale)
										{
											return new {
												scale = kv.Key.Value,
												contrast = contrast,
												name = kv.Value
											};
										}
										else if (kv.Key.IsString)
										{
											return new {
												language = kv.Key.Value,
												name = kv.Value
											};
										}
										else
										{
											return (dynamic)null;   // 不满足条件时返回 null
										}
									})
									.Where (item => item != null)   // 过滤掉 null
									.ToList ();
								var pkgprop = new {
									displayName = dispNameDict,
									publisherDisplayName = dispPubDict,
									description = descDict,
									logo = logoList,
									framework = prop.Framework,
									resourcePackage = prop.ResourcePackage
								};
								#endregion
								#region apps
								var apps = Applications;
								var pkgapps = new List<object> ();
								foreach (var app in apps)
								{
									dynamic obj = new ExpandoObject ();
									var dict = (IDictionary<string, object>)obj;
									foreach (var kv in app)
									{
										if (Utils.AppFileProperties.Contains (kv.Key, StringComparer.OrdinalIgnoreCase))
										{
											dict [Utils.PascalToCamel (kv.Key)] = priAllRes.FileResourceAllValues (kv.Value, zos)
												.Select (skv => {
													string contrast = "";
													switch (skv.Key.Contrast)
													{
														case PriResourceKey.PriContrast.None: contrast = ""; break;
														case PriResourceKey.PriContrast.Black: contrast = "black"; break;
														case PriResourceKey.PriContrast.White: contrast = "white"; break;
														case PriResourceKey.PriContrast.High: contrast = "high"; break;
														case PriResourceKey.PriContrast.Low: contrast = "low"; break;
													}

													if (skv.Key.IsTargetSize)
													{
														return new {
															targetSize = skv.Key.Value,
															contrast = contrast,
															name = skv.Value
														};
													}
													else if (skv.Key.IsScale)
													{
														return new {
															scale = skv.Key.Value,
															contrast = contrast,
															name = skv.Value
														};
													}
													else if (skv.Key.IsString)
													{
														return new {
															language = skv.Key.Value,
															name = skv.Value
														};
													}
													else
													{
														return (dynamic)null;   // 不满足条件时返回 null
													}
												}).Where (item => item != null).ToList ();
										}
										else
										{
											if (PriFileHelper.IsMsResourcePrefix (kv.Value))
											{
												var itemDict = new Dictionary<string, string> ();
												itemDict = priAllRes.LocaleResourceAllValues (kv.Value);
												itemDict ["root"] = kv.Value;
												dict [Utils.PascalToCamel (kv.Key)] = itemDict;
											}
											else
											{
												dict [Utils.PascalToCamel (kv.Key)] = kv.Value;
											}
										}
									}
									pkgapps.Add (obj);
								}
								#endregion
								packageInfo = new {
									file = pkgfile,
									identity = pkgid,
									properties = pkgprop,
									resources = pkgres,
									prerequisites = pkgpreq,
									applications = pkgapps,
									capabilities = pkgcaps,
									dependencies = pkgdeps
								};
							}
							var jsonstr = Newtonsoft.Json.JsonConvert.SerializeObject (packageInfo, Newtonsoft.Json.Formatting.None);
							var ze = new ZipEntry ("info.json");
							ze.DateTime = DateTime.Now;
							zos.PutNextEntry (ze);
							var bytes = Encoding.UTF8.GetBytes (jsonstr);
							zos.Write (bytes, 0, bytes.Length);
							zos.CloseEntry ();
							if (resolve != null) JSHelper.CallJS (resolve);
						}
						finally
						{
							EnablePri = usePri;
							UsePri = parsePri;
						}
					}
				}
				#endregion
			}
			catch (Exception ex)
			{
				if (reject != null) JSHelper.CallJS (reject, ex);
			}
		}
		public void SaveJsonFileAsync (string savefilepath, object resolve, object reject)
		{
			Thread thread = new Thread (() => {
				SaveJsonFile (savefilepath, resolve, reject);
			});
			thread.SetApartmentState (ApartmentState.MTA);
			thread.IsBackground = true;
			thread.Start ();
		}
		public void SaveXmlFile (string savefilepath, object resolve, object reject)
		{
			try
			{
				#region
				using (var fs = File.Create (savefilepath))
				{
					using (var zos = new ZipOutputStream (fs))
					{
						zos.SetLevel (9);
						bool parsePri = UsePri;
						bool usePri = EnablePri;
						try
						{
							UsePri = false;
							EnablePri = false;
							var xml = new XmlDocument ();
							var decl = xml.CreateXmlDeclaration ("1.0", "utf-8", "yes");
							xml.AppendChild (decl);
							var root = xml.CreateElement ("Package");
							xml.AppendChild (root);
							#region file
							var typestr = "unknown";
							switch (Type)
							{
								case PackageType.Appx: typestr = "appx"; break;
								case PackageType.Bundle: typestr = "bundle"; break;
								default:
								case PackageType.Unknown: typestr = "unknown"; break;
							}
							var rolestr = "unknown";
							switch (Role)
							{
								case PackageRole.Application: rolestr = "application"; break;
								case PackageRole.Framework: rolestr = "framework"; break;
								case PackageRole.Resource: rolestr = "resource"; break;
								default:
								case PackageRole.Unknown: rolestr = "unknown"; break;
							}
							{
								var nodefile = xml.CreateElement ("File");
								var nodefilepath = xml.CreateElement ("Path");
								nodefilepath.InnerText = FilePath;
								var nodefilevalid = xml.CreateElement ("Valid");
								nodefilevalid.InnerText = IsValid ? "true" : "false";
								var nodefiletype = xml.CreateElement ("Type");
								nodefiletype.InnerText = typestr;
								var nodefilerole = xml.CreateElement ("Role");
								nodefilerole.InnerText = rolestr;
								nodefile.AppendChild (nodefilepath);
								nodefile.AppendChild (nodefilevalid);
								nodefile.AppendChild (nodefiletype);
								nodefile.AppendChild (nodefilerole);
								root.AppendChild (nodefile);
							}
							#endregion
							#region id
							var id = Identity;
							{
								var nodeid = xml.CreateElement ("Identity");
								var nodeidname = xml.CreateElement ("Name");
								nodeidname.InnerText = id.Name;
								var nodeidpub = xml.CreateElement ("Publisher");
								nodeidpub.InnerText = id.Publisher;
								var nodeidver = xml.CreateElement ("Version");
								nodeidver.InnerText = id.Version.ToString ();
								var nodeidrealver = xml.CreateElement ("RealVersion");
								nodeidrealver.InnerText = id.RealVersion.ToString ();
								var nodeidarchs = xml.CreateElement ("ProcessorArchitectures");
								foreach (var a in id.ProcessArchitecture)
								{
									var astr = "";
									switch (a)
									{
										case Architecture.ARM: astr = "arm"; break;
										case Architecture.ARM64: astr = "arm64"; break;
										case Architecture.Neutral: astr = "neutral"; break;
										case Architecture.x64: astr = "x64"; break;
										case Architecture.x86: astr = "x86"; break;
										default:
										case Architecture.Unknown: astr = "unknown"; break;
									}
									var nodeidarch = xml.CreateElement ("ProcessorArchitecture");
									nodeidarch.SetAttribute ("Value", astr);
									nodeidarchs.AppendChild (nodeidarch);
								}
								var nodeidfamily = xml.CreateElement ("FamilyName");
								nodeidfamily.InnerText = id.FamilyName;
								var nodeidfull = xml.CreateElement ("FullName");
								nodeidfull.InnerText = id.FullName;
								var nodeidresid = xml.CreateElement ("ResourceId");
								nodeidresid.InnerText = id.ResourceId;
								nodeid.AppendChild (nodeidname);
								nodeid.AppendChild (nodeidpub);
								nodeid.AppendChild (nodeidver);
								nodeid.AppendChild (nodeidrealver);
								nodeid.AppendChild (nodeidarchs);
								nodeid.AppendChild (nodeidfamily);
								nodeid.AppendChild (nodeidfamily);
								nodeid.AppendChild (nodeidresid);
								root.AppendChild (nodeid);
							}
							#endregion
							using (var priAllRes = new PriAllValuesReader (this))
							{
								priAllRes.Init ();
								#region prop
								var prop = Properties;
								var dispname = prop.DisplayName;
								var dispNameDict = new Dictionary<string, string> ();
								if (PriFileHelper.IsMsResourcePrefix (dispname))
									dispNameDict = priAllRes.LocaleResourceAllValues (dispname);
								dispNameDict ["root"] = dispname;
								var desc = prop.Description;
								var descDict = new Dictionary<string, string> ();
								if (PriFileHelper.IsMsResourcePrefix (desc))
									descDict = priAllRes.LocaleResourceAllValues (desc);
								descDict ["root"] = desc;
								var disppub = prop.Publisher;
								var dispPubDict = new Dictionary<string, string> ();
								if (PriFileHelper.IsMsResourcePrefix (disppub))
									dispPubDict = priAllRes.LocaleResourceAllValues (disppub);
								dispPubDict ["root"] = disppub;
								{
									var nodeprop = xml.CreateElement ("Properties");
									var nodepropname = xml.CreateElement ("DisplayName");
									foreach (var kv in dispNameDict)
									{
										var nodelocale = xml.CreateElement ("LocaleResource");
										nodelocale.SetAttribute ("Lang", kv.Key);
										nodelocale.InnerText = kv.Value;
										nodepropname.AppendChild (nodelocale);
									}
									var nodeproppub = xml.CreateElement ("PublisherDisplayName");
									foreach (var kv in dispPubDict)
									{
										var nodelocale = xml.CreateElement ("LocaleResource");
										nodelocale.SetAttribute ("Lang", kv.Key);
										nodelocale.InnerText = kv.Value;
										nodeproppub.AppendChild (nodelocale);
									}
									var nodepropdesc = xml.CreateElement ("Description");
									foreach (var kv in descDict)
									{
										var nodelocale = xml.CreateElement ("LocaleResource");
										nodelocale.SetAttribute ("Lang", kv.Key);
										nodelocale.InnerText = kv.Value;
										nodepropdesc.AppendChild (nodelocale);
									}
									var nodeproplogo = xml.CreateElement ("Logo");
									foreach (var kv in priAllRes.FileResourceAllValues (prop.Logo, zos))
									{
										#region logo
										var key = kv.Key;
										var value = kv.Value;
										var nodefile = xml.CreateElement ("File");
										var attrName = xml.CreateAttribute ("Name");
										attrName.Value = value;
										nodefile.Attributes.Append (attrName);
										if (key.Contrast != PriResourceKey.PriContrast.None)
										{
											string contrast = "";
											switch (key.Contrast)
											{
												case PriResourceKey.PriContrast.Black: contrast = "black"; break;
												case PriResourceKey.PriContrast.White: contrast = "white"; break;
												case PriResourceKey.PriContrast.High: contrast = "high"; break;
												case PriResourceKey.PriContrast.Low: contrast = "low"; break;
											}
											var attrContrast = xml.CreateAttribute ("Contrast");
											attrContrast.Value = contrast;
											nodefile.Attributes.Append (attrContrast);
										}
										if (key.IsTargetSize)
										{
											var attr = xml.CreateAttribute ("TargetSize");
											attr.Value = key.Value.ToString ();
											nodefile.Attributes.Append (attr);
										}
										else if (key.IsScale)
										{
											var attr = xml.CreateAttribute ("Scale");
											attr.Value = key.Value.ToString ();
											nodefile.Attributes.Append (attr);
										}
										else if (key.IsString)
										{
											var attr = xml.CreateAttribute ("Language");
											attr.Value = key.Value.ToString ();
											nodefile.Attributes.Append (attr);
										}
										nodeproplogo.AppendChild (nodefile);
										#endregion
									}
									var nodepropfrw = xml.CreateElement ("Framework");
									nodepropfrw.InnerText = prop.Framework ? "true" : "false";
									var nodepropres = xml.CreateElement ("ResourcePackage");
									nodepropres.InnerText = prop.ResourcePackage ? "true" : "false";
									nodeprop.AppendChild (nodepropname);
									nodeprop.AppendChild (nodeproppub);
									nodeprop.AppendChild (nodepropdesc);
									nodeprop.AppendChild (nodeproplogo);
									nodeprop.AppendChild (nodepropfrw);
									nodeprop.AppendChild (nodepropres);
									root.AppendChild (nodeprop);
								}
								#endregion
								#region apps
								// Applications 节点
								XmlElement nodeApplications = xml.CreateElement ("Applications");

								foreach (var app in Applications)
								{
									XmlElement nodeApp = xml.CreateElement ("Application");

									// AppUserModelId 作为属性
									string aumidObj = app ["AppUserModelId"];
									if (aumidObj == null || aumidObj.Trim ().Length == 0) aumidObj = app ["AppUserModelID"];
									nodeApp.SetAttribute ("AppUserModelId", aumidObj);
									foreach (var kv in app)
									{
										string key = kv.Key;
										string value = kv.Value;

										// 跳过 AppUserModelId，因为已经作为属性
										if (string.Equals (key, "AppUserModelId", StringComparison.OrdinalIgnoreCase))
											continue;

										XmlElement nodeProp = xml.CreateElement (key);

										// 文件资源类型
										if (Utils.AppFileProperties.Contains (key, StringComparer.OrdinalIgnoreCase))
										{
											foreach (var skv in priAllRes.FileResourceAllValues (value, zos))
											{
												var fileKey = skv.Key;
												var fileValue = skv.Value;

												XmlElement nodeFile = xml.CreateElement ("File");

												// Name 必有
												XmlAttribute attrName = xml.CreateAttribute ("Name");
												attrName.Value = fileValue;
												nodeFile.Attributes.Append (attrName);

												// Contrast 可选
												if (fileKey.Contrast != PriResourceKey.PriContrast.None)
												{
													string contrast = "";
													switch (fileKey.Contrast)
													{
														case PriResourceKey.PriContrast.Black: contrast = "black"; break;
														case PriResourceKey.PriContrast.White: contrast = "white"; break;
														case PriResourceKey.PriContrast.High: contrast = "high"; break;
														case PriResourceKey.PriContrast.Low: contrast = "low"; break;
													}
													if (!string.IsNullOrEmpty (contrast))
													{
														XmlAttribute attrContrast = xml.CreateAttribute ("Contrast");
														attrContrast.Value = contrast;
														nodeFile.Attributes.Append (attrContrast);
													}
												}

												// TargetSize
												if (fileKey.IsTargetSize)
												{
													XmlAttribute attrTS = xml.CreateAttribute ("TargetSize");
													attrTS.Value = fileKey.Value.ToString ();
													nodeFile.Attributes.Append (attrTS);
												}
												// Scale
												else if (fileKey.IsScale)
												{
													XmlAttribute attrS = xml.CreateAttribute ("Scale");
													attrS.Value = fileKey.Value.ToString ();
													nodeFile.Attributes.Append (attrS);
												}
												// Language
												else if (fileKey.IsString)
												{
													XmlAttribute attrL = xml.CreateAttribute ("Lang");
													attrL.Value = fileKey.Value.ToString ();
													nodeFile.Attributes.Append (attrL);
												}
												nodeProp.AppendChild (nodeFile);
											}
										}
										// 多语言资源
										else if (PriFileHelper.IsMsResourcePrefix (value))
										{
											Dictionary<string, string> localeDict = priAllRes.LocaleResourceAllValues (value);
											localeDict ["root"] = value.ToString ();

											foreach (KeyValuePair<string, string> lkv in localeDict)
											{
												XmlElement nodeLocale = xml.CreateElement ("LocaleResource");
												nodeLocale.SetAttribute ("Lang", lkv.Key);
												nodeLocale.InnerText = lkv.Value;
												nodeProp.AppendChild (nodeLocale);
											}
										}
										else
										{
											nodeProp.InnerText = value.ToString ();
										}

										nodeApp.AppendChild (nodeProp);
									}

									nodeApplications.AppendChild (nodeApp);
								}
								root.AppendChild (nodeApplications);
								#endregion
							}
							#region prerequistes
							var preq = Prerequisites;
							{
								var nodepreq = xml.CreateElement ("Prerequisites");
								var nodeosmin = xml.CreateElement ("OSMinVersion");
								nodeosmin.InnerText = preq.OSMinVersion.ToString ();
								nodeosmin.SetAttribute ("Description", preq.OSMinVersionDescription);
								var nodeosmaxt = xml.CreateElement ("OSMaxVersionTested");
								nodeosmaxt.InnerText = preq.OSMaxVersionTested.ToString ();
								nodeosmaxt.SetAttribute ("Description", preq.OSMaxVersionDescription);
								nodepreq.AppendChild (nodeosmin);
								nodepreq.AppendChild (nodeosmaxt);
								root.AppendChild (nodepreq);
							}
							#endregion
							#region resources
							XmlElement nodeResources = xml.CreateElement ("Resources");
							var res = Resources;
							if (res.Languages != null)
							{
								foreach (var lang in res.Languages)
								{
									XmlElement nodeRes = xml.CreateElement ("Resource");
									XmlAttribute attrLang = xml.CreateAttribute ("Language");
									attrLang.Value = lang;
									nodeRes.Attributes.Append (attrLang);
									nodeResources.AppendChild (nodeRes);
								}
							}
							if (res.Scales != null)
							{
								foreach (var scale in res.Scales)
								{
									XmlElement nodeRes = xml.CreateElement ("Resource");
									XmlAttribute attrScale = xml.CreateAttribute ("Scale");
									attrScale.Value = scale.ToString ();
									nodeRes.Attributes.Append (attrScale);
									nodeResources.AppendChild (nodeRes);
								}
							}
							if (res.DXFeatures != null)
							{
								foreach (var d in res.DXFeatures)
								{
									int level = -1;
									switch (d)
									{
										case DXFeatureLevel.Level9: level = 9; break;
										case DXFeatureLevel.Level10: level = 10; break;
										case DXFeatureLevel.Level11: level = 11; break;
										case DXFeatureLevel.Level12: level = 12; break;
										case DXFeatureLevel.Unspecified:
										default: level = -1; break;
									}
									XmlElement nodeRes = xml.CreateElement ("Resource");
									XmlAttribute attrDX = xml.CreateAttribute ("DXFeatureLevel");
									attrDX.Value = level.ToString ();
									nodeRes.Attributes.Append (attrDX);
									nodeResources.AppendChild (nodeRes);
								}
							}
							root.AppendChild (nodeResources);
							#endregion
							#region capabilities
							XmlElement nodeCapabilities = xml.CreateElement ("Capabilities");
							var caps = Capabilities;
							if (caps.Capabilities != null)
							{
								foreach (var cap in caps.Capabilities)
								{
									XmlElement nodeCap = xml.CreateElement ("Capability");
									XmlAttribute attrName = xml.CreateAttribute ("Name");
									attrName.Value = cap;
									nodeCap.Attributes.Append (attrName);
									nodeCapabilities.AppendChild (nodeCap);
								}
							}
							if (caps.DeviceCapabilities != null)
							{
								foreach (var devCap in caps.DeviceCapabilities)
								{
									XmlElement nodeDevCap = xml.CreateElement ("DeviceCapability");
									XmlAttribute attrName = xml.CreateAttribute ("Name");
									attrName.Value = devCap;
									nodeDevCap.Attributes.Append (attrName);
									nodeCapabilities.AppendChild (nodeDevCap);
								}
							}
							root.AppendChild (nodeCapabilities);
							#endregion
							#region dependencies
							XmlElement nodeDependencies = xml.CreateElement ("Dependencies");
							var deps = Dependencies;
							if (deps != null)
							{
								foreach (var dep in deps)
								{
									XmlElement nodeDep = xml.CreateElement ("Dependency");
									XmlAttribute attrName = xml.CreateAttribute ("Name");
									attrName.Value = dep.Name ?? "";
									nodeDep.Attributes.Append (attrName);
									XmlAttribute attrPublisher = xml.CreateAttribute ("Publisher");
									attrPublisher.Value = dep.Publisher ?? "";
									nodeDep.Attributes.Append (attrPublisher);
									XmlAttribute attrMinVersion = xml.CreateAttribute ("MinVersion");
									attrMinVersion.Value = dep.Version != null ? dep.Version.ToString () : "";
									nodeDep.Attributes.Append (attrMinVersion);
									nodeDependencies.AppendChild (nodeDep);
								}
							}
							root.AppendChild (nodeDependencies);
							#endregion
							var ze = new ZipEntry ("info.xml");
							ze.DateTime = DateTime.Now;
							zos.PutNextEntry (ze);
							byte [] bytes;
							using (var ms = new MemoryStream ())
							{
								var settings = new XmlWriterSettings {
									Encoding = Encoding.UTF8,
									Indent = true,
									OmitXmlDeclaration = false
								};
								using (var writer = XmlWriter.Create (ms, settings))
								{
									xml.Save (writer);
								}
								bytes = ms.ToArray ();
							}
							zos.Write (bytes, 0, bytes.Length);
							zos.CloseEntry ();
							if (resolve != null) JSHelper.CallJS (resolve);
						}
						finally
						{
							EnablePri = usePri;
							UsePri = parsePri;
						}
					}
				}
				#endregion
			}
			catch (Exception ex)
			{
				if (reject != null) JSHelper.CallJS (reject, ex);
			}
		}
		public void SaveXmlFileAsync (string savefilepath, object resolve, object reject)
		{
			Thread thread = new Thread (() => {
				SaveXmlFile (savefilepath, resolve, reject);
			});
			thread.SetApartmentState (ApartmentState.MTA);
			thread.IsBackground = true;
			thread.Start ();
		}
	}
}

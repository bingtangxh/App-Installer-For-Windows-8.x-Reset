using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppxPackage.Info;
using NativeWrappers;
using System.Runtime.InteropServices;
using DataUtils;
using System.Net;
using System.IO;

namespace AppxPackage
{
	public enum DeploymentOptions
	{
		None = 0,
		ForceAppShutdown = 0x00000001,
		DevelopmentMode = 0x00000002,
		InstallAllResources = 0x00000020
	};
	public enum PackageStatus
	{
		Normal = 0,
		LicenseInvalid = 1,
		Modified = 2,
		Tampered = 3
	};
	public static class ImageUriToBase64
	{
		public static string ConvertToDataUri (string uriString)
		{
			if (string.IsNullOrEmpty (uriString)) throw new ArgumentNullException ("uriString");
			Uri uri = new Uri (uriString, UriKind.Absolute);
			byte [] data;
			string mime;
			if (uri.IsFile)
			{
				string path = uri.LocalPath;
				data = File.ReadAllBytes (path);
				mime = GetMimeFromExtension (Path.GetExtension (path));
			}
			else
			{
				using (WebClient wc = new WebClient ())
				{
					data = wc.DownloadData (uri);
					mime = GetMimeFromExtension (Path.GetExtension (uri.AbsolutePath));
				}
			}
			string base64 = Convert.ToBase64String (data);
			return "data:" + mime + ";base64," + base64;
		}
		private static string GetMimeFromExtension (string ext)
		{
			if (string.IsNullOrEmpty (ext)) return "application/octet-stream";
			switch (ext.ToLowerInvariant ())
			{
				case ".png": return "image/png";
				case ".jpg":
				case ".jpeg": return "image/jpeg";
				case ".gif": return "image/gif";
				case ".bmp": return "image/bmp";
				case ".webp": return "image/webp";
				case ".svg": return "image/svg+xml";
				default: return "application/octet-stream";
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PMIdentity: IIdentity
	{
		private string name = "";
		private string publisher = "";
		private DataUtils.Version version = new DataUtils.Version ();
		private IEnumerable<Architecture> archs;
		private string familyName = "";
		private string fullName = "";
		private string resourceId = "";
		public PMIdentity (string _name, string _publisher, DataUtils.Version _ver, IEnumerable<Architecture> _archs, string _family, string _full, string _resid)
		{
			name = _name;
			publisher = _publisher;
			version = _ver;
			archs = _archs ?? new List<Architecture> ();
			familyName = _family;
			fullName = _full;
			resourceId = _resid;
		}
		public PMIdentity (PackageManageHelper.FIND_PACKAGE_ID pkgId) :
			this (
				Marshal.PtrToStringUni (pkgId.lpName),
				Marshal.PtrToStringUni (pkgId.lpPublisher),
				new DataUtils.Version (pkgId.qwVersion),
				new Architecture [] { (Architecture)pkgId.wProcessArchitecture },
				Marshal.PtrToStringUni (pkgId.lpFamilyName),
				Marshal.PtrToStringUni (pkgId.lpFullName),
				Marshal.PtrToStringUni (pkgId.lpResourceId)
				)
		{ }
		public string FamilyName => familyName;
		public string FullName => fullName;
		public string Name => name;
		public List<Architecture> ProcessArchitecture => archs.ToList ();
		public string Publisher => publisher;
		public string ResourceId => resourceId;
		DataUtils.Version IIdentity.Version => version;
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PMProperties: IProperties
	{
		private string desc = "";
		private string displayName = "";
		private bool framework = false;
		private string logo = "";
		private string publisher = "";
		private bool isres = false;
		public PMProperties (string _name, string _pub, string _desc, string _logo, bool _fw, bool _res)
		{
			desc = _desc;
			displayName = _name;
			framework = _fw;
			logo = _logo;
			publisher = _pub;
			isres = _res;
		}
		public PMProperties (PackageManageHelper.FIND_PACKAGE_PROPERTIES prop) :
			this (
				Marshal.PtrToStringUni (prop.lpDisplayName),
				Marshal.PtrToStringUni (prop.lpPublisher),
				Marshal.PtrToStringUni (prop.lpDescription),
				Marshal.PtrToStringUni (prop.lpLogoUri),
				prop.bIsFramework,
				prop.bIsResourcePackage
			)
		{ }
		public string Description => desc;
		public string DisplayName => displayName;
		public bool Framework => framework;
		public string Logo => logo;
		public string LogoBase64
		{
			get
			{
				try { return ImageUriToBase64.ConvertToDataUri (Logo); }
				catch (Exception) { }
				return "";
			}
		}
		public string Publisher => publisher;
		public bool ResourcePackage => isres;
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class PMPackageInfo
	{
		public PMIdentity Identity { get; private set; }
		public PMProperties Properties { get; private set; }
		public bool IsBundle { get; private set; }
		public bool DevelopmentMode { get; private set; }
		public string InstallLocation { get; private set; }
		public List<string> Users { get; private set; }
		public List<string> SIDs { get; private set; }
		public PMPackageInfo (PackageManageHelper.FIND_PACKAGE_INFO info)
		{
			Identity = new PMIdentity (info.piIdentity);
			Properties = new PMProperties (info.piProperties);
			IsBundle = info.piProperties.bIsBundle;
			DevelopmentMode = info.piProperties.bIsDevelopmentMode;
			InstallLocation = Marshal.PtrToStringUni (info.lpInstallLocation);
			Users = (Marshal.PtrToStringUni (info.lpUsers) ?? "").Split (';').ToList ();
			SIDs = (Marshal.PtrToStringUni (info.lpSIDs) ?? "").Split (';').ToList ();
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public static class PackageManager
	{
		private static DataUtils._I_HResult FromNative (int hr,IntPtr pErrorCode,IntPtr pDetailMsg)
		{
			string errCode = null;
			string detail = null;
			try
			{
				if (pErrorCode != IntPtr.Zero) errCode = Marshal.PtrToStringUni (pErrorCode);
				if (pDetailMsg != IntPtr.Zero) detail = Marshal.PtrToStringUni (pDetailMsg);
			}
			finally
			{
				if (pErrorCode != IntPtr.Zero) PackageManageHelper.PackageManagerFreeString (pErrorCode);
				if (pDetailMsg != IntPtr.Zero) PackageManageHelper.PackageManagerFreeString (pDetailMsg);
			}
			return new DataUtils._I_HResult (hr, errCode, detail);
		}
		public delegate void PackageProgressCallback (uint progress);
		internal sealed class ProgressCallbackHolder
		{
			private readonly PackageProgressCallback _callback;
			public ProgressCallbackHolder (PackageProgressCallback cb)
			{
				_callback = cb;
				Native = new PackageManageHelper.PKGMRR_PROGRESSCALLBACK (OnNative);
			}
			public PackageManageHelper.PKGMRR_PROGRESSCALLBACK Native { get; private set; }
			private void OnNative (uint progress, IntPtr custom)
			{
				if (_callback != null)
					_callback (progress);
			}
		}
		internal sealed class ResultCallbackHolder
		{
			public readonly List<PMPackageInfo> RetList = new List<PMPackageInfo> ();
			public PackageManageHelper.PKGMGR_FINDENUMCALLBACK Callback;
			public ResultCallbackHolder ()
			{
				Callback = OnResult;
			}
			private void OnResult (
				IntPtr nativeInfo,
				IntPtr pCustom)
			{
				RetList.Add (new PMPackageInfo (PackageManageHelper.PtrToFindPackageInfo (nativeInfo)));
			}
		}
		public static DataUtils._I_HResult AddPackage (Uri fileUri, IEnumerable<Uri> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			if (fileUri == null) throw new ArgumentNullException ("Required File URI");
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.Select (u => u.AbsoluteUri).ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.AddAppxPackageFromURI (fileUri.AbsoluteUri, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult AddPackage (string filePath, IEnumerable<string> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.AddAppxPackageFromPath (filePath, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static Tuple <DataUtils._I_HResult, List <PMPackageInfo>> GetPackages ()
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ResultCallbackHolder callback = null;
			try
			{
				callback = new ResultCallbackHolder ();
				int hr = PackageManageHelper.GetAppxPackages (callback.Callback, IntPtr.Zero, out pErrCode, out pDetail);
				return Tuple.Create (FromNative (hr, pErrCode, pDetail), callback.RetList);
			}
			finally
			{
				GC.KeepAlive (callback);
			}
		}
		public static DataUtils._I_HResult RemovePackage (string packageFullName, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ProgressCallbackHolder holder = null;
			try
			{
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.RemoveAppxPackage (packageFullName, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult ClearupPackage (string packageName, string userSID, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ProgressCallbackHolder holder = null;
			try
			{
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.CleanupAppxPackage (packageName, userSID, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult RegisterPackage (Uri manifestUri, IEnumerable<Uri> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			if (manifestUri == null) throw new ArgumentNullException ("Required File URI");
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.Select (u => u.AbsoluteUri).ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.RegisterAppxPackageByUri (manifestUri.AbsoluteUri, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult RegisterPackage (string manifestPath, IEnumerable<string> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.RegisterAppxPackageByUri (manifestPath, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult RegisterPackageByFullName (string pkgFullName, IEnumerable<string> depFullNames, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depFullNames == null ? new List<string> () : depFullNames.ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.RegisterAppxPackageByFullName (pkgFullName, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult SetPackageStatus (string packageFullName, PackageStatus status)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			try
			{
				int hr = PackageManageHelper.SetAppxPackageStatus (packageFullName, (uint)status, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally { }
		}
		public static DataUtils._I_HResult StagePackage (Uri fileUri, IEnumerable<Uri> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			if (fileUri == null) throw new ArgumentNullException ("Required File URI");
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.Select (u => u.AbsoluteUri).ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.StageAppxPackageFromURI (fileUri.AbsoluteUri, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult StagePackage (string filePath, IEnumerable<string> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.StageAppxPackageFromPath (filePath, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult StageUserData (string packageFullName, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ProgressCallbackHolder holder = null;
			try
			{
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.StageAppxUserData (packageFullName, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult UpdatePackage (Uri fileUri, IEnumerable<Uri> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			if (fileUri == null) throw new ArgumentNullException ("Required File URI");
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.Select (u => u.AbsoluteUri).ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.UpdateAppxPackageFromURI (fileUri.AbsoluteUri, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static DataUtils._I_HResult UpdatePackage (string filePath, IEnumerable<string> depUris, DeploymentOptions options, PackageProgressCallback progress = null)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			var depList = depUris == null ? new List<string> () : depUris.ToList ();
			ProgressCallbackHolder holder = null;
			try
			{
				depArray = NativeUtil.AllocStringArray (depList);
				PackageManageHelper.PKGMRR_PROGRESSCALLBACK nativeCb = null;
				if (progress != null)
				{
					holder = new ProgressCallbackHolder (progress);
					nativeCb = holder.Native;
				}
				int hr = PackageManageHelper.UpdateAppxPackageFromPath (filePath, depArray, (uint)options, nativeCb, IntPtr.Zero, out pErrCode, out pDetail);
				return FromNative (hr, pErrCode, pDetail);
			}
			finally
			{
				NativeUtil.FreeStringArray (depArray, depList.Count);
				GC.KeepAlive (holder);
			}
		}
		public static Tuple<DataUtils._I_HResult, List<PMPackageInfo>> FindPackage (string packageName, string packagePublisher)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ResultCallbackHolder callback = null;
			try
			{
				callback = new ResultCallbackHolder ();
				int hr = PackageManageHelper.FindAppxPackagesByIdentity (packageName, packagePublisher, callback.Callback, IntPtr.Zero, out pErrCode, out pDetail);
				return Tuple.Create (FromNative (hr, pErrCode, pDetail), callback.RetList);
			}
			finally
			{
				GC.KeepAlive (callback);
			}
		}
		public static Tuple<DataUtils._I_HResult, List<PMPackageInfo>> FindPackage (string packageFamilyName)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ResultCallbackHolder callback = null;
			try
			{
				callback = new ResultCallbackHolder ();
				int hr = PackageManageHelper.FindAppxPackagesByFamilyName (packageFamilyName, callback.Callback, IntPtr.Zero, out pErrCode, out pDetail);
				return Tuple.Create (FromNative (hr, pErrCode, pDetail), callback.RetList);
			}
			finally
			{
				GC.KeepAlive (callback);
			}
		}
		public static Tuple<DataUtils._I_HResult, List<PMPackageInfo>> FindPackageByFullName (string packageFullName)
		{
			IntPtr depArray = IntPtr.Zero;
			IntPtr pErrCode = IntPtr.Zero;
			IntPtr pDetail = IntPtr.Zero;
			ResultCallbackHolder callback = null;
			try
			{
				callback = new ResultCallbackHolder ();
				int hr = PackageManageHelper.FindAppxPackage (packageFullName, callback.Callback, IntPtr.Zero, out pErrCode, out pDetail);
				return Tuple.Create (FromNative (hr, pErrCode, pDetail), callback.RetList);
			}
			finally
			{
				GC.KeepAlive (callback);
			}
		}
	}
}

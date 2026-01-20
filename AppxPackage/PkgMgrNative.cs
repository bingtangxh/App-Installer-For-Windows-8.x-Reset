// PackageManageHelper.cs
// P/Invoke wrapper for pkgmgr.dll (x86).
//
// 说明：此文件兼容 .NET Framework 4。
// - 将项目 Platform target 设置为 x86。
// - pkgmgr.dll 提供 PackageManagerFreeString 来释放它返回的字符串，请务必使用它释放由 pkgmgr 返回的 LPWSTR。

using System;
using System.Runtime.InteropServices;

namespace NativeWrappers
{
	using DWORD = System.UInt32;
	using HRESULT = System.Int32;
	using BOOL = System.Int32;
	using UINT64 = System.UInt64;
	using System.Collections.Generic;
	using System.Linq;

	public static class PackageManageHelper
	{
		private const string DllName = "pkgmgr.dll";
		private const CallingConvention CallConv = CallingConvention.Cdecl;

		[UnmanagedFunctionPointer (CallConv)]
		public delegate void PKGMRR_PROGRESSCALLBACK (DWORD dwProgress, IntPtr pCustom);

		[UnmanagedFunctionPointer (CallConv)]
		public delegate void PKGMGR_FINDENUMCALLBACK (IntPtr pNowItem, IntPtr pCustom);

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct REGISTER_PACKAGE_DEFENDENCIES
		{
			public DWORD dwSize;
			public IntPtr alpDepUris; // tail array
		}

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct FIND_PACKAGE_ID
		{
			public UINT64 qwVersion;
			public ushort wProcessArchitecture;
			[MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
			public ushort [] wPadding;
			public IntPtr lpName;
			public IntPtr lpFullName;
			public IntPtr lpFamilyName;
			public IntPtr lpPublisher;
			public IntPtr lpPublisherId;
			public IntPtr lpResourceId;
		}

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct FIND_PACKAGE_PROPERTIES
		{
			public IntPtr lpDisplayName;
			public IntPtr lpDescription;
			public IntPtr lpPublisher;
			public IntPtr lpLogoUri;
			[MarshalAs (UnmanagedType.Bool)]
			public bool bIsFramework;
			[MarshalAs (UnmanagedType.Bool)]
			public bool bIsResourcePackage;
			[MarshalAs (UnmanagedType.Bool)]
			public bool bIsBundle;
			[MarshalAs (UnmanagedType.Bool)]
			public bool bIsDevelopmentMode;
		}

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct FIND_PACKAGE_INFO
		{
			public FIND_PACKAGE_ID piIdentity;
			public FIND_PACKAGE_PROPERTIES piProperties;
			public IntPtr lpInstallLocation;
			public IntPtr lpUsers;
			public IntPtr lpSIDs;
			public DWORD dwDependencesSize;
			public DWORD dwPadding;
			public UINT64 ullBuffer;
		}

		// ========== Functions ==========
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT AddAppxPackageFromPath (string lpPkgPath, IntPtr alpDepUrlList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT AddAppxPackageFromURI (string lpFileUri, IntPtr alpDepFullNameList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT GetAppxPackages (PKGMGR_FINDENUMCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT RemoveAppxPackage (string lpPkgFullName, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT CleanupAppxPackage (string lpPkgName, string lpUserSID, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT RegisterAppxPackageByPath (string lpManifestPath, IntPtr alpDependencyUriList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT RegisterAppxPackageByUri (string lpManifestUri, IntPtr alpDependencyUriList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT RegisterAppxPackageByFullName (string lpPackageFullName, IntPtr alpDepFullNameList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT SetAppxPackageStatus (string lpPackageFullName, DWORD dwStatus, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT StageAppxPackageFromURI (string lpFileUri, IntPtr alpDepUriList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT StageAppxPackageFromPath (string lpFileUri, IntPtr alpDepUriList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT StageAppxUserData (string lpPackageFullName, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT UpdateAppxPackageFromPath (string lpPkgPath, IntPtr alpDepUrlList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT UpdateAppxPackageFromURI (string lpFileUri, IntPtr alpDepFullNameList, DWORD dwDeployOption, PKGMRR_PROGRESSCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT FindAppxPackage (string lpPackageFullName, PKGMGR_FINDENUMCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackageManagerLastErrorCode ();

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackageManagerLastErrorDetailMessage ();

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT ActivateAppxApplication (string lpAppUserId, out DWORD pdwProcessId);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT FindAppxPackagesByIdentity (string lpPkgName, string lpPkgPublisher, PKGMGR_FINDENUMCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT FindAppxPackagesByFamilyName (string lpPkgFamilyName, PKGMGR_FINDENUMCALLBACK pfCallback, IntPtr pCustom, out IntPtr pErrorCode, out IntPtr pDetailMsg);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void PackageManagerFreeString (IntPtr lpString);

		// ========== 托管辅助 ==========
		public static string PtrToStringAndFree (IntPtr nativePtr)
		{
			if (nativePtr == IntPtr.Zero) return null;
			string s = Marshal.PtrToStringUni (nativePtr);
			try
			{
				PackageManagerFreeString (nativePtr);
			}
			catch
			{
			}
			return s;
		}

		public static FIND_PACKAGE_INFO PtrToFindPackageInfo (IntPtr pInfo)
		{
			if (pInfo == IntPtr.Zero) return default (FIND_PACKAGE_INFO);
			object boxed = Marshal.PtrToStructure (pInfo, typeof (FIND_PACKAGE_INFO));
			return (FIND_PACKAGE_INFO)boxed;
		}

		public static string GetDisplayNameFromFindPackageInfo (IntPtr pInfo)
		{
			FIND_PACKAGE_INFO info = PtrToFindPackageInfo (pInfo);
			if (info.piProperties.lpDisplayName == IntPtr.Zero) return null;
			return Marshal.PtrToStringUni (info.piProperties.lpDisplayName);
		}

		public static string [] ReadRegisterPackageDependencies (IntPtr pReg)
		{
			if (pReg == IntPtr.Zero) return new string [0];
			uint size = (uint)Marshal.ReadInt32 (pReg);
			if (size == 0) return new string [0];
			string [] result = new string [size];
			int offset = Marshal.SizeOf (typeof (uint));
			for (int i = 0; i < size; i++)
			{
				IntPtr pStr = Marshal.ReadIntPtr (pReg, offset + i * IntPtr.Size);
				result [i] = pStr == IntPtr.Zero ? null : Marshal.PtrToStringUni (pStr);
			}
			return result;
		}
	}
	internal static class NativeUtil
	{
		public static string ToAbsoluteUriString (Uri uri)
		{
			if (uri == null) return null;
			return uri.AbsoluteUri;
		}

		public static IntPtr AllocStringArray (IEnumerable<string> values)
		{
			if (values == null) return IntPtr.Zero;

			var list = values.Where (s => !string.IsNullOrEmpty (s)).ToList ();
			if (list.Count == 0) return IntPtr.Zero;

			IntPtr mem = Marshal.AllocHGlobal (IntPtr.Size * list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				IntPtr pStr = Marshal.StringToHGlobalUni (list [i]);
				Marshal.WriteIntPtr (mem, i * IntPtr.Size, pStr);
			}
			return mem;
		}

		public static void FreeStringArray (IntPtr array, int count)
		{
			if (array == IntPtr.Zero) return;

			for (int i = 0; i < count; i++)
			{
				IntPtr p = Marshal.ReadIntPtr (array, i * IntPtr.Size);
				if (p != IntPtr.Zero)
					Marshal.FreeHGlobal (p);
			}
			Marshal.FreeHGlobal (array);
		}
	}
}
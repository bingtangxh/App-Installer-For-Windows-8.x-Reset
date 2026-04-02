// PackageReadHelper.cs
// P/Invoke wrapper for pkgread.dll (x86).
//
// 说明：此文件兼容 .NET Framework 4。
// - 将项目 Platform target 设置为 x86（因为你只编译了 x86 的本机 DLL）。
// - pkgread.dll 返回了很多需由调用者释放的 LPWSTR 指针；header 中未提供通用释放函数，示例中调用 CRT 的 free（msvcrt.dll）来释放。
//   如果能修改 pkgread.dll 并导出专用释放函数（如 PackageReadFreeString），那是更安全的做法。

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NativeWrappers
{
	// 本机类型别名（便于阅读）
	using BOOL = System.Int32;
	using WORD = System.UInt16;
	using DWORD = System.UInt32;
	using UINT16 = System.UInt16;
	using UINT64 = System.UInt64;
	using HRESULT = System.Int32;
	using ULONG = System.UInt32;
	using System.Collections.Generic;
	using System.Linq;

	public static class PackageReadHelper
	{
		private const string DllName = "pkgread.dll";
		private const CallingConvention CallConv = CallingConvention.Cdecl;

		[StructLayout (LayoutKind.Sequential)]
		public struct VERSION
		{
			public ushort major;
			public ushort minor;
			public ushort build;
			public ushort revision;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct PAIR_PVOID
		{
			public IntPtr lpKey;
			public IntPtr lpValue;
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct DEPENDENCY_INFO
		{
			public VERSION verMin;
			public IntPtr lpName; // LPWSTR
			public IntPtr lpPublisher; // LPWSTR
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct LIST_DEPINFO
		{
			public DWORD dwSize;
			public IntPtr aDepInfo; // tail array
		}

		// Delegates
		[UnmanagedFunctionPointer (CallConv)]
		public delegate void PKGMRR_PROGRESSCALLBACK (DWORD dwProgress, IntPtr pCustom);

		// ========== P/Invoke ==========
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr CreatePackageReader ();

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool LoadPackageFromFile (IntPtr hReader, string lpFilePath);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyPackageReader (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern ushort GetPackageType (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsPackageValid (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern ushort GetPackageRole (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackageIdentityStringValue (IntPtr hReader, uint dwName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetPackageIdentityVersion (IntPtr hReader, out VERSION pVersion, [MarshalAs (UnmanagedType.Bool)] bool bGetSubPkgVer);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetPackageIdentityArchitecture (IntPtr hReader, out DWORD pdwArchi);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackagePropertiesStringValue (IntPtr hReader, string lpName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT GetPackagePropertiesBoolValue (IntPtr hReader, string lpName, out BOOL pRet);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool AddPackageApplicationItemGetName (string lpName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool RemovePackageApplicationItemGetName (string lpName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetAllApplicationItemsName ();

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyApplicationItemsName (IntPtr hList);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackageApplications (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr ApplicationsToMap (IntPtr hEnumerator);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyApplicationsMap (IntPtr hEnumerator);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyPackageApplications (IntPtr hEnumerator);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetResourcesLanguages (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetResourcesLanguagesToLcid (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetResourcesScales (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern DWORD GetResourcesDxFeatureLevels (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyResourcesLanguagesList (IntPtr hList);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyResourcesLanguagesLcidList (IntPtr hList);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyUInt32List (IntPtr hList);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetDependencesInfoList (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyDependencesInfoList (IntPtr hList);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetCapabilitiesList (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetDeviceCapabilitiesList (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyWStringList (IntPtr hList);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetPackagePrerequisite (IntPtr hReader, string lpName, out VERSION pVerRet);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackagePrerequistieSystemVersionName (IntPtr hReader, string lpName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetAppxFileFromAppxPackage (IntPtr hReader, string lpFileName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetAppxBundlePayloadPackageFile (IntPtr hReader, string lpFileName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetAppxPriFileStream (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetFileFromPayloadPackage (IntPtr hPackageStream, string lpFileName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPriFileFromPayloadPackage (IntPtr hPackageStream);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetSuitablePackageFromBundle (IntPtr hReader, out IntPtr pStreamForLang, out IntPtr pStreamForScale);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern ULONG DestroyAppxFileStream (IntPtr hFileStream);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr StreamToBase64W (IntPtr hFileStream, StringBuilder lpMimeBuf, DWORD dwCharCount, out IntPtr lpBase64Head);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetAppxBundleApplicationPackageFile (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetPackageCapabilityDisplayName (string lpCapabilityName);

		// CRT free（当 header 指示用 free 释放）
		[DllImport ("msvcrt.dll", CallingConvention = CallConv, EntryPoint = "free")]
		private static extern void crt_free (IntPtr ptr);

		// ========== 托管辅助方法 ==========
		public static string GetStringAndFreeFromPkgRead (IntPtr nativePtr)
		{
			if (nativePtr == IntPtr.Zero) return null;
			string s = Marshal.PtrToStringUni (nativePtr);
			try
			{
				PackageReaderFreeString (nativePtr);
				nativePtr = IntPtr.Zero;
			}
			catch
			{
				// 忽略释放失败（注意可能的 CRT 不匹配风险）
			}
			return s;
		}
		public static string GetStringFromPkgRead (IntPtr nativePtr)
		{
			if (nativePtr == IntPtr.Zero) return null;
			string s = Marshal.PtrToStringUni (nativePtr);
			return s;
		}

		public static string PtrToStringNoFree (IntPtr nativePtr)
		{
			if (nativePtr == IntPtr.Zero) return null;
			return Marshal.PtrToStringUni (nativePtr);
		}

		// 解析 HLIST_PVOID (字符串列表)
		public static string [] ReadWStringList (IntPtr hList)
		{
			if (hList == IntPtr.Zero) return new string [0];
			uint size = (uint)Marshal.ReadInt32 (hList);
			if (size == 0) return new string [0];
			string [] result = new string [size];
			int offset = Marshal.SizeOf (typeof (uint)); // typically 4 on x86
			for (int i = 0; i < size; i++)
			{
				IntPtr pSlot = Marshal.ReadIntPtr (hList, offset + i * IntPtr.Size);
				result [i] = pSlot == IntPtr.Zero ? null : Marshal.PtrToStringUni (pSlot);
			}
			return result;
		}

		public static uint [] ReadUInt32List (IntPtr hList)
		{
			if (hList == IntPtr.Zero) return new uint [0];
			uint size = (uint)Marshal.ReadInt32 (hList);
			if (size == 0) return new uint [0];
			uint [] result = new uint [size];
			int offset = Marshal.SizeOf (typeof (uint));
			for (int i = 0; i < size; i++)
			{
				result [i] = (uint)Marshal.ReadInt32 (hList, offset + i * 4);
			}
			return result;
		}

		public static int [] ReadLcidList (IntPtr hList)
		{
			if (hList == IntPtr.Zero) return new int [0];
			uint size = (uint)Marshal.ReadInt32 (hList);
			if (size == 0) return new int [0];
			int [] result = new int [size];
			int offset = Marshal.SizeOf (typeof (uint));
			for (int i = 0; i < size; i++)
			{
				result [i] = Marshal.ReadInt32 (hList, offset + i * 4);
			}
			return result;
		}

		public static DEPENDENCY_INFO [] ReadDependencyInfoList (IntPtr hList)
		{
			if (hList == IntPtr.Zero) return new DEPENDENCY_INFO [0];
			uint size = (uint)Marshal.ReadInt32 (hList);
			if (size == 0) return new DEPENDENCY_INFO [0];
			DEPENDENCY_INFO [] result = new DEPENDENCY_INFO [size];
			int baseOffset = Marshal.SizeOf (typeof (uint));
			int structSize = Marshal.SizeOf (typeof (DEPENDENCY_INFO));
			for (int i = 0; i < size; i++)
			{
				IntPtr pItem = IntPtr.Add (hList, baseOffset + i * structSize);
				object boxed = Marshal.PtrToStructure (pItem, typeof (DEPENDENCY_INFO));
				result [i] = (DEPENDENCY_INFO)boxed;
			}
			return result;
		}

		public static void FreePkgReadMemory (IntPtr nativePtr)
		{
			if (nativePtr == IntPtr.Zero) return;
			try
			{
				crt_free (nativePtr);
			}
			catch
			{
			}
		}

		// ================= Manifest Reader =================

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr CreateManifestReader ();

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool LoadManifestFromFile (
			IntPtr hReader,
			string lpFilePath
		);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void DestroyManifestReader (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern ushort GetManifestType (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsManifestValid (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern ushort GetManifestRole (IntPtr hReader);
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetManifestIdentityStringValue (
			IntPtr hReader,
			uint dwName
		);

		[DllImport (DllName, CallingConvention = CallConv)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetManifestIdentityVersion (
			IntPtr hReader,
			out VERSION pVersion
		);

		[DllImport (DllName, CallingConvention = CallConv)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetManifestIdentityArchitecture (
			IntPtr hReader,
			out DWORD pdwArchi
		);
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetManifestPropertiesStringValue (
			IntPtr hReader,
			string lpName
		);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern HRESULT GetManifestPropertiesBoolValue (
			IntPtr hReader,
			string lpName,
			out BOOL pRet
		);
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool AddManifestApplicationItemGetName (string lpName);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool RemoveManifestApplicationItemGetName (string lpName);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestApplications (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern void DestroyManifestApplications (IntPtr hEnumerator);
		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestResourcesLanguages (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestResourcesLanguagesToLcid (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestResourcesScales (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern DWORD GetManifestResourcesDxFeatureLevels (IntPtr hReader);
		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestDependencesInfoList (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestCapabilitiesList (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern IntPtr GetManifestDeviceCapabilitiesList (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool GetManifestPrerequisite (
			IntPtr hReader,
			string lpName,
			out VERSION pVerRet
		);
		[DllImport (DllName, CallingConvention = CallConv)]
		public static extern void PackageReaderFreeString (IntPtr p);
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetManifestPrerequistieSystemVersionName (IntPtr hReader, string lpName);

		// ========== Appx Bundle Resource Packages ==========
		[DllImport (DllName, CallingConvention = CallConv)]
		private static extern IntPtr GetAppxBundleAllResourcePackageFileNames (IntPtr hReader);

		[DllImport (DllName, CallingConvention = CallConv)]
		private static extern void FreeAppxBundlePayloadsFileNameList (IntPtr hStringList);

		/// <summary>
		/// 获取 Bundle 包中所有资源包的文件名列表。
		/// </summary>
		/// <param name="hReader">由 CreatePackageReader 创建的读取器句柄</param>
		/// <returns>资源包文件名数组，若无资源包或非 bundle 类型则返回空数组</returns>
		public static string [] GetAppxBundleAllResourcePackageFileNameList (IntPtr hReader)
		{
			IntPtr hList = GetAppxBundleAllResourcePackageFileNames (hReader);
			if (hList == IntPtr.Zero)
				return new string [0];

			try
			{
				return ReadWStringList (hList);
			}
			finally
			{
				FreeAppxBundlePayloadsFileNameList (hList);
			}
		}

		[DllImport (DllName, CallingConvention = CallConv)]
		private static extern IntPtr GetAppxBundleAllLocaleResourcePackageFileNames (IntPtr hReader);

		/// <summary>
		/// 获取 Bundle 包中所有区域设置资源包的文件名列表。
		/// </summary>
		/// <param name="hReader">由 CreatePackageReader 创建的读取器句柄</param>
		/// <returns>区域设置资源包文件名数组，若无则返回空数组</returns>
		public static string [] GetAppxBundleAllLocaleResourcePackageFileNameList (IntPtr hReader)
		{
			IntPtr hList = GetAppxBundleAllLocaleResourcePackageFileNames (hReader);
			if (hList == IntPtr.Zero)
				return new string [0];

			try
			{
				return ReadWStringList (hList);
			}
			finally
			{
				FreeAppxBundlePayloadsFileNameList (hList);
			}
		}

		[DllImport (DllName, CallingConvention = CallConv)]
		private static extern IntPtr GetAppxBundleAllFileResourcePackageFileNames (IntPtr hReader);

		/// <summary>
		/// 获取 Bundle 包中所有文件资源包的文件名列表。
		/// </summary>
		/// <param name="hReader">由 CreatePackageReader 创建的读取器句柄</param>
		/// <returns>文件资源包文件名数组，若无则返回空数组</returns>
		public static string [] GetAppxBundleAllFileResourcePackageFileNameList (IntPtr hReader)
		{
			IntPtr hList = GetAppxBundleAllFileResourcePackageFileNames (hReader);
			if (hList == IntPtr.Zero)
				return new string [0];

			try
			{
				return ReadWStringList (hList);
			}
			finally
			{
				FreeAppxBundlePayloadsFileNameList (hList);
			}
		}

		// 用于更新应用项名称列表（设置要查询的项）
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void UpdatePackageApplicationItemGetName (
			[In] IntPtr [] lpNames,   // 传入一个 IntPtr 数组，每个元素指向一个 Unicode 字符串
			uint dwArrLen
		);

		// 回调委托声明
		[UnmanagedFunctionPointer (CallConv)]
		public delegate void IterWStringCallback ([MarshalAs (UnmanagedType.LPWStr)] string lpString);

		// 枚举当前列表中的项（通过回调逐个返回）
		[DllImport (DllName, CallingConvention = CallConv, CharSet = CharSet.Unicode)]
		public static extern void GetPackageApplicationItemGetNameList (
			IterWStringCallback pfCallback   // 回调函数指针
		);
		/// <summary>
		/// 更新内部的应用项名称列表（设置要查询的项）
		/// </summary>
		/// <param name="names">要设置的项名称集合</param>
		public static void SetApplicationItemNames (IEnumerable<string> names)
		{
			if (names == null)
			{
				// 传入空数组，清空列表
				UpdatePackageApplicationItemGetName (new IntPtr [0], 0);
				return;
			}

			var nameList = names.ToList ();
			if (nameList.Count == 0)
			{
				UpdatePackageApplicationItemGetName (new IntPtr [0], 0);
				return;
			}

			// 为每个字符串分配非托管内存
			IntPtr [] ptrs = new IntPtr [nameList.Count];
			try
			{
				for (int i = 0; i < nameList.Count; i++)
				{
					ptrs [i] = Marshal.StringToHGlobalUni (nameList [i]);
				}

				UpdatePackageApplicationItemGetName (ptrs, (uint)nameList.Count);
			}
			finally
			{
				// 释放分配的内存
				foreach (var ptr in ptrs)
				{
					if (ptr != IntPtr.Zero)
						Marshal.FreeHGlobal (ptr);
				}
			}
		}
		/// <summary>
		/// 获取当前内部的应用项名称列表（通过回调收集所有项）
		/// </summary>
		/// <returns>应用项名称数组</returns>
		public static string [] GetApplicationItemNames ()
		{
			var result = new List<string> ();

			// 定义回调：将收到的字符串添加到列表
			IterWStringCallback callback = (str) =>
			{
				if (!string.IsNullOrEmpty (str))
					result.Add (str);
			};

			// 调用本机函数，传入委托
			GetPackageApplicationItemGetNameList (callback);

			// 注意：GC 需要保持 callback 的存活直到本机调用结束，这里通过局部变量引用即可，
			// 因为本机函数是同步调用的，不会在函数返回后继续使用回调。
			return result.ToArray ();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using PCSPRIFILE = System.IntPtr;
using PCOISTREAM = System.IntPtr;

namespace AppxPackage
{
	public static class PriFileHelper
	{
		private const string DLL = "PriFormatCli.dll"; // 改成你的 DLL 名称

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern PCSPRIFILE CreatePriFileInstanceFromStream (PCOISTREAM pStream);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyPriFileInstance (PCSPRIFILE pFilePri);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetPriStringResource (PCSPRIFILE pFilePri, [MarshalAs (UnmanagedType.LPWStr)] string lpswUri);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetPriPathResource (PCSPRIFILE pFilePri, [MarshalAs (UnmanagedType.LPWStr)] string lpswFilePath);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern PCSPRIFILE CreatePriFileInstanceFromPath ([MarshalAs (UnmanagedType.LPWStr)] string lpswFilePath);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.LPWStr)]
		public static extern string PriFileGetLastError ();

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FindPriStringResource (PCSPRIFILE pFilePri, ref LPCWSTRLIST hUriList);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FindPriPathResource (PCSPRIFILE pFilePri, ref LPCWSTRLIST hPathList);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void ClearPriCacheData ();

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetPriResource (PCSPRIFILE pFilePri, [MarshalAs (UnmanagedType.LPWStr)] string lpswResId);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FindPriResource (PCSPRIFILE pFilePri, ref LPCWSTRLIST hUriList);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsMsResourcePrefix ([MarshalAs (UnmanagedType.LPWStr)] string pResName);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsMsResourceUriFull ([MarshalAs (UnmanagedType.LPWStr)] string pResUri);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsMsResourceUri ([MarshalAs (UnmanagedType.LPWStr)] string pResUri);
		public static string PtrToString (IntPtr ptr)
		{
			if (ptr == IntPtr.Zero) return null;
			string s = Marshal.PtrToStringUni (ptr);
			Marshal.FreeHGlobal (ptr); // 如果 DLL 返回的内存要求 free
			return s;
		}

	}
	[StructLayout (LayoutKind.Sequential)]
	public struct LPCWSTRLIST
	{
		public uint dwLength; // DWORD
		[MarshalAs (UnmanagedType.ByValArray, SizeConst = 1)]
		public IntPtr [] aswArray; // LPCWSTR*，数组
	}

}

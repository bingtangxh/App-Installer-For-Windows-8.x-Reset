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
		public static extern void FindPriStringResource (PCSPRIFILE pFilePri, IntPtr hUriList);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FindPriPathResource (PCSPRIFILE pFilePri, IntPtr hPathList);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void ClearPriCacheData ();

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetPriResource (PCSPRIFILE pFilePri, [MarshalAs (UnmanagedType.LPWStr)] string lpswResId);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void FindPriResource (PCSPRIFILE pFilePri, IntPtr hUriList);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsMsResourcePrefix ([MarshalAs (UnmanagedType.LPWStr)] string pResName);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsMsResourceUriFull ([MarshalAs (UnmanagedType.LPWStr)] string pResUri);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool IsMsResourceUri ([MarshalAs (UnmanagedType.LPWStr)] string pResUri);
		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		private static extern void PriFormatFreeString (IntPtr ptr);
		public static string PtrToString (IntPtr ptr)
		{
			if (ptr == IntPtr.Zero) return null;
			string s = Marshal.PtrToStringUni (ptr);
			PriFormatFreeString (ptr); // 如果 DLL 返回的内存要求 free
			return s;
		}
		[StructLayout (LayoutKind.Sequential)]
		internal struct DWORDWSTRPAIR
		{
			public uint dwKey;
			public IntPtr lpValue; // LPWSTR
		}
		[StructLayout (LayoutKind.Sequential)]
		internal struct DWSPAIRLIST
		{
			public uint dwLength;
			public DWORDWSTRPAIR lpArray; // 第一个元素（柔性数组起点）
		}
		[StructLayout (LayoutKind.Sequential)]
		internal struct WSDSPAIR
		{
			public IntPtr lpKey;         // LPWSTR
			public IntPtr lpValue;       // HDWSPAIRLIST
		}
		[StructLayout (LayoutKind.Sequential)]
		internal struct WSDSPAIRLIST
		{
			public uint dwLength;
			public WSDSPAIR lpArray;     // 第一个元素
		}
		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetPriResourceAllValueList (PCSPRIFILE pFilePri, [MarshalAs (UnmanagedType.LPWStr)] string resName);
		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyPriResourceAllValueList (IntPtr list);
		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetPriResourcesAllValuesList (PCSPRIFILE pFilePri, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string [] lpResNames, uint dwCount);
		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern void DestroyResourcesAllValuesList (IntPtr list);
		public static Dictionary<uint, string> ParseDWSPAIRLIST (IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			var result = new Dictionary<uint, string> ();

			// 读取数量
			uint count = (uint)Marshal.ReadInt32 (ptr);

			// 跳过 dwLength
			IntPtr pFirst = IntPtr.Add (ptr, sizeof (uint));

			int elementSize = Marshal.SizeOf (typeof (DWORDWSTRPAIR));

			for (int i = 0; i < count; i++)
			{
				IntPtr pItem = IntPtr.Add (pFirst, i * elementSize);

				object boxed =
					Marshal.PtrToStructure (pItem, typeof (DWORDWSTRPAIR));

				DWORDWSTRPAIR item = (DWORDWSTRPAIR)boxed;

				string value = null;

				if (item.lpValue != IntPtr.Zero)
					value = Marshal.PtrToStringUni (item.lpValue);

				result [item.dwKey] = value;
			}

			return result;
		}
		public static Dictionary<string, Dictionary<uint, string>> ParseWSDSPAIRLIST (IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null;

			var result =
				new Dictionary<string, Dictionary<uint, string>> ();

			uint count = (uint)Marshal.ReadInt32 (ptr);

			IntPtr pFirst = IntPtr.Add (ptr, sizeof (uint));

			int elementSize = Marshal.SizeOf (typeof (WSDSPAIR));

			for (int i = 0; i < count; i++)
			{
				IntPtr pItem = IntPtr.Add (pFirst, i * elementSize);

				object boxed =
					Marshal.PtrToStructure (pItem, typeof (WSDSPAIR));

				WSDSPAIR item = (WSDSPAIR)boxed;

				string key = null;

				if (item.lpKey != IntPtr.Zero)
					key = Marshal.PtrToStringUni (item.lpKey);

				Dictionary<uint, string> valueDict =
					ParseDWSPAIRLIST (item.lpValue);

				result [key] = valueDict;
			}

			return result;
		}
	}
	public static class LpcwstrListHelper
	{
		public static IntPtr Create (IEnumerable<string> strings)
		{
			if (strings == null) return IntPtr.Zero;
			var list = new List<string> (strings);
			int count = list.Count;
			int size = sizeof (uint) + IntPtr.Size * count;
			IntPtr pMem = Marshal.AllocHGlobal (size);
			Marshal.WriteInt32 (pMem, count);
			IntPtr pArray = pMem + sizeof (uint);
			for (int i = 0; i < count; i++)
			{
				IntPtr pStr = Marshal.StringToHGlobalUni (list [i]);
				Marshal.WriteIntPtr (pArray, i * IntPtr.Size, pStr);
			}
			return pMem;
		}
		public static void Destroy (IntPtr pList)
		{
			if (pList == IntPtr.Zero)
				return;

			int count = Marshal.ReadInt32 (pList);
			IntPtr pArray = pList + sizeof (uint);

			for (int i = 0; i < count; i++)
			{
				IntPtr pStr = Marshal.ReadIntPtr (pArray, i * IntPtr.Size);
				if (pStr != IntPtr.Zero)
					Marshal.FreeHGlobal (pStr);
			}
			Marshal.FreeHGlobal (pList);
		}
	}
}

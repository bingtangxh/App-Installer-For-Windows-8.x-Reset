using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ModernNotice
{
	// HRESULT = int
	[StructLayout (LayoutKind.Sequential)]
	public struct HRESULT
	{
		public int Value;
	}

	// Callback delegate
	[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
	public delegate void NOTICE_ACTIVECALLBACK (IntPtr pCustom);

	public static class Native
	{
		private const string DLL = "notice.dll";  // 改成你的 dll 名称

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetToastNoticeXml (string lpTemplateName);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GenerateSimpleToastNoticeXml (string lpText, string lpImagePath);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GenerateSimpleToastNoticeXml2 (string lpTitle, string lpText, string lpImagePath);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNoticeFromXmlDocument (
			string lpIdName,
			string lpXmlString,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNotice (
			string lpIdName,
			string lpText,
			string lpImgPath,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNotice2 (
			string lpIdName,
			string lpTitle,
			string lpText,
			string lpImgPath,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNoticeWithIStream2 (
			string lpIdName,
			string lpTitle,
			string lpText,
			IntPtr pIImgStream,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNoticeWithIStream (
			string lpIdName,
			string lpText,
			IntPtr pIImgStream,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);

		[DllImport (DLL, CallingConvention = CallingConvention.Cdecl)]
		public static extern int NoticeGetLastHResult ();

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr NoticeGetLastDetailMessage ();

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateShortcutWithAppIdW (
			string pszShortcutPath,
			string pszTargetPath,
			string pszAppId,
			out IntPtr lpException
		);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern void NoticeApiFreeString (IntPtr lpstr);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNoticeWithImgBase64 (
			string lpIdName,
			string lpText,
			string lpImgBase64,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);

		[DllImport (DLL, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateToastNotice2WithImgBase64 (
			string lpIdName,
			string lpTitle,
			string lpText,
			string lpImgBase64,
			NOTICE_ACTIVECALLBACK pfCallback,
			IntPtr pCustom,
			out IntPtr lpExceptMsg
		);
	}
}

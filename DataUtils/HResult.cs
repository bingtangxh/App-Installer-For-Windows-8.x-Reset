using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DataUtils
{
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_HResult
	{
		private int hr;
		private string errorcode;
		private string detailmsg;
		public _I_HResult (int hres)
		{
			hr = hres;
			errorcode = null;
			detailmsg = HResultToMessage (hr) ?? string.Empty;
		}
		public _I_HResult (int hres, string error, string message)
		{
			hr = hres;
			errorcode = error ?? string.Empty;
			detailmsg = message ?? string.Empty;
		}
		// Properties (read-only as in your C++/CLI)
		public int HResult
		{
			get { return hr; }
		}
		// If user provided an explicit error code string use it; otherwise return the hex form "0xXXXXXXXX"
		public string ErrorCode
		{
			get
			{
				if (!string.IsNullOrEmpty (errorcode)) return errorcode;
				return "0x" + hr.ToString ("X8", System.Globalization.CultureInfo.InvariantCulture);
			}
		}
		public string Message
		{
			get { return detailmsg; }
		}
		public bool Succeeded
		{
			get { return hr >= 0; } // SUCCEEDED macro: hr >= 0
		}
		public bool Failed
		{
			get { return hr < 0; } // FAILED macro: hr < 0
		}
		public override string ToString ()
		{
			return string.Format (System.Globalization.CultureInfo.InvariantCulture,
				"HResult={0}, ErrorCode={1}, Message={2}", hr, ErrorCode, Message);
		}
		// Try to obtain a user-friendly message for the HRESULT.
		// First try Marshal.GetExceptionForHR, then fallback to FormatMessage.
		private static string HResultToMessage (int hresult)
		{
			try
			{
				Exception ex = Marshal.GetExceptionForHR (hresult);
				if (ex != null)
				{
					string msg = ex.Message;
					if (!string.IsNullOrEmpty (msg)) return msg;
				}
			}
			catch
			{
			}
			string fmt = FormatMessageFromSystem (hresult);
			if (!string.IsNullOrEmpty (fmt)) return fmt;
			int win32Code = hresult & 0xFFFF;
			fmt = FormatMessageFromSystem (win32Code);
			if (!string.IsNullOrEmpty (fmt)) return fmt;
			return string.Empty;
		}
		private static string FormatMessageFromSystem (int messageId)
		{
			const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
			const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
			const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
			int flags = FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS;
			StringBuilder sb = new StringBuilder (512);

			int res = FormatMessage (flags, IntPtr.Zero, messageId, 0, sb, sb.Capacity, IntPtr.Zero);
			if (res != 0)
			{
				// Trim trailing newlines that FormatMessage often appends
				return sb.ToString ().TrimEnd ('\r', '\n', ' ');
			}
			return null;
		}
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int FormatMessage (int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, IntPtr Arguments);
	}
}

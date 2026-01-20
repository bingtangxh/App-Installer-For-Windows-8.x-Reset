using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DataUtils
{
	public enum OLECMDID
	{
		OLECMDID_OPTICAL_ZOOM = 63
	}
	public enum OLECMDEXECOPT
	{
		OLECMDEXECOPT_DODEFAULT = 0,
		OLECMDEXECOPT_DONTPROMPTUSER = 2
	}
	[ComImport]
	[Guid ("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E")]
	[InterfaceType (ComInterfaceType.InterfaceIsIDispatch)]
	public interface IWebBrowser2
	{
		[DispId (0x000001F4)]
		void ExecWB (
			OLECMDID cmdID,
			OLECMDEXECOPT cmdexecopt,
			[In, Optional] object pvaIn,
			[Out, Optional] object pvaOut
		);
	}
	public static class WebBrowserHelper
	{
		static Guid CGID_MSHTML = new Guid ("DE4BA900-59CA-11CF-9592-444553540000");
		public static IWebBrowser2 GetWebBrowser2 (WebBrowser browser)
		{
			return browser.ActiveXInstance as IWebBrowser2;
		}
	}
	public interface IWebBrowserPageScale
	{
		int PageScale { get; set; }
	}
	[ComImport]
	[Guid ("B722BCCB-4E68-101B-A2BC-00AA00404770")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	public interface IOleCommandTarget
	{
		[PreserveSig]
		int Exec (
			ref Guid pguidCmdGroup,
			uint nCmdID,
			uint nCmdexecopt,
			ref object pvaIn,
			ref object pvaOut
		);
	}


}

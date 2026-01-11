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
			ref object pvaIn,
			ref object pvaOut
		);
	}
	public static class WebBrowserHelper
	{
		public static IWebBrowser2 GetWebBrowser2 (WebBrowser browser)
		{
			return browser.ActiveXInstance as IWebBrowser2;
		}
	}
	public interface IWebBrowserPageScale
	{
		int PageScale { get; set; }
	}
}

using System.Xml;
using System.Runtime.InteropServices;
using HResult = DataUtils._I_HResult;
using System;

namespace ModernNotice
{
	public static class Notice
    {
		private static XmlDocument XmlStringToDom (string xmlContent)
		{
			var ret = new XmlDocument ();
			ret.LoadXml (xmlContent);
			return ret;
		}
		private static string XmlDomToString (XmlDocument xmlDom) { return xmlDom.OuterXml; }
		private static HResult BuildHResult (int hr, IntPtr err, IntPtr msg) { return new HResult (hr, Marshal.PtrToStringUni (err), Marshal.PtrToStringUni (msg)); }
		private static HResult BuildHResult (HRESULT hr, IntPtr err, IntPtr msg) { return BuildHResult (hr.Value, err, msg); }
		private static HResult BuildHResult (int hr, IntPtr msg) { return new HResult (hr, "", Marshal.PtrToStringUni (msg)); }
		private static HResult BuildHResult (HRESULT hr, IntPtr msg) { return BuildHResult (hr.Value, msg); }
		private static HResult BuildHResult (int hr) { return new HResult (hr); }
		private static HResult BuildHResult (HRESULT hr) { return BuildHResult (hr.Value); }
		public static string GetTemplateString (string templateName)
		{
			var ptr = Native.GetToastNoticeXml (templateName);
			try { var ret = Marshal.PtrToStringUni (ptr) ?? ""; return ret; }
			finally { Native.NoticeApiFreeString (ptr); }
		}
		public static XmlDocument GetTemplate (string templateName) { return XmlStringToDom (GetTemplateString (templateName)); }
		public static string GetSimpleTemplateString (string content, string imagePath)
		{
			var ptr = Native.GenerateSimpleToastNoticeXml (content, imagePath);
			try { var ret = Marshal.PtrToStringUni (ptr) ?? ""; return ret; }
			finally { Native.NoticeApiFreeString (ptr); }
		}
		public static XmlDocument GetSimpleTemplate (string content, string imagePath = null) { return XmlStringToDom (GetSimpleTemplateString (content, imagePath)); }
		public static string GetSimpleTemplateString2 (string title, string content = null, string imagePath = null)
		{
			var ptr = Native.GenerateSimpleToastNoticeXml2 (title, content, imagePath);
			try { var ret = Marshal.PtrToStringUni (ptr) ?? ""; return ret; }
			finally { Native.NoticeApiFreeString (ptr); }
		}
		public static XmlDocument GetSimpleTemplate2 (string title, string content = null, string imagePath = null) { return XmlStringToDom (GetSimpleTemplateString2 (title, content, imagePath)); }
		public static HResult Create (string appUserId, XmlDocument xml)
		{
			IntPtr dt = IntPtr.Zero;
			try
			{
				var hr = Native.CreateToastNoticeFromXmlDocument (appUserId, XmlDomToString (xml), null, IntPtr.Zero, out dt);
				return BuildHResult (hr, dt);
			}
			finally
			{
				if (dt != IntPtr.Zero) Native.NoticeApiFreeString (dt);
			}
		}
		public static HResult Create (string appUserId, string content, string imagePath = null)
		{
			var xml = GetSimpleTemplate (content, imagePath);
			return Create (appUserId, xml);
		}
		public static HResult Create (string appUserId, string title, string content, string imagePath = null)
		{
			var xml = GetSimpleTemplate2 (title, content, imagePath);
			return Create (appUserId, xml);
		}
		public static HResult Create (string appUserId, string content, IntPtr img)
		{
			IntPtr dt = IntPtr.Zero;
			try
			{
				var hr = Native.CreateToastNoticeWithIStream (appUserId, content, img, null, IntPtr.Zero, out dt);
				return BuildHResult (hr, dt);
			}
			finally
			{
				if (dt != IntPtr.Zero) Native.NoticeApiFreeString (dt);
			}
		}
		public static HResult Create (string appUserId, string title, string content, IntPtr img)
		{
			IntPtr dt = IntPtr.Zero;
			try
			{
				var hr = Native.CreateToastNoticeWithIStream2 (appUserId, title, content, img, null, IntPtr.Zero, out dt);
				return BuildHResult (hr, dt);
			}
			finally
			{
				if (dt != IntPtr.Zero) Native.NoticeApiFreeString (dt);
			}
		}
		public static HResult CreateWithImgBase64 (string appUserId, string content, string imageBase64)
		{
			IntPtr dt = IntPtr.Zero;
			try
			{
				var hr = Native.CreateToastNoticeWithImgBase64 (appUserId, content, imageBase64, null, IntPtr.Zero, out dt);
				return BuildHResult (hr, dt);
			}
			finally
			{
				if (dt != IntPtr.Zero) Native.NoticeApiFreeString (dt);
			}
		}
		public static HResult CreateWithImgBase64 (string appUserId, string title, string content, string imageBase64)
		{
			IntPtr dt = IntPtr.Zero;
			try
			{
				var hr = Native.CreateToastNotice2WithImgBase64 (appUserId, title, content, imageBase64, null, IntPtr.Zero, out dt);
				return BuildHResult (hr, dt);
			}
			finally
			{
				if (dt != IntPtr.Zero) Native.NoticeApiFreeString (dt);
			}
		}
	}
}

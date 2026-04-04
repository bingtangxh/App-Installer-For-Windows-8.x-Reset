using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace DataUtils
{
	[ComVisible (true)]
	[InterfaceType (ComInterfaceType.InterfaceIsDual)]
	public interface IHttpResponse: IDisposable
	{
		int Status { get; }              // 兼容旧版，等同于 StatusCode
		int StatusCode { get; }
		string StatusText { get; }       // 等同于 StatusDescription
		string StatusDescription { get; }
		string ResponseUrl { get; }
		Uri ResponseUri { get; }
		string ContentType { get; }
		long ContentLength { get; }      // 注意：JS 中 Number 可表示 2^53 以内整数
		string CharacterSet { get; }
		string ContentEncoding { get; }
		DateTime LastModified { get; }
		string Method { get; }           // 原始请求方法 (GET, POST...)
		Version ProtocolVersion { get; } // 如 1.1，JS 中可能转为字符串
		bool IsFromCache { get; }
		bool IsMutuallyAuthenticated { get; }
		string Text ();
		_I_Enumerable Bytes ();
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class HttpResponse: IHttpResponse, IDisposable
	{
		private readonly byte [] _data;
		private readonly Dictionary<string, string> _headersDict;
		private bool _disposed = false;
		public int Status => StatusCode;
		public int StatusCode { get; private set; }
		public string StatusText => StatusDescription;
		public string StatusDescription { get; private set; }
		public string ResponseUrl => ResponseUri?.ToString ();
		public Uri ResponseUri { get; private set; }
		public string ContentType { get; private set; }
		public long ContentLength { get; private set; }
		public string CharacterSet { get; private set; }
		public string ContentEncoding { get; private set; }
		public DateTime LastModified { get; private set; }
		public string Method { get; private set; }
		public Version ProtocolVersion { get; private set; }
		public bool IsFromCache { get; private set; }
		public bool IsMutuallyAuthenticated { get; private set; }
		public HttpResponse (HttpWebResponse response)
		{
			if (response == null) throw new ArgumentNullException (nameof (response));
			using (var stream = response.GetResponseStream ())
			using (var ms = new MemoryStream ())
			{
				stream.CopyTo (ms);
				_data = ms.ToArray ();
			}
			StatusCode = (int)response.StatusCode;
			StatusDescription = response.StatusDescription;
			ResponseUri = response.ResponseUri;
			Method = response.Method;
			ProtocolVersion = new Version ((ushort)response.ProtocolVersion.Major, (ushort)response.ProtocolVersion.Minor, (ushort)response.ProtocolVersion.Build, (ushort)response.ProtocolVersion.Revision);
			IsFromCache = response.IsFromCache;
			IsMutuallyAuthenticated = response.IsMutuallyAuthenticated;
			ContentType = response.ContentType ?? "";
			ContentLength = response.ContentLength;
			CharacterSet = response.CharacterSet ?? "";
			ContentEncoding = response.ContentEncoding ?? "";
			LastModified = response.LastModified;
			_headersDict = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			foreach (string key in response.Headers.AllKeys)
			{
				if (!string.IsNullOrEmpty (key))
					_headersDict [key] = response.Headers [key];
			}
		}
		public HttpResponse (int statusCode, string statusDescription, string responseUrl, byte [] data, Dictionary<string, string> headers)
		{
			StatusCode = statusCode;
			StatusDescription = statusDescription ?? "";
			ResponseUri = string.IsNullOrEmpty (responseUrl) ? null : new Uri (responseUrl);
			_data = data ?? new byte [0];
			_headersDict = headers ?? new Dictionary<string, string> ();
			ContentType = GetHeader ("Content-Type") ?? "";
			ContentLength = _data.Length;
			CharacterSet = "";
			ContentEncoding = "";
			LastModified = DateTime.MinValue;
			Method = "";
			ProtocolVersion = new Version (0, 0);
			IsFromCache = false;
			IsMutuallyAuthenticated = false;
		}
		public string GetHeader (string sName)
		{
			if (string.IsNullOrEmpty (sName)) return null;
			try
			{
				return _headersDict [sName];
			}
			catch { return null; }
		}
		public string GetHeadersToJson ()
		{
			return JsonConvert.SerializeObject (_headersDict);
		}
		public string Text ()
		{
			string charset = CharacterSet;
			Encoding enc = Encoding.UTF8;
			if (!string.IsNullOrEmpty (charset))
			{
				try { enc = Encoding.GetEncoding (charset); }
				catch { /* 保持 UTF-8 */ }
			}
			return enc.GetString (_data);
		}
		public _I_Enumerable Bytes ()
		{
			var list = new List<object> ();
			foreach (byte b in _data)
				list.Add (b);
			return new _I_List (list);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing)
		{
			if (!_disposed)
			{
				_disposed = true;
			}
		}
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class HttpRequest: IDisposable
	{
		private string _method;
		private string _url;
		private Dictionary<string, string> _headers = new Dictionary<string, string> ();
		public int Timeout { get; set; } = 100000;          // 毫秒，默认 100 秒
		public int ReadWriteTimeout { get; set; } = 300000; // 读写超时，默认 5 分钟
		public bool AllowAutoRedirect { get; set; } = true;
		public bool AllowWriteStreamBuffering { get; set; } = true;
		public bool KeepAlive { get; set; } = true;
		public int MaximumAutomaticRedirections { get; set; } = 50;
		public string UserAgent
		{
			get { return GetHeader ("User-Agent"); }
			set { SetHeader ("User-Agent", value); }
		}
		public string Referer
		{
			get { return GetHeader ("Referer"); }
			set { SetHeader ("Referer", value); }
		}
		public string ContentType
		{
			get { return GetHeader ("Content-Type"); }
			set { SetHeader ("Content-Type", value); }
		}
		public string Accept
		{
			get { return GetHeader ("Accept"); }
			set { SetHeader ("Accept", value); }
		}
		public IWebProxy Proxy { get; set; } = null;
		public CookieContainer CookieContainer { get; set; } = null;
		private string _httpver = "1.1";
		public string ProtocolVersion
		{
			get { return _httpver; }
			set { _httpver = value; }
		}
		public System.Version ProtVer
		{
			get
			{
				switch (_httpver)
				{
					case "1.0": return HttpVersion.Version10;
					default:
					case "1.1": return HttpVersion.Version11;
				}
			}
		}
		public bool PreAuthenticate { get; set; } = false;
		public ICredentials Credentials { get; set; } = null;
		public bool AutomaticDecompression { get; set; } = false;
		public Action<long, long> UploadProgressCallback { get; set; } = null;
		public void Open (string sMethod, string sUrl)
		{
			_method = sMethod;
			_url = sUrl;
		}
		public void SetHeader (string sName, string sValue) => _headers [sName] = sValue;
		public string GetHeader (string sName) => _headers [sName];
		public string GetHeadersToJson () => JsonConvert.SerializeObject (_headers);
		public void RemoveHeader (string sName) => _headers.Remove (sName);
		public void ClearHeader () => _headers.Clear ();
		public IHttpResponse Send (string sBody, string encoding)
		{
			var req = (HttpWebRequest)WebRequest.Create (_url);
			req.Method = _method;
			req.Timeout = Timeout;
			req.ReadWriteTimeout = ReadWriteTimeout;
			req.AllowAutoRedirect = AllowAutoRedirect;
			req.AllowWriteStreamBuffering = AllowWriteStreamBuffering;
			req.KeepAlive = KeepAlive;
			req.MaximumAutomaticRedirections = MaximumAutomaticRedirections;
			if (Proxy != null) req.Proxy = Proxy;
			if (CookieContainer != null) req.CookieContainer = CookieContainer;
			req.ProtocolVersion = ProtVer;
			req.PreAuthenticate = PreAuthenticate;
			if (Credentials != null) req.Credentials = Credentials;
			if (AutomaticDecompression)
				req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			if (_headers.ContainsKey ("User-Agent"))
				req.UserAgent = _headers ["User-Agent"];
			foreach (var h in _headers)
			{
				if (string.Equals (h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase))
					continue;
				if (string.Equals (h.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
				{
					string ct = h.Value;
					if (!string.IsNullOrEmpty (sBody) && !ct.Contains ("charset"))
					{
						Encoding enc = Encoding.GetEncoding (encoding);
						ct = ct + "; charset=" + enc.WebName;
					}
					req.ContentType = ct;
				}
				else if (string.Equals (h.Key, "Accept", StringComparison.OrdinalIgnoreCase))
				{
					req.Accept = h.Value;
				}
				else if (string.Equals (h.Key, "Referer", StringComparison.OrdinalIgnoreCase))
				{
					req.Referer = h.Value;
				}
				else
				{
					req.Headers [h.Key] = h.Value;
				}
			}

			// 如果没有显式设置 Content-Type 且是 POST/PUT 且有请求体，设置默认值
			bool hasContentType = _headers.Keys.Any (k => string.Equals (k, "Content-Type", StringComparison.OrdinalIgnoreCase));
			if (!hasContentType && (string.Equals (_method, "POST", StringComparison.OrdinalIgnoreCase) ||
									string.Equals (_method, "PUT", StringComparison.OrdinalIgnoreCase)) &&
				!string.IsNullOrEmpty (sBody))
			{
				Encoding enc = Encoding.GetEncoding (encoding);
				req.ContentType = "application/x-www-form-urlencoded; charset=" + enc.WebName;
			}

			// 写入请求体
			if (!string.IsNullOrEmpty (sBody))
			{
				Encoding enc = Encoding.GetEncoding (encoding);
				byte [] bytes = enc.GetBytes (sBody);
				req.ContentLength = bytes.Length;
				using (var stream = req.GetRequestStream ())
				{
					if (UploadProgressCallback != null)
					{
						int totalWritten = 0;
						int bufferSize = 8192;
						for (int offset = 0; offset < bytes.Length; offset += bufferSize)
						{
							int chunkSize = Math.Min (bufferSize, bytes.Length - offset);
							stream.Write (bytes, offset, chunkSize);
							totalWritten += chunkSize;
							UploadProgressCallback (totalWritten, bytes.Length);
						}
					}
					else
					{
						stream.Write (bytes, 0, bytes.Length);
					}
				}
			}
			using (var res = (HttpWebResponse)req.GetResponse ())
			{
				return new HttpResponse (res);
			}
		}
		public void SendAsync (string sBody, string encoding, object pfResolve)
		{
			System.Threading.ThreadPool.QueueUserWorkItem (delegate
			{
				JsUtils.Call (pfResolve, Send (sBody, encoding));
			});
		}
		public void Dispose () { }
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Http
	{
		public HttpRequest CreateHttpRequest () => new HttpRequest ();
	}
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Web
	{
		public _I_Http Http => new _I_Http ();
	}
}

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace DataUtils
{
	[ComVisible (false)]
	public sealed class DownloadHelper
	{
		private static readonly object s_downloadLock = new object (); // mimic g_download_cs

		// Public static entry (starts background thread)
		public static void DownloadFile (string httpUrl, string savePath, object onProgress, object onComplete, object onError)
		{
			if (string.IsNullOrEmpty (httpUrl)) throw new ArgumentNullException ("httpUrl");
			if (string.IsNullOrEmpty (savePath)) throw new ArgumentNullException ("savePath");

			var helper = new DownloadHelper (httpUrl, savePath, onProgress, onComplete, onError);
			Thread th = new Thread (helper.Worker);
			th.IsBackground = true;
			th.Start ();
		}

		// Instance members
		private readonly string _url;
		private readonly string _savePath;
		private readonly object _cbProgress;
		private readonly object _cbComplete;
		private readonly object _cbError;

		private DownloadHelper (string url, string savePath, object cbProgress, object cbComplete, object cbError)
		{
			_url = url;
			_savePath = savePath;
			_cbProgress = cbProgress;
			_cbComplete = cbComplete;
			_cbError = cbError;
		}

		private void Worker ()
		{
			// Single download at a time (mimic CreateScopedLock)
			lock (s_downloadLock)
			{
				HttpWebRequest request = null;
				HttpWebResponse response = null;
				Stream responseStream = null;
				FileStream fileStream = null;

				try
				{
					request = (HttpWebRequest)WebRequest.Create (_url);
					request.Method = "GET";
					request.AllowAutoRedirect = true;
					request.UserAgent = "Mozilla/5.0 (Windows NT 6.2; Win32; x86) AppInstallerUpdater/1.0";
					request.Timeout = 60000; // 60s connect timeout
					request.ReadWriteTimeout = 60000;

					response = (HttpWebResponse)request.GetResponse ();
					long contentLength = -1;
					try { contentLength = response.ContentLength; } catch { contentLength = -1; }

					responseStream = response.GetResponseStream ();

					// create directory if needed
					string dir = Path.GetDirectoryName (_savePath);
					if (!string.IsNullOrEmpty (dir) && !Directory.Exists (dir))
						Directory.CreateDirectory (dir);

					fileStream = new FileStream (_savePath, FileMode.Create, FileAccess.Write, FileShare.Read);

					byte [] buffer = new byte [8192];
					int bytesRead;
					long received = 0;

					var lastCheck = DateTime.UtcNow;
					long lastBytes = 0;
					const double reportIntervalSeconds = 0.5;

					while ((bytesRead = SafeRead (responseStream, buffer, 0, buffer.Length)) > 0)
					{
						fileStream.Write (buffer, 0, bytesRead);
						received += bytesRead;

						var now = DateTime.UtcNow;
						double interval = (now - lastCheck).TotalSeconds;
						long speed = -1;
						if (interval >= reportIntervalSeconds)
						{
							long bytesInInterval = received - lastBytes;
							if (interval > 0)
								speed = (long)(bytesInInterval / interval); // B/s
							lastCheck = now;
							lastBytes = received;
						}

						ReportProgress (received, contentLength >= 0 ? contentLength : 0, speed);
					}

					// flush and close file
					fileStream.Flush ();

					ReportComplete (_savePath, received);
				}
				catch (WebException wex)
				{
					string reason = BuildWebExceptionReason (wex);
					ReportError (_savePath, reason);
				}
				catch (Exception ex)
				{
					ReportError (_savePath, ex.Message ?? ex.ToString ());
				}
				finally
				{
					try { if (responseStream != null) responseStream.Close (); } catch { }
					try { if (response != null) response.Close (); } catch { }
					try { if (fileStream != null) fileStream.Close (); } catch { }
				}
			}
		}

		// Safe read wrapper to handle potential stream interruptions
		private static int SafeRead (Stream s, byte [] buffer, int offset, int count)
		{
			try
			{
				return s.Read (buffer, offset, count);
			}
			catch
			{
				return 0;
			}
		}

		// Build a user-friendly reason text from WebException (includes status / inner messages)
		private static string BuildWebExceptionReason (WebException wex)
		{
			try
			{
				StringBuilder sb = new StringBuilder ();
				sb.Append ("WebException: ");
				sb.Append (wex.Status.ToString ());
				if (wex.Response != null)
				{
					try
					{
						var resp = (HttpWebResponse)wex.Response;
						sb.AppendFormat (CultureInfo.InvariantCulture, " (HTTP {0})", (int)resp.StatusCode);
					}
					catch { }
				}
				if (!string.IsNullOrEmpty (wex.Message))
				{
					sb.Append (" - ");
					sb.Append (wex.Message);
				}
				return sb.ToString ();
			}
			catch
			{
				return wex.Message ?? "Unknown WebException";
			}
		}

		// ---------- Reporting helpers (use Newtonsoft.Json) ----------

		private void ReportProgress (long received, long total, long speed)
		{
			if (_cbProgress == null) return;

			double progress = 0.0;
			if (total > 0) progress = received / (double)total * 100.0;

			var payload = new
			{
				received = received,
				total = total,
				speed = FormatSpeed (speed),
				progress = progress
			};

			string json = JsonConvert.SerializeObject (payload);
			CallJS (_cbProgress, json);
		}

		private void ReportComplete (string file, long size)
		{
			if (_cbComplete == null) return;

			var payload = new
			{
				file = file ?? string.Empty,
				status = "ok",
				size = size
			};

			string json = JsonConvert.SerializeObject (payload);
			CallJS (_cbComplete, json);
		}

		private void ReportError (string file, string reason)
		{
			if (_cbError == null) return;

			var payload = new
			{
				file = file ?? string.Empty,
				status = "failed",
				reason = reason ?? string.Empty
			};

			string json = JsonConvert.SerializeObject (payload);
			CallJS (_cbError, json);
		}

		// Call JS callback object: invoke its "call" method like original code: jsFunc.call(1, arg)
		private void CallJS (object jsFunc, string arg)
		{
			if (jsFunc == null) return;
			try
			{
				// Use reflection to invoke `call` method with (thisArg, arg)
				jsFunc.GetType ().InvokeMember (
					"call",
					BindingFlags.InvokeMethod,
					null,
					jsFunc,
					new object [] { 1, arg });
			}
			catch
			{
				// ignore errors in callback invocation
			}
		}

		// Format speed like original: B/s, KB/s, MB/s, …
		private string FormatSpeed (long speed)
		{
			if (speed < 0) return "--/s";
			string [] units = new string [] { "B/s", "KB/s", "MB/s", "GB/s", "TB/s" };
			double s = (double)speed;
			int idx = 0;
			while (s >= 1024.0 && idx < units.Length - 1)
			{
				s /= 1024.0;
				idx++;
			}
			return string.Format (CultureInfo.InvariantCulture, "{0:0.##} {1}", s, units [idx]);
		}
	}

	// Simple COM-visible wrapper class for JS/COM consumers
	[ComVisible (true)]
	[ClassInterface (ClassInterfaceType.AutoDual)]
	public class _I_Download
	{
		// Keep method order and names similar to your C++/CLI _I_Download.WorkAsync
		public void WorkAsync (string httpurl, string saveFilePath, object onComplete, object onError, object onProgress)
		{
			DownloadHelper.DownloadFile (httpurl, saveFilePath, onProgress, onComplete, onError);
		}
	}
}

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace PriFormat
{
	public static class StreamHelper
	{
		public static Stream FromIStream (IStream comStream)
		{
			if (comStream == null) return null;
			return new ComIStreamBufferedReader (comStream);
		}

		public static Stream FromIStream (IntPtr comStreamPtr)
		{
			if (comStreamPtr == IntPtr.Zero) return null;

			IStream comStream =
				(IStream)Marshal.GetObjectForIUnknown (comStreamPtr);

			return new ComIStreamBufferedReader (comStream);
		}
	}

	internal sealed class ComIStreamBufferedReader: Stream
	{
		private readonly MemoryStream _memory;
		private bool _disposed;

		public ComIStreamBufferedReader (IStream comStream)
		{
			if (comStream == null)
				throw new ArgumentNullException ("comStream");

			_memory = LoadAll (comStream);
		}

		/// <summary>
		/// 一次性把 IStream 全部复制到托管内存
		/// </summary>
		private static MemoryStream LoadAll (IStream stream)
		{
			// 保存原始位置
			long originalPos = GetPosition (stream);

			try
			{
				// Seek 到头
				stream.Seek (0, 0 /* STREAM_SEEK_SET */, IntPtr.Zero);

				// 获取长度
				System.Runtime.InteropServices.ComTypes.STATSTG stat;
				stream.Stat (out stat, 1); // STATFLAG_NONAME
				long length = stat.cbSize;

				if (length < 0 || length > int.MaxValue)
					throw new NotSupportedException ("Stream too large to buffer.");

				MemoryStream ms = new MemoryStream ((int)length);

				byte [] buffer = new byte [64 * 1024]; // 64KB
				IntPtr pcbRead = Marshal.AllocHGlobal (sizeof (int));

				try
				{
					while (true)
					{
						stream.Read (buffer, buffer.Length, pcbRead);
						int read = Marshal.ReadInt32 (pcbRead);
						if (read <= 0)
							break;

						ms.Write (buffer, 0, read);
					}
				}
				finally
				{
					Marshal.FreeHGlobal (pcbRead);
				}

				ms.Position = 0;
				return ms;
			}
			finally
			{
				// 恢复 COM IStream 的原始位置
				stream.Seek (originalPos, 0 /* STREAM_SEEK_SET */, IntPtr.Zero);
			}
		}

		private static long GetPosition (IStream stream)
		{
			IntPtr posPtr = Marshal.AllocHGlobal (sizeof (long));
			try
			{
				stream.Seek (0, 1 /* STREAM_SEEK_CUR */, posPtr);
				return Marshal.ReadInt64 (posPtr);
			}
			finally
			{
				Marshal.FreeHGlobal (posPtr);
			}
		}

		// ===== Stream 重写，全部委托给 MemoryStream =====

		public override bool CanRead { get { return !_disposed; } }
		public override bool CanSeek { get { return !_disposed; } }
		public override bool CanWrite { get { return false; } }

		public override long Length
		{
			get { EnsureNotDisposed (); return _memory.Length; }
		}

		public override long Position
		{
			get { EnsureNotDisposed (); return _memory.Position; }
			set { EnsureNotDisposed (); _memory.Position = value; }
		}

		public override int Read (byte [] buffer, int offset, int count)
		{
			EnsureNotDisposed ();
			return _memory.Read (buffer, offset, count);
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			EnsureNotDisposed ();
			return _memory.Seek (offset, origin);
		}

		public override void Flush ()
		{
			// no-op
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public override void Write (byte [] buffer, int offset, int count)
		{
			throw new NotSupportedException ();
		}

		protected override void Dispose (bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				_memory.Dispose ();
			}

			_disposed = true;
			base.Dispose (disposing);
		}

		private void EnsureNotDisposed ()
		{
			if (_disposed)
				throw new ObjectDisposedException ("ComIStreamBufferedReader");
		}
	}
}

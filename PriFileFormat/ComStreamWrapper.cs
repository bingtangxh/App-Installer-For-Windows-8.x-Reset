using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace PriFileFormat
{
	public class ComStreamWrapper: Stream
	{
		private IStream comStream;
		private object _sync = new object ();
		public ComStreamWrapper (IStream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
			comStream = stream;
		}
		public override bool CanRead
		{
			get { return true; }
		}
		public override bool CanSeek
		{
			get { return true; }
		}
		public override bool CanWrite
		{
			get { return true; }
		}
		public override long Length
		{
			get
			{
				lock (_sync)
				{
					System.Runtime.InteropServices.ComTypes.STATSTG stat;
					comStream.Stat (out stat, 1); // STATFLAG_NONAME = 1
					return stat.cbSize;
				}
			}
		}
		public override long Position
		{
			get
			{
				lock (_sync)
				{
					IntPtr posPtr = Marshal.AllocHGlobal (sizeof (long));
					try
					{
						// SEEK_CUR = 1
						comStream.Seek (0, 1, posPtr);
						return Marshal.ReadInt64 (posPtr);
					}
					finally
					{
						Marshal.FreeHGlobal (posPtr);
					}
				}
			}
			set
			{
				lock (_sync)
				{
					// SEEK_SET = 0
					comStream.Seek (value, 0, IntPtr.Zero);
				}
			}
		}
		public override void Flush ()
		{
			lock (_sync)
			{
				comStream.Commit (0); // STGC_DEFAULT = 0
			}
		}
		public override int Read (byte [] buffer, int offset, int count)
		{
			lock (_sync)
			{
				if (offset != 0)
					throw new NotSupportedException ("Offset != 0 not supported in this wrapper.");

				IntPtr bytesRead = Marshal.AllocHGlobal (sizeof (int));
				try
				{
					comStream.Read (buffer, count, bytesRead);
					return Marshal.ReadInt32 (bytesRead);
				}
				finally
				{
					Marshal.FreeHGlobal (bytesRead);
				}
			}
		}
		public override void Write (byte [] buffer, int offset, int count)
		{
			lock (_sync)
			{
				if (offset != 0)
					throw new NotSupportedException ("Offset != 0 not supported in this wrapper.");

				IntPtr bytesWritten = Marshal.AllocHGlobal (sizeof (int));
				try
				{
					comStream.Write (buffer, count, bytesWritten);
				}
				finally
				{
					Marshal.FreeHGlobal (bytesWritten);
				}
			}
		}
		public override long Seek (long offset, SeekOrigin origin)
		{
			lock (_sync)
			{
				int originInt = 0;
				switch (origin)
				{
					case SeekOrigin.Begin: originInt = 0; break; // STREAM_SEEK_SET
					case SeekOrigin.Current: originInt = 1; break; // STREAM_SEEK_CUR
					case SeekOrigin.End: originInt = 2; break; // STREAM_SEEK_END
				}

				IntPtr posPtr = Marshal.AllocHGlobal (sizeof (long));
				try
				{
					comStream.Seek (offset, originInt, posPtr);
					return Marshal.ReadInt64 (posPtr);
				}
				finally
				{
					Marshal.FreeHGlobal (posPtr);
				}
			}
		}
		public override void SetLength (long value)
		{
			lock (_sync)
			{
				comStream.SetSize (value);
			}
		}
		~ComStreamWrapper () { comStream = null;}
	}
}

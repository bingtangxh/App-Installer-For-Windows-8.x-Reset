using System;
using System.IO;

namespace PriFormat
{
	internal sealed class SubStream: Stream
	{
		private readonly Stream _baseStream;
		private readonly long _baseOffset;
		private readonly long _length;
		private long _position;

		public SubStream (Stream baseStream, long offset, long length)
		{
			if (baseStream == null)
				throw new ArgumentNullException ("baseStream");
			if (!baseStream.CanSeek)
				throw new ArgumentException ("Base stream must be seekable.");

			_baseStream = baseStream;
			_baseOffset = offset;
			_length = length;
			_position = 0;
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return true; } }
		public override bool CanWrite { get { return false; } }

		public override long Length
		{
			get { return _length; }
		}

		public override long Position
		{
			get { return _position; }
			set
			{
				if (value < 0 || value > _length)
					throw new ArgumentOutOfRangeException ("value");
				_position = value;
			}
		}

		public override int Read (byte [] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");
			if (offset < 0 || count < 0 || buffer.Length - offset < count)
				throw new ArgumentOutOfRangeException ();

			long remaining = _length - _position;
			if (remaining <= 0)
				return 0;

			if (count > remaining)
				count = (int)remaining;

			_baseStream.Position = _baseOffset + _position;
			int read = _baseStream.Read (buffer, offset, count);
			_position += read;
			return read;
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			long target;

			switch (origin)
			{
				case SeekOrigin.Begin:
					target = offset;
					break;

				case SeekOrigin.Current:
					target = _position + offset;
					break;

				case SeekOrigin.End:
					target = _length + offset;
					break;

				default:
					throw new ArgumentException ("origin");
			}

			if (target < 0 || target > _length)
				throw new IOException ("Seek out of range.");

			_position = target;
			return _position;
		}

		public override void Flush ()
		{
			// no-op (read-only)
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public override void Write (byte [] buffer, int offset, int count)
		{
			throw new NotSupportedException ();
		}
	}
}

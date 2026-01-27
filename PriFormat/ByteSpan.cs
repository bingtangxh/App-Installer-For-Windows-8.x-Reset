using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PriFormat
{
	public struct ByteSpan
	{
		public long Offset { get; private set; }
		public uint Length { get; private set; }
		public ByteSpan (long offset, uint length)
		{
			Offset = offset;
			Length = length;
		}
		public override string ToString ()
		{
			return "ByteSpan | Offset = " + Offset + "\t, Length = " + Length;
		}
	}
}

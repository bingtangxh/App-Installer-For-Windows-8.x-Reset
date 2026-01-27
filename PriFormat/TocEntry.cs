using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PriFormat
{
	public sealed class TocEntry
	{
		public string SectionIdentifier { get; private set; }
		public ushort Flags { get; private set; }
		public ushort SectionFlags { get; private set; }
		public uint SectionQualifier { get; private set; }
		public uint SectionOffset { get; private set; }
		public uint SectionLength { get; private set; }
		private TocEntry () { }
		internal static TocEntry Parse (BinaryReader reader)
		{
			return new TocEntry
			{
				SectionIdentifier = new string (reader.ReadChars (16)),
				Flags = reader.ReadUInt16 (),
				SectionFlags = reader.ReadUInt16 (),
				SectionQualifier = reader.ReadUInt32 (),
				SectionOffset = reader.ReadUInt32 (),
				SectionLength = reader.ReadUInt32 ()
			};
		}
		public override string ToString ()
		{
			return SectionIdentifier.TrimEnd ('\0', ' ') + "\t length: " + SectionLength;
		}
	}
}

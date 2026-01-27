using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PriFormat
{
	public abstract class Section: IDisposable
	{
		protected PriFile PriFile { get; private set; }

		public string SectionIdentifier { get; private set; }
		public uint SectionQualifier { get; private set; }
		public uint Flags { get; private set; }
		public uint SectionFlags { get; private set; }
		public uint SectionLength { get; private set; }

		protected Section (string sectionIdentifier, PriFile priFile)
		{
			if (sectionIdentifier == null)
				throw new ArgumentNullException ("sectionIdentifier");

			if (sectionIdentifier.Length != 16)
				throw new ArgumentException (
					"Section identifiers must be exactly 16 characters long.",
					"sectionIdentifier");

			SectionIdentifier = sectionIdentifier;
			PriFile = priFile;
		}

		internal bool Parse (BinaryReader binaryReader)
		{
			// identifier
			string identifier = new string (binaryReader.ReadChars (16));
			if (identifier != SectionIdentifier)
				throw new InvalidDataException ("Unexpected section identifier.");

			SectionQualifier = binaryReader.ReadUInt32 ();
			Flags = binaryReader.ReadUInt16 ();
			SectionFlags = binaryReader.ReadUInt16 ();
			SectionLength = binaryReader.ReadUInt32 ();

			binaryReader.ExpectUInt32 (0);

			// 跳到 section 尾部校验
			long contentLength = SectionLength - 16 - 24;

			binaryReader.BaseStream.Seek (contentLength, SeekOrigin.Current);

			binaryReader.ExpectUInt32 (0xDEF5FADE);
			binaryReader.ExpectUInt32 (SectionLength);

			// 回到 section 内容起始位置
			binaryReader.BaseStream.Seek (-8 - contentLength, SeekOrigin.Current);

			//关键点：SubStream + BinaryReader 生命周期
			using (SubStream subStream = new SubStream (
				binaryReader.BaseStream,
				binaryReader.BaseStream.Position,
				contentLength))
			{
				using (BinaryReader subReader =
					new BinaryReader (subStream, Encoding.ASCII))
				{
					return ParseSectionContent (subReader);
				}
			}
		}

		protected abstract bool ParseSectionContent (BinaryReader binaryReader);

		public override string ToString ()
		{
			return SectionIdentifier.TrimEnd ('\0', ' ') +
				   " length: " + SectionLength;
		}

		internal static Section CreateForIdentifier (
			string sectionIdentifier,
			PriFile priFile)
		{
			if (sectionIdentifier == null)
				throw new ArgumentNullException ("sectionIdentifier");

			switch (sectionIdentifier)
			{
				case PriDescriptorSection.Identifier:
					return new PriDescriptorSection (priFile);

				case HierarchicalSchemaSection.Identifier1:
					return new HierarchicalSchemaSection (priFile, false);

				case HierarchicalSchemaSection.Identifier2:
					return new HierarchicalSchemaSection (priFile, true);

				case DecisionInfoSection.Identifier:
					return new DecisionInfoSection (priFile);

				case ResourceMapSection.Identifier1:
					return new ResourceMapSection (priFile, false);

				case ResourceMapSection.Identifier2:
					return new ResourceMapSection (priFile, true);

				case DataItemSection.Identifier:
					return new DataItemSection (priFile);

				case ReverseMapSection.Identifier:
					return new ReverseMapSection (priFile);

				case ReferencedFileSection.Identifier:
					return new ReferencedFileSection (priFile);

				default:
					return new UnknownSection (sectionIdentifier, priFile);
			}
		}

		public virtual void Dispose ()
		{
			this.PriFile = null;
		}
	}
}

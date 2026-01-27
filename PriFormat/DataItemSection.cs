using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace PriFormat
{
	public class DataItemSection: Section
	{
		public IList<ByteSpan> DataItems { get; private set; }

		internal const string Identifier = "[mrm_dataitem] \0";

		internal DataItemSection (PriFile priFile)
			: base (Identifier, priFile)
		{
		}

		protected override bool ParseSectionContent (BinaryReader binaryReader)
		{
			long sectionPosition = 0;
			SubStream sub = binaryReader.BaseStream as SubStream;
			if (sub != null)
				sectionPosition = sub.Position;

			binaryReader.ExpectUInt32 (0);

			ushort numStrings = binaryReader.ReadUInt16 ();
			ushort numBlobs = binaryReader.ReadUInt16 ();
			uint totalDataLength = binaryReader.ReadUInt32 ();

			List<ByteSpan> dataItems = new List<ByteSpan> (numStrings + numBlobs);

			long dataStartOffset =
				binaryReader.BaseStream.Position +
				numStrings * 2 * sizeof (ushort) +
				numBlobs * 2 * sizeof (uint);

			for (int i = 0; i < numStrings; i++)
			{
				ushort stringOffset = binaryReader.ReadUInt16 ();
				ushort stringLength = binaryReader.ReadUInt16 ();
				dataItems.Add (new ByteSpan (sectionPosition + dataStartOffset + stringOffset, stringLength));
			}

			for (int i = 0; i < numBlobs; i++)
			{
				uint blobOffset = binaryReader.ReadUInt32 ();
				uint blobLength = binaryReader.ReadUInt32 ();
				dataItems.Add (new ByteSpan (sectionPosition + dataStartOffset + blobOffset, blobLength));
			}

			DataItems = new ReadOnlyCollection<ByteSpan> (dataItems);

			return true;
		}
		public override void Dispose ()
		{
			DataItems?.Clear ();
			DataItems = null;
			base.Dispose ();
		}
	}

	public class DataItemRef
	{
		internal SectionRef<DataItemSection> dataItemSection;
		internal int itemIndex;
		public SectionRef<DataItemSection> DataItemSection => dataItemSection;
		public int ItemIndex => itemIndex;
		internal DataItemRef (SectionRef<DataItemSection> dataItemSection, int itemIndex)
		{
			this.dataItemSection = dataItemSection;
			this.itemIndex = itemIndex;
		}

		public override string ToString ()
		{
			return string.Format ("Data item {0} in section {1}", itemIndex, dataItemSection.SectionIndex);
		}
	}
}

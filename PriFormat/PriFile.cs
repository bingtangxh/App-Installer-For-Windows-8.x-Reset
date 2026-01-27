using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PriFormat
{
	public class PriFile: IDisposable
	{
		public string Version { get; private set; }
		public uint TotalFileSize { get; private set; }
		public IList<TocEntry> TableOfContents { get; private set; }
		public IList<Section> Sections { get; private set; }
		private PriFile ()
		{
		}
		public static PriFile Parse (Stream stream)
		{
			PriFile priFile = new PriFile ();
			priFile.ParseInternal (stream);
			return priFile;
		}

		private void ParseInternal (Stream stream)
		{
			BinaryReader binaryReader = new BinaryReader (stream, Encoding.ASCII);
			long fileStartOffset = binaryReader.BaseStream.Position;
			string magic = new string (binaryReader.ReadChars (8));
			switch (magic)
			{
				case "mrm_pri0":
				case "mrm_pri1":
				case "mrm_pri2":
				case "mrm_pri3":
				case "mrm_prif":
					Version = magic;
					break;
				default:
					throw new InvalidDataException ("Data does not start with a PRI file header.");
			}
			binaryReader.ExpectUInt16 (0);
			binaryReader.ExpectUInt16 (1);
			TotalFileSize = binaryReader.ReadUInt32 ();
			uint tocOffset = binaryReader.ReadUInt32 ();
			uint sectionStartOffset = binaryReader.ReadUInt32 ();
			ushort numSections = binaryReader.ReadUInt16 ();
			binaryReader.ExpectUInt16 (0xFFFF);
			binaryReader.ExpectUInt32 (0);
			binaryReader.BaseStream.Seek (fileStartOffset + TotalFileSize - 16, SeekOrigin.Begin);
			binaryReader.ExpectUInt32 (0xDEFFFADE);
			binaryReader.ExpectUInt32 (TotalFileSize);
			binaryReader.ExpectString (magic);
			binaryReader.BaseStream.Seek (tocOffset, SeekOrigin.Begin);
			List<TocEntry> toc = new List<TocEntry> (numSections);
			for (int i = 0; i < numSections; i++)
			{
				toc.Add (TocEntry.Parse (binaryReader));
			}
			TableOfContents = toc;
			Section [] sections = new Section [numSections];
			Sections = sections;
			bool parseSuccess;
			bool parseFailure;
			do
			{
				parseSuccess = false;
				parseFailure = false;
				for (int i = 0; i < sections.Length; i++)
				{
					if (sections [i] != null) continue;
					binaryReader.BaseStream.Seek (
						sectionStartOffset + toc [i].SectionOffset,
						SeekOrigin.Begin);
					Section section = Section.CreateForIdentifier (
						toc [i].SectionIdentifier,
						this);
					if (section.Parse (binaryReader))
					{
						sections [i] = section;
						parseSuccess = true;
					}
					else
					{
						parseFailure = true;
					}
				}
			}
			while (parseFailure && parseSuccess);
			if (parseFailure) throw new InvalidDataException ("Failed to parse all sections.");
		}

		private PriDescriptorSection _priDescriptorSection;
		public PriDescriptorSection PriDescriptorSection
		{
			get
			{
				if (_priDescriptorSection == null)
				{
					_priDescriptorSection =
						Sections.OfType<PriDescriptorSection> ().Single ();
				}
				return _priDescriptorSection;
			}
		}

		public T GetSectionByRef<T> (SectionRef<T> sectionRef)
			where T : Section
		{
			return (T)Sections [sectionRef.SectionIndex];
		}
		public ResourceMapItem GetResourceMapItemByRef (
			ResourceMapItemRef resourceMapItemRef)
		{
			HierarchicalSchemaSection schema =
				GetSectionByRef (resourceMapItemRef.SchemaSection);

			return schema.Items [resourceMapItemRef.ItemIndex];
		}
		public ByteSpan GetDataItemByRef (DataItemRef dataItemRef)
		{
			DataItemSection section =
				GetSectionByRef (dataItemRef.DataItemSection);

			return section.DataItems [dataItemRef.ItemIndex];
		}
		public ReferencedFile GetReferencedFileByRef (
			ReferencedFileRef referencedFileRef)
		{
			SectionRef<ReferencedFileSection> refSection =
				PriDescriptorSection.ReferencedFileSections.First ();
			ReferencedFileSection section = GetSectionByRef (refSection);
			return section.ReferencedFiles [referencedFileRef.FileIndex];
		}

		public void Dispose ()
		{
			TableOfContents?.Clear ();
			TableOfContents = null;
			//Sections?.Clear ();
			Sections = null;
		}
	}
}

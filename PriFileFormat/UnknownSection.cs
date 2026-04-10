using System.IO;
using System.Runtime.InteropServices;
namespace PriFileFormat
{
	public class UnknownSection: Section
	{
		public byte [] SectionContent { get; private set; }
		internal UnknownSection (string sectionIdentifier, PriFile priFile) : base (sectionIdentifier, priFile) {}
		protected override bool ParseSectionContent (BinaryReader binaryReader)
		{
			int contentLength = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);

			SectionContent = binaryReader.ReadBytes (contentLength);

			return true;
		}
		public void ClearContent ()
		{
			SectionContent = null;
		}
		~UnknownSection () { ClearContent (); }
	}
}

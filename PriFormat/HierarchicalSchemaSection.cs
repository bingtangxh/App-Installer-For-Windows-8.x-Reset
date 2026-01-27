using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PriFormat
{
	public class HierarchicalSchemaSection: Section
	{
		public HierarchicalSchemaVersionInfo Version { get; private set; }
		public string UniqueName { get; private set; }
		public string Name { get; private set; }
		public IList<ResourceMapScope> Scopes { get; private set; }
		public IList<ResourceMapItem> Items { get; private set; }

		readonly bool extendedVersion;

		internal const string Identifier1 = "[mrm_hschema]  \0";
		internal const string Identifier2 = "[mrm_hschemaex] ";

		internal HierarchicalSchemaSection (PriFile priFile, bool extendedVersion)
			: base (extendedVersion ? Identifier2 : Identifier1, priFile)
		{
			this.extendedVersion = extendedVersion;
		}

		protected override bool ParseSectionContent (BinaryReader binaryReader)
		{
			if (binaryReader.BaseStream.Length == 0)
			{
				Version = null;
				UniqueName = null;
				Name = null;
				Scopes = new List<ResourceMapScope> ();
				Items = new List<ResourceMapItem> ();
				return true;
			}

			binaryReader.ExpectUInt16 (1);
			ushort uniqueNameLength = binaryReader.ReadUInt16 ();
			ushort nameLength = binaryReader.ReadUInt16 ();
			binaryReader.ExpectUInt16 (0);

			bool extendedHNames;
			if (extendedVersion)
			{
				string def = new string (binaryReader.ReadChars (16));
				switch (def)
				{
					case "[def_hnamesx]  \0":
						extendedHNames = true;
						break;
					case "[def_hnames]   \0":
						extendedHNames = false;
						break;
					default:
						throw new InvalidDataException ();
				}
			}
			else
			{
				extendedHNames = false;
			}

			// hierarchical schema version info
			ushort majorVersion = binaryReader.ReadUInt16 ();
			ushort minorVersion = binaryReader.ReadUInt16 ();
			binaryReader.ExpectUInt32 (0);
			uint checksum = binaryReader.ReadUInt32 ();
			uint numScopes = binaryReader.ReadUInt32 ();
			uint numItems = binaryReader.ReadUInt32 ();

			Version = new HierarchicalSchemaVersionInfo (majorVersion, minorVersion, checksum, numScopes, numItems);

			UniqueName = binaryReader.ReadNullTerminatedString (Encoding.Unicode);
			Name = binaryReader.ReadNullTerminatedString (Encoding.Unicode);

			if (UniqueName.Length != uniqueNameLength - 1 || Name.Length != nameLength - 1)
				throw new InvalidDataException ();

			binaryReader.ExpectUInt16 (0);
			ushort maxFullPathLength = binaryReader.ReadUInt16 ();
			binaryReader.ExpectUInt16 (0);
			binaryReader.ExpectUInt32 (numScopes + numItems);
			binaryReader.ExpectUInt32 (numScopes);
			binaryReader.ExpectUInt32 (numItems);
			uint unicodeDataLength = binaryReader.ReadUInt32 ();
			binaryReader.ReadUInt32 (); // meaning unknown

			if (extendedHNames)
				binaryReader.ReadUInt32 (); // meaning unknown

			List<ScopeAndItemInfo> scopeAndItemInfos = new List<ScopeAndItemInfo> ((int)(numScopes + numItems));

			for (int i = 0; i < numScopes + numItems; i++)
			{
				ushort parent = binaryReader.ReadUInt16 ();
				ushort fullPathLength = binaryReader.ReadUInt16 ();
				char uppercaseFirstChar = (char)binaryReader.ReadUInt16 ();
				byte nameLength2 = binaryReader.ReadByte ();
				byte flags = binaryReader.ReadByte ();
				uint nameOffset = binaryReader.ReadUInt16 () | (uint)((flags & 0xF) << 16);
				ushort index = binaryReader.ReadUInt16 ();
				scopeAndItemInfos.Add (new ScopeAndItemInfo (parent, fullPathLength, flags, nameOffset, index));
			}

			List<ScopeExInfo> scopeExInfos = new List<ScopeExInfo> ((int)numScopes);

			for (int i = 0; i < numScopes; i++)
			{
				ushort scopeIndex = binaryReader.ReadUInt16 ();
				ushort childCount = binaryReader.ReadUInt16 ();
				ushort firstChildIndex = binaryReader.ReadUInt16 ();
				binaryReader.ExpectUInt16 (0);
				scopeExInfos.Add (new ScopeExInfo (scopeIndex, childCount, firstChildIndex));
			}

			ushort [] itemIndexPropertyToIndex = new ushort [numItems];
			for (int i = 0; i < numItems; i++)
				itemIndexPropertyToIndex [i] = binaryReader.ReadUInt16 ();

			long unicodeDataOffset = binaryReader.BaseStream.Position;
			long asciiDataOffset = binaryReader.BaseStream.Position + unicodeDataLength * 2;

			ResourceMapScope [] scopes = new ResourceMapScope [numScopes];
			ResourceMapItem [] items = new ResourceMapItem [numItems];

			for (int i = 0; i < numScopes + numItems; i++)
			{
				long pos;

				if (scopeAndItemInfos [i].NameInAscii)
					pos = asciiDataOffset + scopeAndItemInfos [i].NameOffset;
				else
					pos = unicodeDataOffset + scopeAndItemInfos [i].NameOffset * 2;

				binaryReader.BaseStream.Seek (pos, SeekOrigin.Begin);

				string name;

				if (scopeAndItemInfos [i].FullPathLength != 0)
					name = binaryReader.ReadNullTerminatedString (scopeAndItemInfos [i].NameInAscii ? Encoding.ASCII : Encoding.Unicode);
				else
					name = string.Empty;

				ushort index = scopeAndItemInfos [i].Index;

				if (scopeAndItemInfos [i].IsScope)
				{
					if (scopes [index] != null)
						throw new InvalidDataException ();

					scopes [index] = new ResourceMapScope (index, null, name);
				}
				else
				{
					if (items [index] != null)
						throw new InvalidDataException ();

					items [index] = new ResourceMapItem (index, null, name);
				}
			}

			for (int i = 0; i < numScopes + numItems; i++)
			{
				ushort index = scopeAndItemInfos [i].Index;
				ushort parent = scopeAndItemInfos [scopeAndItemInfos [i].Parent].Index;

				if (parent != 0xFFFF)
				{
					if (scopeAndItemInfos [i].IsScope)
					{
						if (parent != index)
							scopes [index].Parent = scopes [parent];
					}
					else
						items [index].Parent = scopes [parent];
				}
			}

			for (int i = 0; i < numScopes; i++)
			{
				List<ResourceMapEntry> children = new List<ResourceMapEntry> (scopeExInfos [i].ChildCount);

				for (int j = 0; j < scopeExInfos [i].ChildCount; j++)
				{
					ScopeAndItemInfo saiInfo = scopeAndItemInfos [scopeExInfos [i].FirstChildIndex + j];

					if (saiInfo.IsScope)
						children.Add (scopes [saiInfo.Index]);
					else
						children.Add (items [saiInfo.Index]);
				}

				scopes [i].Children = children;
			}

			Scopes = scopes;
			Items = items;

			return true;
		}

		private struct ScopeAndItemInfo
		{
			public ushort Parent;
			public ushort FullPathLength;
			public byte Flags;
			public uint NameOffset;
			public ushort Index;

			public ScopeAndItemInfo (ushort parent, ushort fullPathLength, byte flags, uint nameOffset, ushort index)
			{
				Parent = parent;
				FullPathLength = fullPathLength;
				Flags = flags;
				NameOffset = nameOffset;
				Index = index;
			}

			public bool IsScope
			{
				get { return (Flags & 0x10) != 0; }
			}

			public bool NameInAscii
			{
				get { return (Flags & 0x20) != 0; }
			}
		}

		private struct ScopeExInfo
		{
			public ushort ScopeIndex;
			public ushort ChildCount;
			public ushort FirstChildIndex;

			public ScopeExInfo (ushort scopeIndex, ushort childCount, ushort firstChildIndex)
			{
				ScopeIndex = scopeIndex;
				ChildCount = childCount;
				FirstChildIndex = firstChildIndex;
			}
		}
		public override void Dispose ()
		{
			this.Version = null;
			Scopes?.Clear ();
			Scopes = null;
			Items?.Clear ();
			Items = null;
			base.Dispose ();
		}
	}

	public class HierarchicalSchemaVersionInfo
	{
		public ushort MajorVersion { get; private set; }
		public ushort MinorVersion { get; private set; }
		public uint Checksum { get; private set; }
		public uint NumScopes { get; private set; }
		public uint NumItems { get; private set; }

		public HierarchicalSchemaVersionInfo (ushort major, ushort minor, uint checksum, uint numScopes, uint numItems)
		{
			MajorVersion = major;
			MinorVersion = minor;
			Checksum = checksum;
			NumScopes = numScopes;
			NumItems = numItems;
		}
	}

	public abstract class ResourceMapEntry: IDisposable
	{
		public ushort Index { get; private set; }
		public ResourceMapScope Parent { get; internal set; }
		public string Name { get; private set; }

		internal ResourceMapEntry (ushort index, ResourceMapScope parent, string name)
		{
			Index = index;
			Parent = parent;
			Name = name;
		}

		string fullName;

		public string FullName
		{
			get
			{
				if (fullName == null)
				{
					if (Parent == null)
						fullName = Name;
					else
						fullName = Parent.FullName + "\\" + Name;
				}
				return fullName;
			}
		}
		~ResourceMapEntry ()
		{
			Dispose ();
		}
		public virtual void Dispose ()
		{
			Parent = null;
		}
	}

	public sealed class ResourceMapScope: ResourceMapEntry
	{
		internal ResourceMapScope (ushort index, ResourceMapScope parent, string name)
			: base (index, parent, name)
		{
		}

		public IList<ResourceMapEntry> Children { get; internal set; }

		public override string ToString ()
		{
			return string.Format ("Scope {0} {1} ({2} children)", Index, FullName, Children.Count);
		}
		public override void Dispose ()
		{
			Children?.Clear ();
			Children = null;
			base.Dispose ();
		}
		~ResourceMapScope () { Dispose (); }
	}

	public sealed class ResourceMapItem: ResourceMapEntry
	{
		internal ResourceMapItem (ushort index, ResourceMapScope parent, string name)
			: base (index, parent, name)
		{
		}

		public override string ToString ()
		{
			return string.Format ("Item {0} {1}", Index, FullName);
		}
	}
}

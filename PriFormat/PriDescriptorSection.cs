using System;
using System.Collections.Generic;
using System.IO;

namespace PriFormat
{
	public class PriDescriptorSection: Section
	{
		public PriDescriptorFlags PriFlags { get; private set; }

		public IList<SectionRef<HierarchicalSchemaSection>> HierarchicalSchemaSections { get; private set; }
		public IList<SectionRef<DecisionInfoSection>> DecisionInfoSections { get; private set; }
		public IList<SectionRef<ResourceMapSection>> ResourceMapSections { get; private set; }
		public IList<SectionRef<ReferencedFileSection>> ReferencedFileSections { get; private set; }
		public IList<SectionRef<DataItemSection>> DataItemSections { get; private set; }

		public SectionRef<ResourceMapSection> PrimaryResourceMapSection { get; private set; }
		public bool HasPrimaryResourceMapSection { get; private set; }

		internal const string Identifier = "[mrm_pridescex]\0";

		internal PriDescriptorSection (PriFile priFile)
			: base (Identifier, priFile)
		{
		}

		protected override bool ParseSectionContent (BinaryReader binaryReader)
		{
			PriFlags = (PriDescriptorFlags)binaryReader.ReadUInt16 ();
			ushort includedFileListSection = binaryReader.ReadUInt16 ();
			binaryReader.ExpectUInt16 (0);

			ushort numHierarchicalSchemaSections = binaryReader.ReadUInt16 ();
			ushort numDecisionInfoSections = binaryReader.ReadUInt16 ();
			ushort numResourceMapSections = binaryReader.ReadUInt16 ();

			ushort primaryResourceMapSection = binaryReader.ReadUInt16 ();
			if (primaryResourceMapSection != 0xFFFF)
			{
				PrimaryResourceMapSection =
					new SectionRef<ResourceMapSection> (primaryResourceMapSection);
				HasPrimaryResourceMapSection = true;
			}
			else
			{
				HasPrimaryResourceMapSection = false;
			}

			ushort numReferencedFileSections = binaryReader.ReadUInt16 ();
			ushort numDataItemSections = binaryReader.ReadUInt16 ();

			binaryReader.ExpectUInt16 (0);

			// Hierarchical schema sections
			List<SectionRef<HierarchicalSchemaSection>> hierarchicalSchemaSections =
				new List<SectionRef<HierarchicalSchemaSection>> (numHierarchicalSchemaSections);

			for (int i = 0; i < numHierarchicalSchemaSections; i++)
			{
				hierarchicalSchemaSections.Add (
					new SectionRef<HierarchicalSchemaSection> (binaryReader.ReadUInt16 ()));
			}

			HierarchicalSchemaSections = hierarchicalSchemaSections;

			// Decision info sections
			List<SectionRef<DecisionInfoSection>> decisionInfoSections =
				new List<SectionRef<DecisionInfoSection>> (numDecisionInfoSections);

			for (int i = 0; i < numDecisionInfoSections; i++)
			{
				decisionInfoSections.Add (
					new SectionRef<DecisionInfoSection> (binaryReader.ReadUInt16 ()));
			}

			DecisionInfoSections = decisionInfoSections;

			// Resource map sections
			List<SectionRef<ResourceMapSection>> resourceMapSections =
				new List<SectionRef<ResourceMapSection>> (numResourceMapSections);

			for (int i = 0; i < numResourceMapSections; i++)
			{
				resourceMapSections.Add (
					new SectionRef<ResourceMapSection> (binaryReader.ReadUInt16 ()));
			}

			ResourceMapSections = resourceMapSections;

			// Referenced file sections
			List<SectionRef<ReferencedFileSection>> referencedFileSections =
				new List<SectionRef<ReferencedFileSection>> (numReferencedFileSections);

			for (int i = 0; i < numReferencedFileSections; i++)
			{
				referencedFileSections.Add (
					new SectionRef<ReferencedFileSection> (binaryReader.ReadUInt16 ()));
			}

			ReferencedFileSections = referencedFileSections;

			// Data item sections
			List<SectionRef<DataItemSection>> dataItemSections =
				new List<SectionRef<DataItemSection>> (numDataItemSections);

			for (int i = 0; i < numDataItemSections; i++)
			{
				dataItemSections.Add (
					new SectionRef<DataItemSection> (binaryReader.ReadUInt16 ()));
			}

			DataItemSections = dataItemSections;

			return true;
		}
		public override void Dispose ()
		{
			this.HierarchicalSchemaSections?.Clear ();
			this.DecisionInfoSections?.Clear ();
			this.ResourceMapSections?.Clear ();
			this.ReferencedFileSections?.Clear ();
			this.DataItemSections?.Clear ();
			HierarchicalSchemaSections = null;
			DecisionInfoSections = null;
			ResourceMapSections = null;
			ReferencedFileSections = null;
			DataItemSections = null;
			base.Dispose ();
		}
	}
	[Flags]
	public enum PriDescriptorFlags: ushort
	{
		AutoMerge = 1,
		IsDeploymentMergeable = 2,
		IsDeploymentMergeResult = 4,
		IsAutomergeMergeResult = 8
	}
}

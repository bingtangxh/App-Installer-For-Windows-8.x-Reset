using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PriFormat
{
	public class DecisionInfoSection: Section
	{
		public IList<Decision> Decisions { get; private set; }
		public IList<QualifierSet> QualifierSets { get; private set; }
		public IList<Qualifier> Qualifiers { get; private set; }

		internal const string Identifier = "[mrm_decn_info]\0";

		internal DecisionInfoSection (PriFile priFile)
			: base (Identifier, priFile)
		{
		}

		protected override bool ParseSectionContent (BinaryReader binaryReader)
		{
			ushort numDistinctQualifiers = binaryReader.ReadUInt16 ();
			ushort numQualifiers = binaryReader.ReadUInt16 ();
			ushort numQualifierSets = binaryReader.ReadUInt16 ();
			ushort numDecisions = binaryReader.ReadUInt16 ();
			ushort numIndexTableEntries = binaryReader.ReadUInt16 ();
			ushort totalDataLength = binaryReader.ReadUInt16 ();

			List<DecisionInfo> decisionInfos = new List<DecisionInfo> (numDecisions);
			for (int i = 0; i < numDecisions; i++)
			{
				ushort firstQualifierSetIndexIndex = binaryReader.ReadUInt16 ();
				ushort numQualifierSetsInDecision = binaryReader.ReadUInt16 ();
				decisionInfos.Add (new DecisionInfo (firstQualifierSetIndexIndex, numQualifierSetsInDecision));
			}

			List<QualifierSetInfo> qualifierSetInfos = new List<QualifierSetInfo> (numQualifierSets);
			for (int i = 0; i < numQualifierSets; i++)
			{
				ushort firstQualifierIndexIndex = binaryReader.ReadUInt16 ();
				ushort numQualifiersInSet = binaryReader.ReadUInt16 ();
				qualifierSetInfos.Add (new QualifierSetInfo (firstQualifierIndexIndex, numQualifiersInSet));
			}

			List<QualifierInfo> qualifierInfos = new List<QualifierInfo> (numQualifiers);
			for (int i = 0; i < numQualifiers; i++)
			{
				ushort index = binaryReader.ReadUInt16 ();
				ushort priority = binaryReader.ReadUInt16 ();
				ushort fallbackScore = binaryReader.ReadUInt16 ();
				binaryReader.ExpectUInt16 (0);
				qualifierInfos.Add (new QualifierInfo (index, priority, fallbackScore));
			}

			List<DistinctQualifierInfo> distinctQualifierInfos = new List<DistinctQualifierInfo> (numDistinctQualifiers);
			for (int i = 0; i < numDistinctQualifiers; i++)
			{
				binaryReader.ReadUInt16 ();
				QualifierType qualifierType = (QualifierType)binaryReader.ReadUInt16 ();
				binaryReader.ReadUInt16 ();
				binaryReader.ReadUInt16 ();
				uint operandValueOffset = binaryReader.ReadUInt32 ();
				distinctQualifierInfos.Add (new DistinctQualifierInfo (qualifierType, operandValueOffset));
			}

			ushort [] indexTable = new ushort [numIndexTableEntries];
			for (int i = 0; i < numIndexTableEntries; i++)
				indexTable [i] = binaryReader.ReadUInt16 ();

			long dataStartOffset = binaryReader.BaseStream.Position;

			List<Qualifier> qualifiers = new List<Qualifier> (numQualifiers);

			for (int i = 0; i < numQualifiers; i++)
			{
				DistinctQualifierInfo distinctQualifierInfo = distinctQualifierInfos [qualifierInfos [i].Index];

				binaryReader.BaseStream.Seek (dataStartOffset + distinctQualifierInfo.OperandValueOffset * 2, SeekOrigin.Begin);

				string value = binaryReader.ReadNullTerminatedString (Encoding.Unicode);

				qualifiers.Add (new Qualifier (
					(ushort)i,
					distinctQualifierInfo.QualifierType,
					qualifierInfos [i].Priority,
					qualifierInfos [i].FallbackScore / 1000f,
					value));
			}

			Qualifiers = qualifiers;

			List<QualifierSet> qualifierSets = new List<QualifierSet> (numQualifierSets);

			for (int i = 0; i < numQualifierSets; i++)
			{
				List<Qualifier> qualifiersInSet = new List<Qualifier> (qualifierSetInfos [i].NumQualifiersInSet);

				for (int j = 0; j < qualifierSetInfos [i].NumQualifiersInSet; j++)
					qualifiersInSet.Add (qualifiers [indexTable [qualifierSetInfos [i].FirstQualifierIndexIndex + j]]);

				qualifierSets.Add (new QualifierSet ((ushort)i, qualifiersInSet));
			}

			QualifierSets = qualifierSets;

			List<Decision> decisions = new List<Decision> (numDecisions);

			for (int i = 0; i < numDecisions; i++)
			{
				List<QualifierSet> qualifierSetsInDecision = new List<QualifierSet> (decisionInfos [i].NumQualifierSetsInDecision);

				for (int j = 0; j < decisionInfos [i].NumQualifierSetsInDecision; j++)
					qualifierSetsInDecision.Add (qualifierSets [indexTable [decisionInfos [i].FirstQualifierSetIndexIndex + j]]);

				decisions.Add (new Decision ((ushort)i, qualifierSetsInDecision));
			}

			Decisions = decisions;

			return true;
		}

		private struct DecisionInfo
		{
			public ushort FirstQualifierSetIndexIndex;
			public ushort NumQualifierSetsInDecision;

			public DecisionInfo (ushort first, ushort num)
			{
				FirstQualifierSetIndexIndex = first;
				NumQualifierSetsInDecision = num;
			}
		}

		private struct QualifierSetInfo
		{
			public ushort FirstQualifierIndexIndex;
			public ushort NumQualifiersInSet;

			public QualifierSetInfo (ushort first, ushort num)
			{
				FirstQualifierIndexIndex = first;
				NumQualifiersInSet = num;
			}
		}

		private struct QualifierInfo
		{
			public ushort Index;
			public ushort Priority;
			public ushort FallbackScore;

			public QualifierInfo (ushort index, ushort priority, ushort fallbackScore)
			{
				Index = index;
				Priority = priority;
				FallbackScore = fallbackScore;
			}
		}

		private struct DistinctQualifierInfo
		{
			public QualifierType QualifierType;
			public uint OperandValueOffset;

			public DistinctQualifierInfo (QualifierType type, uint offset)
			{
				QualifierType = type;
				OperandValueOffset = offset;
			}
		}
		public override void Dispose ()
		{
			Decisions?.Clear ();
			Decisions = null;
			QualifierSets?.Clear ();
			QualifierSets = null;
			Qualifiers?.Clear ();
			Qualifiers = null;
			base.Dispose ();
		}
		~DecisionInfoSection () { Dispose (); }
	}

	public enum QualifierType
	{
		Language,
		Contrast,
		Scale,
		HomeRegion,
		TargetSize,
		LayoutDirection,
		Theme,
		AlternateForm,
		DXFeatureLevel,
		Configuration,
		DeviceFamily,
		Custom
	}

	public struct Qualifier
	{
		public ushort Index;
		public QualifierType Type;
		public ushort Priority;
		public float FallbackScore;
		public string Value;

		public Qualifier (ushort index, QualifierType type, ushort priority, float fallbackScore, string value)
		{
			Index = index;
			Type = type;
			Priority = priority;
			FallbackScore = fallbackScore;
			Value = value;
		}
	}

	public struct QualifierSet
	{
		public ushort Index;
		public IList<Qualifier> Qualifiers;

		public QualifierSet (ushort index, IList<Qualifier> qualifiers)
		{
			Index = index;
			Qualifiers = qualifiers;
		}
	}

	public struct Decision
	{
		public ushort Index;
		public IList<QualifierSet> QualifierSets;

		public Decision (ushort index, IList<QualifierSet> qualifierSets)
		{
			Index = index;
			QualifierSets = qualifierSets;
		}
	}
}

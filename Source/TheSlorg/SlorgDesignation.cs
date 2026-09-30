using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// A drone's hive designation ("Three of Nine"). Remembers who the drone was before assimilation, so a freed
    /// drone can get its name back.
    /// </summary>
    public class Hediff_SlorgDesignation : HediffWithComps
    {
        public Name originalName;
        public string ordinal;

        public override string LabelInBrackets => originalName != null ? "formerly " + originalName.ToStringFull : null;

        public override bool ShouldRemove => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref originalName, "originalName");
            Scribe_Values.Look(ref ordinal, "ordinal");
        }
    }

    public static class SlorgDesignation
    {
        private static readonly string[] Numbers =
        {
            "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"
        };

        public static Hediff_SlorgDesignation Of(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_Designation) as Hediff_SlorgDesignation;
        }

        /// <summary>
        /// Replaces the pawn's name with a hive designation. Assimilated people keep their old name on file; drones born
        /// to the hive have none.
        /// </summary>
        public static void Designate(Pawn pawn, bool rememberName)
        {
            if (pawn?.health == null || pawn.Dead || Of(pawn) != null || SlorgUtility.IsQueen(pawn))
            {
                return;
            }
            int group = Rand.RangeInclusive(3, Numbers.Length);
            string ordinal = Numbers[Rand.Range(0, group)];
            string designation = $"{ordinal} of {Numbers[group - 1]}";
            string unimatrix = $"of Unimatrix {Rand.RangeInclusive(1, 99):00}";

            Hediff_SlorgDesignation hediff = (Hediff_SlorgDesignation)HediffMaker.MakeHediff(SlorgDefOf.Slorg_Designation, pawn);
            hediff.originalName = rememberName ? pawn.Name : null;
            hediff.ordinal = ordinal;
            pawn.health.AddHediff(hediff);
            pawn.Name = new NameTriple(designation, designation, unimatrix);
        }

        /// <summary>
        /// A freed drone becomes a person again. The assimilated get their old name back. A drone born to the hive gets a
        /// new name and keeps its old number as a nickname.
        /// </summary>
        public static void Restore(Pawn pawn)
        {
            Hediff_SlorgDesignation hediff = Of(pawn);
            if (hediff == null)
            {
                return;
            }
            if (hediff.originalName != null)
            {
                pawn.Name = hediff.originalName;
            }
            else if (PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full) is NameTriple fresh)
            {
                pawn.Name = new NameTriple(fresh.First, hediff.ordinal ?? fresh.Nick, fresh.Last);
            }
            pawn.health.RemoveHediff(hediff);
        }
    }
}

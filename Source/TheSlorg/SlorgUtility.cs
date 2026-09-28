using Verse;

namespace TheSlorg
{
    public static class SlorgUtility
    {
        /// <summary>A drone is a living humanlike with an active collective link gene who belongs to a faction.</summary>
        public static bool IsDrone(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.Faction != null
                && pawn.genes != null
                && pawn.genes.HasActiveGene(SlorgDefOf.Slorg_CollectiveLink);
        }

        /// <summary>Prisoners and slaves are cut off from the collective until freed.</summary>
        public static bool IsLinkedDrone(Pawn pawn)
        {
            return IsDrone(pawn) && !pawn.IsPrisoner && !pawn.IsSlave;
        }
    }
}

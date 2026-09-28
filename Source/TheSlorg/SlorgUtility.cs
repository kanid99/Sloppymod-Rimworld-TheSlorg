using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheSlorg
{
    public static class SlorgUtility
    {
        public static bool IsSlorgFaction(Faction faction)
        {
            return faction != null && faction.def == SlorgDefOf.Slorg_CollectiveFaction;
        }

        /// <summary>Carries the collective link gene (drones and queens, linked or not).</summary>
        public static bool HasLinkGene(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(SlorgDefOf.Slorg_CollectiveLink);
        }

        public static bool IsQueen(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(SlorgDefOf.Slorg_HiveSovereign);
        }

        /// <summary>Any Slorg pawn at all, including disconnected drones.</summary>
        public static bool IsSlorg(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return false;
            }
            foreach (Gene gene in pawn.genes.GenesListForReading)
            {
                if (gene.def.defName.StartsWith("Slorg_"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A living pawn with the link gene, in a non-player faction.</summary>
        public static bool IsDrone(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.Faction != null
                && !pawn.Faction.IsPlayer
                && HasLinkGene(pawn);
        }

        /// <summary>A drone currently able to hear the collective: not captured, enslaved or severed.</summary>
        public static bool IsLinkedDrone(Pawn pawn)
        {
            return IsDrone(pawn)
                && !pawn.IsPrisoner
                && !pawn.IsSlave
                && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_Severance);
        }

        public static bool IsSpace(PlanetTile tile)
        {
            return tile.Valid && tile.LayerDef != null && tile.LayerDef.isSpace;
        }

        /// <summary>Turns any Slorg into a disconnected drone: implants stay, the link and Assimilate go.</summary>
        public static void MakeDisconnected(Pawn pawn)
        {
            if (pawn?.genes == null || pawn.genes.Xenotype == SlorgDefOf.Slorg_DisconnectedDrone)
            {
                return;
            }
            pawn.genes.SetXenotype(SlorgDefOf.Slorg_DisconnectedDrone);
            if (pawn.guest != null)
            {
                // Without the collective's voice in their head, a freed drone is open to persuasion.
                pawn.guest.resistance = UnityEngine.Mathf.Min(pawn.guest.resistance, 8f);
                pawn.guest.will = UnityEngine.Mathf.Min(pawn.guest.will, 2f);
            }
            Hediff link = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff);
            if (link != null)
            {
                pawn.health.RemoveHediff(link);
            }
        }

        public static void AddSeverance(Pawn pawn)
        {
            if (!pawn.Dead && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_Severance))
            {
                pawn.health.AddHediff(SlorgDefOf.Slorg_Severance);
            }
        }
    }
}

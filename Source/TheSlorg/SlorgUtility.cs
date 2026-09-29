using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

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

        /// <summary>
        /// Turns any Slorg into a disconnected drone. Full drones keep their implants; thralls, who never had any,
        /// lose their Slorg genes and keep only the severed-link marker.
        /// </summary>
        public static void MakeDisconnected(Pawn pawn)
        {
            if (pawn?.genes == null || pawn.genes.Xenotype == SlorgDefOf.Slorg_DisconnectedDrone)
            {
                return;
            }
            if (pawn.genes.Xenotype == SlorgDefOf.Slorg_Thrall)
            {
                foreach (Gene gene in pawn.genes.Xenogenes.ToList())
                {
                    if (gene.def.defName.StartsWith("Slorg_"))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
                pawn.genes.AddGene(SlorgDefOf.Slorg_Severed, xenogene: true);
                pawn.genes.SetXenotypeDirect(SlorgDefOf.Slorg_DisconnectedDrone);
            }
            else
            {
                pawn.genes.SetXenotype(SlorgDefOf.Slorg_DisconnectedDrone);
            }
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

        /// <summary>
        /// Half-assimilated: the collective's genes without the cybernetics. Keeps the pawn's own body and looks.
        /// </summary>
        public static void MakeThrall(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return;
            }
            pawn.genes.SetXenotype(SlorgDefOf.Slorg_Thrall);
        }

        /// <summary>The Slorg faction to hand a new thrall to, or null if the collective is gone from this world.</summary>
        public static Faction ResolveSlorgFaction(Faction preferred, PlanetTile tile)
        {
            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            bool Usable(Faction f) => f != null && !f.defeated && IsSlorgFaction(f)
                && !(collective != null && collective.SurfaceControlLost(f) && !IsSpace(tile));
            if (Usable(preferred))
            {
                return preferred;
            }
            Faction any = Find.FactionManager.FirstFactionOfDef(SlorgDefOf.Slorg_CollectiveFaction);
            return Usable(any) ? any : null;
        }

        /// <summary>New thralls on a colony map turn on it instead of leaving.</summary>
        public static void TurnOnColony(List<Pawn> pawns, Faction faction, Map map)
        {
            foreach (Pawn pawn in pawns)
            {
                if (pawn.Faction != faction)
                {
                    pawn.SetFaction(faction);
                }
                pawn.GetLord()?.RemovePawn(pawn);
            }
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false,
                    useAvoidGridSmart: false, canSteal: false, breachers: false, canPickUpOpportunisticWeapons: true),
                map, pawns);
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

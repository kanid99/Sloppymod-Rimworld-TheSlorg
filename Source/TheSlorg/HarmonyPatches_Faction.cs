using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>Once their queen core is gone, the Slorg can't raid the planet surface. Space maps are still fair game.</summary>
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "FactionCanBeGroupSource")]
    public static class IncidentWorker_RaidEnemy_FactionCanBeGroupSource_Patch
    {
        public static void Postfix(Faction f, IncidentParms parms, ref bool __result)
        {
            if (!__result || !SlorgUtility.IsSlorgFaction(f))
            {
                return;
            }
            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            if (collective == null || !collective.SurfaceControlLost(f))
            {
                return;
            }
            if (!(parms.target is Map map) || !SlorgUtility.IsSpace(map.Tile))
            {
                __result = false;
            }
        }
    }

    /// <summary>Very large Slorg raids may bring the queen herself.</summary>
    [HarmonyPatch(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns))]
    public static class PawnGroupMakerUtility_GeneratePawns_Patch
    {
        public static void Postfix(PawnGroupMakerParms parms, ref IEnumerable<Pawn> __result)
        {
            Faction faction = parms?.faction;
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            if (!SlorgUtility.IsSlorgFaction(faction)
                || parms.groupKind != PawnGroupKindDefOf.Combat
                || parms.points < tuning.queenRaidMinPoints
                || !Rand.Chance(tuning.queenRaidChance))
            {
                return;
            }

            Pawn queen = GameComponent_SlorgCollective.Instance?.QueenOf(faction) ?? faction.leader;
            if (queen == null || queen.Dead || queen.Spawned || queen.Faction != faction || queen.IsPrisoner
                || !SlorgUtility.IsQueen(queen) || !Find.WorldPawns.Contains(queen))
            {
                return;
            }

            List<Pawn> pawns = __result.ToList();
            Find.WorldPawns.RemovePawn(queen);
            pawns.Add(queen);
            __result = pawns;
        }
    }

    /// <summary>Slorg nanoprobes destroy any genetic material taken from their hosts.</summary>
    [HarmonyPatch(typeof(Building_GeneExtractor), nameof(Building_GeneExtractor.CanAcceptPawn))]
    public static class Building_GeneExtractor_CanAcceptPawn_Patch
    {
        public static void Postfix(Pawn pawn, ref AcceptanceReport __result)
        {
            if (__result.Accepted && SlorgUtility.IsSlorg(pawn))
            {
                __result = "Slorg nanoprobes destroy any extracted genetic material.";
            }
        }
    }

    /// <summary>Slorg can't pass their genes on by reimplanting a xenogerm either.</summary>
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.ReimplantXenogerm))]
    public static class GeneUtility_ReimplantXenogerm_Patch
    {
        public static bool Prefix(Pawn caster)
        {
            if (SlorgUtility.IsSlorg(caster))
            {
                Messages.Message("Slorg nanoprobes destroy the xenogerm before it can be implanted.", caster, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }
    }
}

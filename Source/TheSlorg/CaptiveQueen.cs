using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TheSlorg
{
    public class HediffCompProperties_QueenBound : HediffCompProperties
    {
        public HediffCompProperties_QueenBound()
        {
            compClass = typeof(HediffComp_QueenBound);
        }
    }

    /// <summary>A drone summoned by a captive queen. It works for the colony only while she stays captive.</summary>
    public class HediffComp_QueenBound : HediffComp
    {
        public Pawn queen;

        public override string CompLabelInBracketsExtra => queen != null ? queen.LabelShortCap : null;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref queen, "queen");
        }
    }

    /// <summary>
    /// A captured queen, held as a prisoner, can call drones to serve the colony. If she gets free, dies or is severed
    /// while the queen core stands, they turn on it.
    /// </summary>
    public static class CaptiveQueen
    {
        public static bool IsCaptiveQueen(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.IsPrisonerOfColony && SlorgUtility.IsQueen(pawn);
        }

        public static bool IsBound(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_QueenBound);
        }

        public static IEnumerable<Pawn> BoundTo(Pawn queen)
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (QueenOf(pawn) == queen)
                    {
                        yield return pawn;
                    }
                }
            }
        }

        public static Pawn QueenOf(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_QueenBound)?.TryGetComp<HediffComp_QueenBound>()?.queen;
        }

        public static AcceptanceReport CanSummon(Pawn queen)
        {
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            if (!IsCaptiveQueen(queen) || !queen.Spawned)
            {
                return false;
            }
            if (queen.Downed)
            {
                return $"{queen.LabelShortCap} is too weak to call her drones.";
            }
            int bound = BoundTo(queen).Count();
            if (bound >= tuning.captiveQueenMaxDrones)
            {
                return $"{queen.LabelShortCap} already controls {bound} drones.";
            }
            int ready = GameComponent_SlorgCollective.Instance?.QueenSummonReadyTick(queen) ?? 0;
            if (Find.TickManager.TicksGame < ready)
            {
                return $"She can call another drone in {(ready - Find.TickManager.TicksGame).ToStringTicksToPeriod()}.";
            }
            if (!TryFindArrivalCell(queen.Map, out _))
            {
                return "No drone could reach the colony.";
            }
            return true;
        }

        private static bool TryFindArrivalCell(Map map, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out cell);
        }

        public static void Summon(Pawn queen)
        {
            Map map = queen.Map;
            if (!TryFindArrivalCell(map, out IntVec3 cell))
            {
                return;
            }
            Pawn drone = PawnGenerator.GeneratePawn(new PawnGenerationRequest(SlorgDefOf.Slorg_DroneKind, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
                tile: map.Tile, forceGenerateNewPawn: true));
            Hediff bond = HediffMaker.MakeHediff(SlorgDefOf.Slorg_QueenBound, drone);
            bond.TryGetComp<HediffComp_QueenBound>().queen = queen;
            drone.health.AddHediff(bond);
            GenSpawn.Spawn(drone, cell, map);

            GameComponent_SlorgCollective.Instance?.SetQueenSummonReady(queen,
                Find.TickManager.TicksGame + (int)(SlorgDefOf.Slorg_Collective.captiveQueenSummonCooldownDays * GenDate.TicksPerDay));
            Find.LetterStack.ReceiveLetter("A drone answers",
                $"{queen.LabelShortCap} has called a drone to the colony. It will work for you as long as she stays captive.\n\n"
                + "If she escapes, dies or is severed from the hive, every drone bound to her will turn on the colony.",
                LetterDefOf.PositiveEvent, drone);
        }

        /// <summary>Checks every bound drone against its queen. Runs with each collective refresh.</summary>
        public static void CheckBonds()
        {
            Dictionary<Pawn, List<Pawn>> byQueen = new Dictionary<Pawn, List<Pawn>>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    Hediff bond = pawn.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_QueenBound);
                    if (bond == null || pawn.Dead)
                    {
                        continue;
                    }
                    Pawn queen = bond.TryGetComp<HediffComp_QueenBound>()?.queen;
                    if (queen == null)
                    {
                        Release(pawn);
                        continue;
                    }
                    if (!byQueen.TryGetValue(queen, out List<Pawn> list))
                    {
                        byQueen[queen] = list = new List<Pawn>();
                    }
                    list.Add(pawn);
                }
            }

            foreach (KeyValuePair<Pawn, List<Pawn>> entry in byQueen)
            {
                Pawn queen = entry.Key;
                if (IsCaptiveQueen(queen))
                {
                    continue;
                }
                bool escaped = !queen.Dead && !queen.Destroyed && SlorgUtility.IsQueen(queen)
                    && queen.Faction != null && !queen.Faction.IsPlayer && !queen.IsPrisonerOfColony;
                if (escaped)
                {
                    TurnHostile(queen, entry.Value, queen.Faction,
                        $"{queen.LabelShortCap} has escaped. Every drone she called has answered her again: {entry.Value.Count} drone(s) have turned on the colony.");
                    continue;
                }

                // Dead, severed or recruited: her hold is gone. While the queen core stands, the collective raises a new
                // queen and takes her drones back. Only a world without Slorg control lets them go free.
                Map anyMap = entry.Value.FirstOrDefault(p => p.Spawned)?.Map;
                Faction collective = SlorgUtility.ResolveSlorgFaction(queen.Faction, anyMap?.Tile ?? PlanetTile.Invalid);
                if (collective != null)
                {
                    TurnHostile(queen, entry.Value, collective,
                        $"{queen.LabelShortCap}'s hold on her drones is gone, and the collective has already raised a new queen. "
                        + $"The {entry.Value.Count} drone(s) she called have rejoined the hive and turned on the colony.");
                }
                else
                {
                    foreach (Pawn drone in entry.Value)
                    {
                        Release(drone);
                    }
                    Messages.Message($"{queen.LabelShortCap}'s hold is broken, and with the queen core gone there is no hive to answer. Her drones are free.",
                        MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        /// <summary>Queen gone, recruited or severed: the drone is simply free.</summary>
        private static void Release(Pawn drone)
        {
            Hediff bond = drone.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_QueenBound);
            if (bond != null)
            {
                drone.health.RemoveHediff(bond);
            }
            SlorgUtility.MakeDisconnected(drone);
        }

        private static void TurnHostile(Pawn queen, List<Pawn> drones, Faction faction, string letter)
        {
            Dictionary<Map, List<Pawn>> byMap = new Dictionary<Map, List<Pawn>>();
            foreach (Pawn drone in drones)
            {
                Hediff bond = drone.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_QueenBound);
                if (bond != null)
                {
                    drone.health.RemoveHediff(bond);
                }
                if (drone.Drafted)
                {
                    drone.drafter.Drafted = false;
                }
                if (drone.Spawned)
                {
                    if (!byMap.TryGetValue(drone.Map, out List<Pawn> list))
                    {
                        byMap[drone.Map] = list = new List<Pawn>();
                    }
                    list.Add(drone);
                }
                else
                {
                    drone.SetFaction(faction);
                }
            }
            foreach (KeyValuePair<Map, List<Pawn>> entry in byMap)
            {
                SlorgUtility.TurnOnColony(entry.Value, faction, entry.Key);
            }
            Find.LetterStack.ReceiveLetter("Bound drones turn hostile", letter, LetterDefOf.ThreatBig, new LookTargets(drones));
            GameComponent_SlorgCollective.RefreshNow();
        }
    }

    /// <summary>Adds "Summon drone" to a captive queen.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_CaptiveQueen_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (Gizmo gizmo in gizmos)
            {
                yield return gizmo;
            }
            if (!CaptiveQueen.IsCaptiveQueen(__instance))
            {
                yield break;
            }
            Pawn queen = __instance;
            Command_Action summon = new Command_Action
            {
                defaultLabel = "Summon drone",
                defaultDesc = $"Have {queen.LabelShortCap} call a drone to the colony. It will serve you as long as she stays captive. "
                    + "If she escapes, dies or is severed (while the queen core stands), every drone she called turns hostile.\n\n"
                    + $"Drones bound to her: {CaptiveQueen.BoundTo(queen).Count()} / {SlorgDefOf.Slorg_Collective.captiveQueenMaxDrones}",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Xenotypes/Slorg"),
                action = () => CaptiveQueen.Summon(queen)
            };
            AcceptanceReport can = CaptiveQueen.CanSummon(queen);
            if (!can.Accepted)
            {
                summon.Disable(can.Reason);
            }
            yield return summon;
        }
    }
}

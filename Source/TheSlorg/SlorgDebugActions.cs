using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>Dev-mode tools for testing. Open the debug actions menu and look under "The Slorg".</summary>
    public static class SlorgDebugActions
    {
        private const string Category = "The Slorg";

        /// <summary>Makes the next qualifying raid always bring the queen.</summary>
        internal static bool forceQueenNextRaid;

        private static Faction SlorgFaction => Find.FactionManager.FirstFactionOfDef(SlorgDefOf.Slorg_CollectiveFaction);

        [DebugAction(Category, "Infect with nanoprobes", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Infect(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_NanoprobeInfection) || SlorgUtility.HasLinkGene(pawn))
            {
                return;
            }
            Hediff infection = HediffMaker.MakeHediff(SlorgDefOf.Slorg_NanoprobeInfection, pawn);
            infection.TryGetComp<HediffComp_NanoprobeInfection>().sourceFaction = SlorgFaction;
            pawn.health.AddHediff(infection);
        }

        [DebugAction(Category, "Advance infection +30%", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AdvanceInfection(Pawn pawn)
        {
            Hediff infection = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_NanoprobeInfection);
            if (infection != null)
            {
                infection.Severity = UnityEngine.Mathf.Min(infection.Severity + 0.3f, 0.99f);
            }
        }

        [DebugAction(Category, "Complete infection now", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CompleteInfection(Pawn pawn)
        {
            pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_NanoprobeInfection)?.TryGetComp<HediffComp_NanoprobeInfection>()?.Complete();
        }

        [DebugAction(Category, "Make sleeper agent (active now)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void MakeSleeper(Pawn pawn)
        {
            Hediff hediff = HediffComp_DormantNanoprobes.Implant(pawn, SlorgFaction);
            HediffComp_DormantNanoprobes comp = hediff.TryGetComp<HediffComp_DormantNanoprobes>();
            comp.activeAtTick = Find.TickManager.TicksGame;
            comp.nextInjectTick = Find.TickManager.TicksGame;
            Messages.Message($"{pawn.LabelShortCap} is now a hidden sleeper agent.", pawn, MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Category, "Force uprising on this map", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceUprising()
        {
            Map map = Find.CurrentMap;
            var sleepers = map.mapPawns.FreeColonistsSpawned.Where(p => SleeperUtility.SleeperComp(p) != null).ToList();
            if (sleepers.Count == 0)
            {
                Messages.Message("No sleeper agents on this map. Use \"Make sleeper agent\" first.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            SleeperUtility.StartUprising(map, sleepers);
        }

        [DebugAction(Category, "Sever from collective", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Sever(Pawn pawn)
        {
            SlorgUtility.AddSeverance(pawn);
        }

        [DebugAction(Category, "Slorg raid (1500 pts)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void Raid()
        {
            ExecuteRaid(1500f, withQueen: false);
        }

        [DebugAction(Category, "Slorg raid with queen (6000 pts)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RaidWithQueen()
        {
            ExecuteRaid(6000f, withQueen: true);
        }

        private static void ExecuteRaid(float points, bool withQueen)
        {
            Faction faction = SlorgFaction;
            if (faction == null)
            {
                Messages.Message("There is no Slorg faction in this world.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
            parms.faction = faction;
            parms.points = points;
            parms.forced = true;
            forceQueenNextRaid = withQueen;
            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
            forceQueenNextRaid = false;
        }

        [DebugAction(Category, "Spawn queen core", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnQueenCore()
        {
            Faction faction = SlorgFaction;
            if (faction == null)
            {
                return;
            }
            Thing core = ThingMaker.MakeThing(SlorgDefOf.Slorg_QueenCore);
            core.SetFaction(faction);
            GenSpawn.Spawn(core, UI.MouseCell(), Find.CurrentMap, WipeMode.Vanish);
        }

        [DebugAction(Category, "Log collective state", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogCollective()
        {
            GameComponent_SlorgCollective.RefreshNow();
            StringBuilder sb = new StringBuilder("[The Slorg] Collective state\n");
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading.Where(SlorgUtility.IsSlorgFaction))
            {
                GameComponent_SlorgCollective component = GameComponent_SlorgCollective.Instance;
                Pawn queen = component?.QueenOf(faction);
                sb.AppendLine($"{faction.Name}: defeated={faction.defeated}, surfaceControlLost={component?.SurfaceControlLost(faction)}, "
                    + $"queen={(queen == null ? "none" : queen.LabelShortCap + (queen.Spawned ? " (on map)" : " (off map)"))}, "
                    + $"unicomplex={GameComponent_SlorgCollective.UnicomplexOf(faction)?.Label ?? "none"}");
            }
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    SlorgCollective collective = GameComponent_SlorgCollective.CollectiveOf(pawn);
                    if (collective != null)
                    {
                        sb.AppendLine($"  {collective.faction.Name}: {collective.DroneCount} drones on maps, {collective.offMapDrones.Count} off map");
                        foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
                        {
                            Pawn source = collective.SourceFor(skill);
                            if (source != null)
                            {
                                sb.AppendLine($"    {skill.label}: {collective.LevelFor(skill)} from {source.LabelShortCap}");
                            }
                        }
                        sb.AppendLine($"    shared traits: {string.Join(", ", collective.sharedTraits.Select(t => t.def.DataAtDegree(t.degree).label))}");
                        break;
                    }
                }
                int sleepers = map.mapPawns.FreeColonistsSpawned.Count(p => SleeperUtility.SleeperComp(p) != null);
                if (sleepers > 0)
                {
                    sb.AppendLine($"  Map {map}: {sleepers} hidden sleeper agent(s)");
                }
            }
            Log.Message(sb.ToString());
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// Keeps each faction's collective up to date. Drones only count while they are on a map or in a caravan,
    /// so a drone that dies, is captured, or wanders off takes its knowledge with it.
    /// </summary>
    public class GameComponent_SlorgCollective : GameComponent
    {
        private static GameComponent_SlorgCollective instance;

        private readonly Dictionary<Faction, SlorgCollective> collectives = new Dictionary<Faction, SlorgCollective>();
        private readonly Dictionary<Pawn, SlorgCollective> droneLookup = new Dictionary<Pawn, SlorgCollective>();
        private readonly HashSet<Pawn> scratchPawns = new HashSet<Pawn>();
        private bool announceChanges;

        /// <summary>
        /// While true, skill lookups return a drone's own level. Used when reading what each drone contributes.
        /// </summary>
        internal static bool readingNaturalSkills;

        public GameComponent_SlorgCollective(Game game)
        {
            instance = this;
        }

        public static SlorgCollective CollectiveOf(Pawn pawn)
        {
            if (instance == null || pawn == null)
            {
                return null;
            }
            instance.droneLookup.TryGetValue(pawn, out SlorgCollective collective);
            return collective;
        }

        public static void RefreshNow()
        {
            instance?.Refresh();
        }

        public override void StartedNewGame()
        {
            announceChanges = false;
            Refresh();
            announceChanges = true;
        }

        public override void LoadedGame()
        {
            announceChanges = false;
            Refresh();
            announceChanges = true;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % SlorgDefOf.Slorg_Collective.refreshIntervalTicks == 0)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            GatherPawns();

            Dictionary<Faction, SlorgCollective> previous = new Dictionary<Faction, SlorgCollective>(collectives);
            collectives.Clear();
            droneLookup.Clear();

            foreach (Pawn pawn in scratchPawns)
            {
                if (!SlorgUtility.IsLinkedDrone(pawn))
                {
                    // Cut off from the collective (captured, gene removed, ...): lose the link and anything it lent.
                    Hediff stale = pawn.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff);
                    if (stale != null)
                    {
                        pawn.health.RemoveHediff(stale);
                    }
                    continue;
                }

                if (!collectives.TryGetValue(pawn.Faction, out SlorgCollective collective))
                {
                    collective = new SlorgCollective(pawn.Faction);
                    collectives.Add(pawn.Faction, collective);
                }
                collective.drones.Add(pawn);
                droneLookup[pawn] = collective;
            }

            foreach (SlorgCollective collective in collectives.Values)
            {
                GatherKnowledge(collective);
                previous.TryGetValue(collective.faction, out SlorgCollective before);
                AnnounceSkillChanges(before, collective);
                ApplyToDrones(collective);
            }
        }

        private void GatherPawns()
        {
            scratchPawns.Clear();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (pawn.RaceProps.Humanlike)
                    {
                        scratchPawns.Add(pawn);
                    }
                }
            }
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                foreach (Pawn pawn in caravan.PawnsListForReading)
                {
                    if (pawn.RaceProps.Humanlike)
                    {
                        scratchPawns.Add(pawn);
                    }
                }
            }
        }

        private static void GatherKnowledge(SlorgCollective collective)
        {
            readingNaturalSkills = true;
            try
            {
                foreach (Pawn drone in collective.drones)
                {
                    if (drone.skills != null)
                    {
                        foreach (SkillRecord record in drone.skills.skills)
                        {
                            if (record.TotallyDisabled)
                            {
                                continue;
                            }
                            int index = record.def.index;
                            int level = record.Level;
                            if (level > collective.skillLevels[index])
                            {
                                collective.skillLevels[index] = level;
                                collective.skillSources[index] = drone;
                            }
                        }
                    }

                    if (drone.story?.traits != null)
                    {
                        Hediff_CollectiveLink link = LinkOf(drone);
                        foreach (Trait trait in drone.story.traits.allTraits)
                        {
                            if (trait.sourceGene != null
                                || trait.Suppressed
                                || (link != null && link.IsGranted(trait))
                                || !SlorgDefOf.Slorg_Collective.IsShareable(trait)
                                || collective.HasSharedTrait(trait.def, trait.Degree))
                            {
                                continue;
                            }
                            collective.sharedTraits.Add(new SlorgCollective.SharedTrait
                            {
                                def = trait.def,
                                degree = trait.Degree,
                                source = drone
                            });
                        }
                    }
                }
            }
            finally
            {
                readingNaturalSkills = false;
            }
        }

        private void AnnounceSkillChanges(SlorgCollective before, SlorgCollective after)
        {
            if (!announceChanges || before == null || after.faction != Faction.OfPlayer)
            {
                return;
            }

            foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
            {
                Pawn oldSource = before.SourceFor(skill);
                Pawn newSource = after.SourceFor(skill);
                int oldLevel = before.LevelFor(skill);
                int newLevel = after.LevelFor(skill);
                if (oldSource == newSource)
                {
                    continue;
                }

                if (newLevel > oldLevel && newSource != null)
                {
                    Messages.Message(
                        $"The collective has assimilated {newSource.LabelShortCap}'s knowledge of {skill.label} ({oldLevel} → {newLevel}).",
                        newSource, MessageTypeDefOf.PositiveEvent, historical: false);
                }
                else if (newLevel < oldLevel && oldSource != null)
                {
                    Messages.Message(
                        $"The collective has lost {oldSource.LabelShortCap}'s knowledge of {skill.label} ({oldLevel} → {newLevel}).",
                        oldSource, MessageTypeDefOf.NegativeEvent, historical: false);
                }
            }
        }

        private static void ApplyToDrones(SlorgCollective collective)
        {
            foreach (Pawn drone in collective.drones)
            {
                Hediff_CollectiveLink link = LinkOf(drone);
                if (link == null)
                {
                    link = (Hediff_CollectiveLink)HediffMaker.MakeHediff(SlorgDefOf.Slorg_CollectiveLinkHediff, drone);
                    drone.health.AddHediff(link);
                }
                link.Severity = collective.DroneCount;
                link.SyncTraits(collective);
            }
        }

        private static Hediff_CollectiveLink LinkOf(Pawn pawn)
        {
            return pawn.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff) as Hediff_CollectiveLink;
        }
    }
}

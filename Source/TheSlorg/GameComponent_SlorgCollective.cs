using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// Runs every Slorg collective:
    ///  - rebuilds each faction's shared skills and traits from its living drones (a lost drone takes its knowledge with it),
    ///  - watches each faction's queen and severs the drones around her when she falls,
    ///  - ends Slorg control of the planet surface when a queen core is destroyed.
    /// </summary>
    public class GameComponent_SlorgCollective : GameComponent
    {
        private static GameComponent_SlorgCollective instance;

        // Saved state.
        private Dictionary<Faction, Pawn> queens = new Dictionary<Faction, Pawn>();
        private List<Faction> surfaceControlLost = new List<Faction>();
        private Dictionary<Pawn, int> queenSummonReady = new Dictionary<Pawn, int>();
        private List<Faction> unicomplexRevealed = new List<Faction>();
        private List<Pawn> tmpSummonKeys;
        private List<int> tmpSummonValues;

        // Rebuilt every refresh.
        private readonly Dictionary<Faction, SlorgCollective> collectives = new Dictionary<Faction, SlorgCollective>();
        private readonly Dictionary<Pawn, SlorgCollective> droneLookup = new Dictionary<Pawn, SlorgCollective>();
        private readonly Dictionary<Faction, Map> lastQueenMap = new Dictionary<Faction, Map>();
        private readonly HashSet<Pawn> activePawns = new HashSet<Pawn>();
        private bool announceChanges;

        private List<Faction> tmpQueenKeys;
        private List<Pawn> tmpQueenValues;

        /// <summary>While true, skill lookups return a drone's own level. Used when reading what each drone contributes.</summary>
        internal static bool readingNaturalSkills;

        public GameComponent_SlorgCollective(Game game)
        {
            instance = this;
        }

        public static GameComponent_SlorgCollective Instance => instance;

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

        /// <summary>The queen core stays hidden until someone traces the hive signal with a control implant.</summary>
        public bool UnicomplexRevealed(Faction faction)
        {
            return faction != null && unicomplexRevealed.Contains(faction);
        }

        public void RevealUnicomplex(Faction faction)
        {
            if (faction != null && !unicomplexRevealed.Contains(faction))
            {
                unicomplexRevealed.Add(faction);
                NameUnicomplexes();
            }
        }

        public int QueenSummonReadyTick(Pawn queen)
        {
            return queenSummonReady.TryGetValue(queen, out int tick) ? tick : 0;
        }

        public void SetQueenSummonReady(Pawn queen, int tick)
        {
            queenSummonReady[queen] = tick;
        }

        public bool SurfaceControlLost(Faction faction)
        {
            return faction != null && surfaceControlLost.Contains(faction);
        }

        public override void StartedNewGame()
        {
            OnGameReady();
        }

        public override void LoadedGame()
        {
            OnGameReady();
        }

        private void OnGameReady()
        {
            announceChanges = false;
            NameUnicomplexes();
            Refresh();
            announceChanges = true;
        }

        private readonly List<KeyValuePair<HediffComp_NanoprobeInfection, InfectionAction>> pendingInfectionActions =
            new List<KeyValuePair<HediffComp_NanoprobeInfection, InfectionAction>>();

        /// <summary>Infection stage changes run here, outside the health tick that noticed them.</summary>
        public void QueueInfectionAction(HediffComp_NanoprobeInfection infection, InfectionAction action)
        {
            pendingInfectionActions.Add(new KeyValuePair<HediffComp_NanoprobeInfection, InfectionAction>(infection, action));
        }

        public override void GameComponentTick()
        {
            if (pendingInfectionActions.Count > 0)
            {
                var ready = new List<KeyValuePair<HediffComp_NanoprobeInfection, InfectionAction>>(pendingInfectionActions);
                pendingInfectionActions.Clear();
                foreach (var entry in ready)
                {
                    entry.Key.Run(entry.Value);
                }
            }
            if (Find.TickManager.TicksGame % SlorgDefOf.Slorg_Collective.refreshIntervalTicks == 0)
            {
                foreach (Map map in Find.Maps.ToList())
                {
                    if (map.IsPlayerHome)
                    {
                        foreach (Pawn prisoner in map.mapPawns.PrisonersOfColonySpawned.ToList())
                        {
                            QueenSuppression.Maintain(prisoner);
                        }
                        QueenSuppression.TickMap(map);
                        SleeperUtility.TickMap(map);
                        CheckCollectiveCall(map);
                    }
                }
                Refresh();
            }
        }

        public void Refresh()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn prisoner in map.mapPawns.PrisonersOfColonySpawned.ToList())
                {
                    QueenSuppression.Maintain(prisoner);
                }
            }
            CheckQueens();
            CaptiveQueen.CheckBonds();
            GatherActivePawns();

            Dictionary<Faction, SlorgCollective> previous = new Dictionary<Faction, SlorgCollective>(collectives);
            collectives.Clear();
            droneLookup.Clear();

            SlorgImplants.PruneSkillCache();
            foreach (Pawn pawn in activePawns)
            {
                SlorgImplants.UpdateSkillBonus(pawn);
                if (pawn.Faction != null && pawn.Faction.IsPlayer && !pawn.IsPrisoner && SlorgUtility.HasLinkGene(pawn)
                    && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_NanoprobeInfection)
                    && !CaptiveQueen.IsBound(pawn))
                {
                    // The collective cannot hold a drone that serves the player.
                    SlorgUtility.MakeDisconnected(pawn);
                    Messages.Message($"{pawn.LabelShortCap}'s link to the collective has burned out. They are now a disconnected drone.",
                        pawn, MessageTypeDefOf.NeutralEvent);
                    continue;
                }

                if (!SlorgUtility.IsLinkedDrone(pawn))
                {
                    Hediff stale = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff);
                    if (stale != null)
                    {
                        pawn.health.RemoveHediff(stale);
                    }
                    continue;
                }

                SlorgCollective collective = GetOrMake(pawn.Faction);
                collective.drones.Add(pawn);
                droneLookup[pawn] = collective;
            }

            // Drones off-map (away with raids, holding settlements) still know things.
            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAlive)
            {
                if (!droneLookup.ContainsKey(pawn) && SlorgUtility.IsLinkedDrone(pawn))
                {
                    GetOrMake(pawn.Faction).offMapDrones.Add(pawn);
                }
            }

            foreach (SlorgCollective collective in collectives.Values)
            {
                foreach (Pawn drone in collective.drones)
                {
                    SlorgUtility.NanoprobeHeal(drone);
                }
                GatherKnowledge(collective);
                previous.TryGetValue(collective.faction, out SlorgCollective before);
                AnnounceSkillChanges(before, collective);
                ApplyToDrones(collective);
            }
        }

        /// <summary>Freed Slorg who still carry implants can hear the collective again, very rarely.</summary>
        private static void CheckCollectiveCall(Map map)
        {
            if (SlorgUtility.ResolveSlorgFaction(null, map.Tile) == null)
            {
                return;
            }
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned.ToList())
            {
                if (pawn.Downed || pawn.InMentalState || !SlorgImplants.HasAnyImplant(pawn)
                    || (pawn.genes != null && pawn.genes.HasActiveGene(SlorgDefOf.Slorg_LiberatedSovereign)))
                {
                    continue;
                }
                if (!Rand.MTBEventOccurs(SlorgDefOf.Slorg_Collective.collectiveCallMtbDays, GenDate.TicksPerDay, SlorgDefOf.Slorg_Collective.refreshIntervalTicks))
                {
                    continue;
                }
                if (pawn.mindState.mentalStateHandler.TryStartMentalState(SlorgDefOf.Slorg_CollectiveCall, "the collective's call", forceWake: true))
                {
                    Find.LetterStack.ReceiveLetter("The collective's call",
                        $"{pawn.LabelShortCap}'s implants have picked up the collective again. {pawn.LabelShortCap} is walking away from the colony to rejoin it.\n\n"
                        + "Arrest them or down them before they leave the map, or they are gone for good.",
                        LetterDefOf.ThreatSmall, pawn);
                }
            }
        }

        private SlorgCollective GetOrMake(Faction faction)
        {
            if (!collectives.TryGetValue(faction, out SlorgCollective collective))
            {
                collective = new SlorgCollective(faction);
                collectives.Add(faction, collective);
            }
            return collective;
        }

        private void GatherActivePawns()
        {
            activePawns.Clear();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (pawn.RaceProps.Humanlike)
                    {
                        activePawns.Add(pawn);
                    }
                }
            }
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                foreach (Pawn pawn in caravan.PawnsListForReading)
                {
                    if (pawn.RaceProps.Humanlike)
                    {
                        activePawns.Add(pawn);
                    }
                }
            }
        }

        private static void GatherKnowledge(SlorgCollective collective)
        {
            readingNaturalSkills = true;
            try
            {
                foreach (Pawn drone in collective.AllDrones)
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

                    if (SlorgUtility.IsQueen(drone) && drone.MapHeld != null)
                    {
                        collective.queenMap = drone.MapHeld;
                    }
                }
            }
            finally
            {
                readingNaturalSkills = false;
            }
        }

        /// <summary>Only report changes the player can see: the drone that gained or lost the skill is on a map.</summary>
        private void AnnounceSkillChanges(SlorgCollective before, SlorgCollective after)
        {
            if (!announceChanges || before == null)
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

                if (newLevel > oldLevel && newSource?.MapHeld != null)
                {
                    Messages.Message(
                        $"The Slorg collective has assimilated {newSource.LabelShortCap}'s knowledge of {skill.label} ({oldLevel} → {newLevel}).",
                        newSource, MessageTypeDefOf.NegativeEvent, historical: false);
                }
                else if (newLevel < oldLevel && oldSource?.MapHeld != null)
                {
                    Messages.Message(
                        $"The Slorg collective has lost {oldSource.LabelShortCap}'s knowledge of {skill.label} ({oldLevel} → {newLevel}).",
                        new LookTargets(oldSource.PositionHeld, oldSource.MapHeld), MessageTypeDefOf.PositiveEvent, historical: false);
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
                int strength = collective.DroneCount;
                if (collective.queenMap != null && drone.MapHeld == collective.queenMap)
                {
                    strength += SlorgDefOf.Slorg_Collective.queenPresenceBonusDrones;
                }
                link.Severity = strength;
                link.SyncTraits(collective);
            }
        }

        private static Hediff_CollectiveLink LinkOf(Pawn pawn)
        {
            return pawn.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff) as Hediff_CollectiveLink;
        }

        // ---------------------------------------------------------------- Queens

        public Pawn QueenOf(Faction faction)
        {
            queens.TryGetValue(faction, out Pawn queen);
            return queen;
        }

        /// <summary>Settles succession right now and returns the faction's reigning queen, raising a new one if needed.</summary>
        public Pawn EnsureQueen(Faction faction)
        {
            CheckQueens();
            return QueenOf(faction);
        }

        private void CheckQueens()
        {
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!SlorgUtility.IsSlorgFaction(faction) || faction.defeated)
                {
                    continue;
                }

                Pawn queen = QueenOf(faction);
                if (queen != null)
                {
                    bool captured = queen.Faction != faction || (queen.IsPrisoner && queen.HostFaction != faction);
                    if (queen.Dead || queen.Destroyed || captured)
                    {
                        lastQueenMap.TryGetValue(faction, out Map fallenOn);
                        OnQueenFallen(faction, queen, queen.MapHeld ?? fallenOn, captured && !queen.Dead);
                        queens.Remove(faction);
                        queen = null;
                    }
                    else
                    {
                        lastQueenMap.TryGetValue(faction, out Map before);
                        Map now = queen.MapHeld;
                        if (now != null && now != before && queen.Spawned && now.IsPlayerHome && announceChanges)
                        {
                            Find.LetterStack.ReceiveLetter("The Slorg queen is here",
                                $"{queen.LabelShortCap}, queen of {faction.Name}, has come with this assault. Her drones fight harder near her, "
                                + "and anyone she injects becomes a drone on the spot.\n\nKill or capture her and every drone on the map is severed from the collective.",
                                LetterDefOf.ThreatBig, queen);
                        }
                        lastQueenMap[faction] = now;
                    }
                }

                if (queen == null)
                {
                    // The collective always raises a new queen.
                    if (faction.leader == null || faction.leader.Dead || !SlorgUtility.IsQueen(faction.leader) || faction.leader.Faction != faction)
                    {
                        faction.TryGenerateNewLeader();
                    }
                    if (faction.leader != null && !faction.leader.Dead && SlorgUtility.IsQueen(faction.leader))
                    {
                        queens[faction] = faction.leader;
                    }
                }
            }
        }

        private void OnQueenFallen(Faction faction, Pawn queen, Map map, bool captured)
        {
            if (captured)
            {
                SlorgUtility.AddSeverance(queen);
                if (faction.leader == queen)
                {
                    faction.leader = null;
                }
            }

            int severed = 0;
            if (map != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns.ToList())
                {
                    if (pawn.Faction == faction && SlorgUtility.IsDrone(pawn))
                    {
                        SlorgUtility.AddSeverance(pawn);
                        severed++;
                    }
                }
            }

            if (severed > 0 || map != null)
            {
                Find.LetterStack.ReceiveLetter(
                    "Slorg queen fallen",
                    $"{queen.LabelShortCap}, queen of {faction.Name}, has been {(captured ? "captured" : "killed")}.\n\n"
                    + $"{severed} drone(s) nearby have been severed from the collective and have collapsed. For the next couple of days they can be captured, "
                    + "and a skilled doctor with glitterworld medicine can perform the Sever link surgery on them to free them permanently.\n\n"
                    + "Elsewhere, the collective is already raising a new queen.",
                    LetterDefOf.PositiveEvent,
                    new LookTargets(queen.PositionHeld, map));
            }
        }

        // ---------------------------------------------------------------- Unicomplex and queen core

        /// <summary>The queen core sits in the faction's oldest settlement on the planet surface.</summary>
        public static Settlement UnicomplexOf(Faction faction)
        {
            Settlement best = null;
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (settlement.Faction == faction && !SlorgUtility.IsSpace(settlement.Tile) && (best == null || settlement.ID < best.ID))
                {
                    best = settlement;
                }
            }
            return best;
        }

        public void NameUnicomplexes()
        {
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!SlorgUtility.IsSlorgFaction(faction) || SurfaceControlLost(faction) || !UnicomplexRevealed(faction))
                {
                    continue;
                }
                Settlement unicomplex = UnicomplexOf(faction);
                if (unicomplex != null)
                {
                    unicomplex.Name = SlorgDefOf.Slorg_Collective.unicomplexName;
                }
            }
        }

        /// <summary>
        /// The queen core is destroyed: every drone of this faction on the planet surface is permanently disconnected, its
        /// surface settlements fall, and it can no longer raid the surface. Any presence in space is unaffected.
        /// </summary>
        public void Notify_QueenCoreDestroyed(Faction faction, Map map)
        {
            if (faction == null || SurfaceControlLost(faction))
            {
                return;
            }
            surfaceControlLost.Add(faction);

            int disconnected = 0;
            List<Pawn> pawns = new List<Pawn>();
            foreach (Map m in Find.Maps)
            {
                if (!SlorgUtility.IsSpace(m.Tile))
                {
                    pawns.AddRange(m.mapPawns.AllPawns);
                }
            }
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                pawns.AddRange(caravan.PawnsListForReading);
            }
            pawns.AddRange(Find.WorldPawns.AllPawnsAlive);

            foreach (Pawn pawn in pawns)
            {
                if (pawn.Dead || pawn.Faction != faction || !SlorgUtility.IsSlorg(pawn))
                {
                    continue;
                }
                SlorgUtility.MakeDisconnected(pawn);
                if (pawn.Spawned)
                {
                    SlorgUtility.AddSeverance(pawn);
                }
                if (!pawn.IsPrisoner)
                {
                    pawn.SetFaction(null);
                }
                disconnected++;
            }

            foreach (Settlement settlement in Find.WorldObjects.Settlements.ToList())
            {
                if (settlement.Faction == faction && !settlement.HasMap && !SlorgUtility.IsSpace(settlement.Tile))
                {
                    settlement.Destroy();
                }
            }
            queens.Remove(faction);
            if (faction.leader != null && faction.leader.Faction != faction)
            {
                faction.leader = null;
            }

            Find.LetterStack.ReceiveLetter(
                "Queen core destroyed",
                $"The queen core of {faction.Name} has been destroyed. The collective has lost control of this world.\n\n"
                + $"{disconnected} drone(s) have been permanently disconnected. They are now individuals with no faction, dazed for the next couple of days, "
                + "and can be captured and recruited.\n\n"
                + "The Slorg will no longer raid the surface, but they remain out there in space.",
                LetterDefOf.PositiveEvent,
                map != null ? new LookTargets(map.Center, map) : LookTargets.Invalid);

            Refresh();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref queens, "queens", LookMode.Reference, LookMode.Reference, ref tmpQueenKeys, ref tmpQueenValues);
            Scribe_Collections.Look(ref surfaceControlLost, "surfaceControlLost", LookMode.Reference);
            Scribe_Collections.Look(ref queenSummonReady, "queenSummonReady", LookMode.Reference, LookMode.Value, ref tmpSummonKeys, ref tmpSummonValues);
            Scribe_Collections.Look(ref unicomplexRevealed, "unicomplexRevealed", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queens ??= new Dictionary<Faction, Pawn>();
                surfaceControlLost ??= new List<Faction>();
                queens.RemoveAll(kv => kv.Key == null || kv.Value == null);
                surfaceControlLost.RemoveAll(f => f == null);
                queenSummonReady ??= new Dictionary<Pawn, int>();
                unicomplexRevealed ??= new List<Faction>();
                unicomplexRevealed.RemoveAll(f => f == null);
                queenSummonReady.RemoveAll(kv => kv.Key == null);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace TheSlorg
{
    /// <summary>
    /// Once located, the Unicomplex is the heart of the hive: the queen core, the queen herself, a heavy garrison,
    /// turrets, regeneration alcoves and hive conduits.
    /// </summary>
    public class MapComponent_SlorgUnicomplex : MapComponent
    {
        private bool coreSpawned;

        public MapComponent_SlorgUnicomplex(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            TrySetUp();
        }

        public override void MapComponentTick()
        {
            // Covers the hive signal being traced while the Unicomplex map is already open.
            if (!coreSpawned && map.Parent is Settlement && Find.TickManager.TicksGame % 250 == 0)
            {
                TrySetUp();
            }
        }

        private void TrySetUp()
        {
            if (coreSpawned || !(map.Parent is Settlement settlement))
            {
                return;
            }
            Faction faction = settlement.Faction;
            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            if (!SlorgUtility.IsSlorgFaction(faction)
                || collective == null
                || collective.SurfaceControlLost(faction)
                || !collective.UnicomplexRevealed(faction)
                || GameComponent_SlorgCollective.UnicomplexOf(faction) != settlement)
            {
                return;
            }

            coreSpawned = true;
            IntVec3 center = map.Center;
            if (map.listerThings.ThingsOfDef(SlorgDefOf.Slorg_QueenCore).Count == 0)
            {
                Thing core = ThingMaker.MakeThing(SlorgDefOf.Slorg_QueenCore);
                core.SetFaction(faction);
                GenSpawn.Spawn(core, center, map, WipeMode.Vanish);
            }

            Fortify(faction, center);
            List<Pawn> defenders = SpawnGarrison(faction, center);
            Pawn queen = SummonQueen(faction, collective, center);
            if (queen != null)
            {
                defenders.Add(queen);
            }
            Lord lord = map.lordManager.lords.FirstOrDefault(l => l.faction == faction)
                ?? LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, center, 180000, false), map);
            foreach (Pawn defender in defenders)
            {
                if (defender.GetLord() == null)
                {
                    lord.AddPawn(defender);
                }
            }
            GameComponent_SlorgCollective.RefreshNow();
        }

        /// <summary>
        /// There is only ever one queen, and she is always home when the Unicomplex is attacked. If she's somewhere else,
        /// even in the middle of a raid, she's pulled back through the hive's transwarp link.
        /// </summary>
        private Pawn SummonQueen(Faction faction, GameComponent_SlorgCollective collective, IntVec3 center)
        {
            Pawn queen = collective.EnsureQueen(faction);
            if (queen == null || queen.Dead || queen.Faction != faction)
            {
                return null;
            }
            if (queen.Spawned && queen.Map == map)
            {
                return queen;
            }

            bool wasAway = queen.Spawned || queen.GetCaravan() != null;
            if (queen.Spawned)
            {
                Map from = queen.Map;
                IntVec3 at = queen.Position;
                queen.GetLord()?.RemovePawn(queen);
                queen.DeSpawn();
                FleckMaker.ThrowLightningGlow(at.ToVector3Shifted(), from, 2.5f);
                FleckMaker.ThrowMicroSparks(at.ToVector3Shifted(), from);
                if (from.IsPlayerHome)
                {
                    Messages.Message($"{queen.LabelShortCap} vanishes in a flash of green light. The Unicomplex has called its queen home.",
                        new LookTargets(at, from), MessageTypeDefOf.NeutralEvent);
                }
            }
            else if (queen.GetCaravan() is Caravan caravan)
            {
                caravan.RemovePawn(queen);
                if (!caravan.PawnsListForReading.Any())
                {
                    caravan.Destroy();
                }
            }
            else if (queen.holdingOwner != null)
            {
                queen.holdingOwner.Remove(queen);
            }
            if (Find.WorldPawns.Contains(queen))
            {
                Find.WorldPawns.RemovePawn(queen);
            }

            if (!CellFinder.TryFindRandomCellNear(center, map, 3, c => c.Standable(map) && c.GetFirstPawn(map) == null, out IntVec3 spot))
            {
                spot = CellFinder.RandomClosewalkCellNear(center, map, 6);
            }
            GenSpawn.Spawn(queen, spot, map);
            FleckMaker.ThrowLightningGlow(spot.ToVector3Shifted(), map, 2.5f);
            FleckMaker.ThrowMicroSparks(spot.ToVector3Shifted(), map);
            if (wasAway)
            {
                Find.LetterStack.ReceiveLetter("The queen returns",
                    $"{queen.LabelShortCap} has transwarped back to the Unicomplex to defend the queen core.",
                    LetterDefOf.ThreatBig, queen);
            }
            return queen;
        }

        private void Fortify(Faction faction, IntVec3 center)
        {
            for (int i = 0; i < 4; i++)
            {
                TryPlace(SlorgDefOf.Slorg_PlasmaTurret, faction, center, 7, 14);
                TryPlace(SlorgDefOf.Slorg_DisruptorTurret, faction, center, 5, 12);
            }
            for (int i = 0; i < 10; i++)
            {
                TryPlace(SlorgDefOf.Slorg_Alcove, faction, center, 3, 10);
            }
            for (int i = 0; i < 12; i++)
            {
                TryPlace(SlorgDefOf.Slorg_Conduit, faction, center, 3, 16);
            }
        }

        private void TryPlace(ThingDef def, Faction faction, IntVec3 center, int minDist, int maxDist)
        {
            bool Fits(IntVec3 c)
            {
                if (c.DistanceTo(center) < minDist)
                {
                    return false;
                }
                foreach (IntVec3 cell in GenAdj.OccupiedRect(c, Rot4.North, def.Size))
                {
                    if (!cell.InBounds(map) || !cell.Standable(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null)
                    {
                        return false;
                    }
                }
                return true;
            }
            if (CellFinder.TryFindRandomCellNear(center, map, maxDist, Fits, out IntVec3 spot))
            {
                Thing thing = ThingMaker.MakeThing(def);
                thing.SetFaction(faction);
                GenSpawn.Spawn(thing, spot, map, WipeMode.Vanish);
            }
        }

        private List<Pawn> SpawnGarrison(Faction faction, IntVec3 center)
        {
            List<Pawn> spawned = new List<Pawn>();

            PawnKindDef[] garrison =
            {
                SlorgDefOf.Slorg_AssaultDroneKind, SlorgDefOf.Slorg_AssaultDroneKind, SlorgDefOf.Slorg_AssaultDroneKind,
                SlorgDefOf.Slorg_TacticalDroneKind, SlorgDefOf.Slorg_TacticalDroneKind,
                SlorgDefOf.Slorg_DroneKind, SlorgDefOf.Slorg_DroneKind, SlorgDefOf.Slorg_DroneKind
            };
            foreach (PawnKindDef kind in garrison)
            {
                Pawn drone = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, PawnGenerationContext.NonPlayer,
                    tile: map.Tile, forceGenerateNewPawn: true));
                if (SpawnNear(drone, center, 8))
                {
                    spawned.Add(drone);
                }
            }
            return spawned;
        }

        private bool SpawnNear(Pawn pawn, IntVec3 center, int radius)
        {
            if (!CellFinder.TryFindRandomCellNear(center, map, radius, c => c.Standable(map) && c.GetFirstPawn(map) == null, out IntVec3 spot))
            {
                return false;
            }
            GenSpawn.Spawn(pawn, spot, map);
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref coreSpawned, "coreSpawned");
        }
    }
}

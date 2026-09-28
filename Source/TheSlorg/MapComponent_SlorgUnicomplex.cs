using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheSlorg
{
    /// <summary>Places the queen core when the Unicomplex's map is generated.</summary>
    public class MapComponent_SlorgUnicomplex : MapComponent
    {
        private bool coreSpawned;

        public MapComponent_SlorgUnicomplex(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (coreSpawned || !(map.Parent is Settlement settlement))
            {
                return;
            }
            Faction faction = settlement.Faction;
            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            if (!SlorgUtility.IsSlorgFaction(faction)
                || collective == null
                || collective.SurfaceControlLost(faction)
                || GameComponent_SlorgCollective.UnicomplexOf(faction) != settlement)
            {
                return;
            }

            coreSpawned = true;
            if (map.listerThings.ThingsOfDef(SlorgDefOf.Slorg_QueenCore).Count > 0)
            {
                return;
            }
            Thing core = ThingMaker.MakeThing(SlorgDefOf.Slorg_QueenCore);
            core.SetFaction(faction);
            GenSpawn.Spawn(core, map.Center, map, WipeMode.Vanish);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref coreSpawned, "coreSpawned");
        }
    }
}

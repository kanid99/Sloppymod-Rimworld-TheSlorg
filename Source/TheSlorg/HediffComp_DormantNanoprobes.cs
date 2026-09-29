using RimWorld;
using Verse;

namespace TheSlorg
{
    public class HediffCompProperties_DormantNanoprobes : HediffCompProperties
    {
        public HediffCompProperties_DormantNanoprobes()
        {
            compClass = typeof(HediffComp_DormantNanoprobes);
        }
    }

    /// <summary>
    /// A hidden sleeper agent. The carrier looks cured and keeps working for the colony, but once active it
    /// quietly injects other colonists. Sleepers rise up together when there are enough of them.
    /// The sleeper logic itself runs in GameComponent_SlorgCollective.
    /// </summary>
    public class HediffComp_DormantNanoprobes : HediffComp
    {
        public Faction sourceFaction;
        public int activeAtTick;
        public int nextInjectTick;

        public bool IsActive => Find.TickManager.TicksGame >= activeAtTick;

        public static Hediff Implant(Pawn pawn, Faction source)
        {
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_DormantNanoprobes);
            if (existing != null)
            {
                return existing;
            }
            Hediff hediff = HediffMaker.MakeHediff(SlorgDefOf.Slorg_DormantNanoprobes, pawn);
            HediffComp_DormantNanoprobes comp = hediff.TryGetComp<HediffComp_DormantNanoprobes>();
            comp.sourceFaction = source;
            comp.activeAtTick = Find.TickManager.TicksGame + SlorgDefOf.Slorg_Collective.sleeperIncubationTicks;
            comp.nextInjectTick = comp.activeAtTick;
            pawn.health.AddHediff(hediff);
            return hediff;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref sourceFaction, "sourceFaction");
            Scribe_Values.Look(ref activeAtTick, "activeAtTick");
            Scribe_Values.Look(ref nextInjectTick, "nextInjectTick");
        }
    }
}

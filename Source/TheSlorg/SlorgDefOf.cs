using RimWorld;
using Verse;

namespace TheSlorg
{
    [DefOf]
    public static class SlorgDefOf
    {
        public static XenotypeDef Slorg_Drone;
        public static GeneDef Slorg_CollectiveLink;
        public static HediffDef Slorg_CollectiveLinkHediff;
        public static SlorgCollectiveDef Slorg_Collective;

        static SlorgDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SlorgDefOf));
        }
    }
}

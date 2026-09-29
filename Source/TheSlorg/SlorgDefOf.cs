using RimWorld;
using Verse;

namespace TheSlorg
{
    [DefOf]
    public static class SlorgDefOf
    {
        public static XenotypeDef Slorg_Drone;
        public static XenotypeDef Slorg_Queen;
        public static XenotypeDef Slorg_DisconnectedDrone;
        public static XenotypeDef Slorg_Thrall;

        public static GeneDef Slorg_CollectiveLink;
        public static GeneDef Slorg_HiveSovereign;
        public static GeneDef Slorg_Severed;

        public static HediffDef Slorg_CollectiveLinkHediff;
        public static HediffDef Slorg_NanoprobeInfection;
        public static HediffDef Slorg_Severance;
        public static HediffDef Slorg_DormantNanoprobes;

        public static JobDef Slorg_SecretInject;
        public static MentalStateDef Slorg_CollectiveCall;

        public static FactionDef Slorg_CollectiveFaction;
        public static ThingDef Slorg_QueenCore;
        public static AbilityDef Slorg_Assimilate;

        public static PawnKindDef Slorg_DroneKind;
        public static AbilityDef Slorg_CuttingBeam;

        public static SlorgCollectiveDef Slorg_Collective;

        static SlorgDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SlorgDefOf));
        }
    }
}

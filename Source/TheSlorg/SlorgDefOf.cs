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
        public static XenotypeDef Slorg_FreedQueen;

        public static GeneDef Slorg_CollectiveLink;
        public static GeneDef Slorg_HiveSovereign;
        public static GeneDef Slorg_Severed;
        public static GeneDef Slorg_LiberatedSovereign;

        public static HediffDef Slorg_CollectiveLinkHediff;
        public static HediffDef Slorg_NanoprobeInfection;
        public static HediffDef Slorg_Severance;
        public static HediffDef Slorg_DormantNanoprobes;
        public static HediffDef Slorg_QueenBound;
        public static HediffDef Slorg_QueenSuppression;
        public static HediffDef Slorg_ControlImplant;

        public static JobDef Slorg_SecretInject;
        public static JobDef Slorg_SuppressQueen;
        public static MentalStateDef Slorg_CollectiveCall;

        public static FactionDef Slorg_CollectiveFaction;
        public static ThingDef Slorg_QueenCore;
        public static ThingDef Slorg_ControlImplantItem;
        public static AbilityDef Slorg_Assimilate;

        public static PawnKindDef Slorg_DroneKind;
        public static AbilityDef Slorg_CuttingBeam;
        public static AbilityDef Slorg_PlasmaLance;
        public static PawnKindDef Slorg_QueenKind;
        public static PawnKindDef Slorg_TacticalDroneKind;
        public static PawnKindDef Slorg_AssaultDroneKind;
        public static ThingDef Slorg_DisruptorTurret;
        public static ThingDef Slorg_PlasmaTurret;
        public static ThingDef Slorg_Alcove;
        public static ThingDef Slorg_Conduit;

        public static SlorgCollectiveDef Slorg_Collective;

        static SlorgDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SlorgDefOf));
        }
    }
}

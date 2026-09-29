using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// Tuning for the collective. Lives in XML so balance can be changed without recompiling.
    /// </summary>
    public class SlorgCollectiveDef : Def
    {
        /// <summary>How often (in ticks) the collective re-evaluates its drones.</summary>
        public int refreshIntervalTicks = 500;

        /// <summary>A living queen on the same map counts as this many extra drones for the collective link stages.</summary>
        public int queenPresenceBonusDrones = 10;

        /// <summary>Raids of at least this many points may bring the queen along.</summary>
        public float queenRaidMinPoints = 5000f;

        /// <summary>Chance a qualifying raid brings the queen.</summary>
        public float queenRaidChance = 0.5f;

        /// <summary>Name given to the settlement that holds the queen core.</summary>
        public string unicomplexName = "Unicomplex";

        /// <summary>Chance that a "purged" infection actually goes dormant, leaving a hidden sleeper agent.</summary>
        public float dormantChanceOnPurge = 0.4f;

        /// <summary>Chance a prisoner or slave who succumbs becomes a hidden sleeper instead of breaking out as a hostile thrall.</summary>
        public float captiveSleeperChance = 0.5f;

        /// <summary>How long a new sleeper waits before it starts injecting others.</summary>
        public int sleeperIncubationTicks = 60000;

        /// <summary>Days between a sleeper's injections.</summary>
        public FloatRange sleeperInjectIntervalDays = new FloatRange(0.5f, 1.5f);

        /// <summary>Chance an injection is noticed when another colonist is awake nearby, and when nobody is.</summary>
        public float witnessChanceObserved = 0.6f;
        public float witnessChanceUnobserved = 0.1f;

        /// <summary>Sleepers rise up once there are at least this many and they make up this share of the colony.</summary>
        public int uprisingMinSleepers = 3;
        public float uprisingColonistFraction = 0.34f;

        /// <summary>Chance the collective sends a raid to back up an uprising, and when it arrives.</summary>
        public float uprisingRaidChance = 0.5f;
        public int uprisingRaidDelayTicks = 2500;
        public float uprisingRaidPointsFactor = 0.7f;

        /// <summary>Colonists carrying Slorg implants: mean days between the collective's call (a mental break to rejoin it).</summary>
        public float collectiveCallMtbDays = 60f;

        /// <summary>A captive queen can call a drone this often, up to this many at once.</summary>
        public float captiveQueenSummonCooldownDays = 3f;
        public int captiveQueenMaxDrones = 6;

        /// <summary>Captive queen suppression (0-1): starting level, how much a warden session adds (plus per Social level),
        /// when wardens start working on it, the minimum to summon, and the level below which her drones seed sleepers.</summary>
        public float queenSuppressionStart = 0.5f;
        public float queenSuppressionPerSession = 0.15f;
        public float queenSuppressionPerSocialLevel = 0.015f;
        public float queenSuppressionWorkBelow = 0.8f;
        public float queenSuppressionToSummon = 0.5f;
        public float queenSuppressionDangerLevel = 0.3f;

        /// <summary>Mean days between hidden infections by her drones, just under the danger level and at zero suppression.</summary>
        public float queenSeedingMtbDaysAtDanger = 3f;
        public float queenSeedingMtbDaysAtZero = 0.5f;

        /// <summary>Conditions nanoprobes cure in linked drones, on top of anything marked chronic.</summary>
        public List<HediffDef> nanoprobeCures = new List<HediffDef>();

        /// <summary>Traits a drone can share with the whole collective. A trait only spreads while a drone that naturally has it is linked.</summary>
        public List<ShareableTrait> shareableTraits = new List<ShareableTrait>();

        public bool IsShareable(Trait trait)
        {
            foreach (ShareableTrait entry in shareableTraits)
            {
                if (entry.def == trait.def && entry.degree == trait.Degree)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class ShareableTrait
    {
        public TraitDef def;
        public int degree;
    }
}

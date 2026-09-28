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

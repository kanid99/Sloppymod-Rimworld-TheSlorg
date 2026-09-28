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
        public int refreshIntervalTicks = 250;

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

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// One faction's collective. Nothing in here is saved: it is rebuilt from the living drones on every refresh,
    /// so the collective only knows what its current drones know.
    /// </summary>
    public class SlorgCollective
    {
        public readonly Faction faction;
        /// <summary>Linked drones on a map or in a caravan. These receive the collective's skills, traits and stats.</summary>
        public readonly List<Pawn> drones = new List<Pawn>();

        /// <summary>Linked drones elsewhere in the world. They still contribute what they know.</summary>
        public readonly List<Pawn> offMapDrones = new List<Pawn>();

        /// <summary>The map the queen is on, if she is on one.</summary>
        public Map queenMap;

        /// <summary>Best natural level of each skill among linked drones, indexed by SkillDef.index.</summary>
        public readonly int[] skillLevels;

        /// <summary>The drone that currently provides each skill level, indexed by SkillDef.index.</summary>
        public readonly Pawn[] skillSources;

        /// <summary>Shareable traits some drone naturally has, with the drone that provides each.</summary>
        public readonly List<SharedTrait> sharedTraits = new List<SharedTrait>();

        public SlorgCollective(Faction faction)
        {
            this.faction = faction;
            int skillCount = DefDatabase<SkillDef>.DefCount;
            skillLevels = new int[skillCount];
            skillSources = new Pawn[skillCount];
        }

        public int DroneCount => drones.Count;

        public IEnumerable<Pawn> AllDrones
        {
            get
            {
                foreach (Pawn drone in drones)
                {
                    yield return drone;
                }
                foreach (Pawn drone in offMapDrones)
                {
                    yield return drone;
                }
            }
        }

        public int LevelFor(SkillDef skill)
        {
            return skill.index < skillLevels.Length ? skillLevels[skill.index] : 0;
        }

        public Pawn SourceFor(SkillDef skill)
        {
            return skill.index < skillSources.Length ? skillSources[skill.index] : null;
        }

        public bool HasSharedTrait(TraitDef def, int degree)
        {
            for (int i = 0; i < sharedTraits.Count; i++)
            {
                if (sharedTraits[i].def == def && sharedTraits[i].degree == degree)
                {
                    return true;
                }
            }
            return false;
        }

        public struct SharedTrait
        {
            public TraitDef def;
            public int degree;
            public Pawn source;
        }
    }
}

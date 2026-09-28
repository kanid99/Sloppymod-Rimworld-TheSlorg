using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// Every linked drone carries this. Severity is the number of drones in its collective, which drives the stat
    /// stages defined in XML. It also remembers which traits the collective lent this drone so they can be taken back.
    /// </summary>
    public class Hediff_CollectiveLink : HediffWithComps
    {
        private List<TraitDef> grantedTraitDefs = new List<TraitDef>();
        private List<int> grantedTraitDegrees = new List<int>();

        public override bool ShouldRemove => false;

        public bool IsGranted(Trait trait)
        {
            for (int i = 0; i < grantedTraitDefs.Count; i++)
            {
                if (grantedTraitDefs[i] == trait.def && grantedTraitDegrees[i] == trait.Degree)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Makes this drone's lent traits match what the collective currently shares.</summary>
        public void SyncTraits(SlorgCollective collective)
        {
            TraitSet traits = pawn.story?.traits;
            if (traits == null)
            {
                return;
            }

            // Take back traits whose providing drone is gone.
            for (int i = grantedTraitDefs.Count - 1; i >= 0; i--)
            {
                if (!collective.HasSharedTrait(grantedTraitDefs[i], grantedTraitDegrees[i]))
                {
                    RevokeAt(i);
                }
            }

            // Lend traits this drone doesn't have and that don't clash with what it already is.
            foreach (SlorgCollective.SharedTrait shared in collective.sharedTraits)
            {
                if (shared.source == pawn || traits.HasTrait(shared.def) || Conflicts(traits, shared.def))
                {
                    continue;
                }
                traits.GainTrait(new Trait(shared.def, shared.degree, forced: true));
                grantedTraitDefs.Add(shared.def);
                grantedTraitDegrees.Add(shared.degree);
            }
        }

        private static bool Conflicts(TraitSet traits, TraitDef def)
        {
            foreach (Trait existing in traits.allTraits)
            {
                if (existing.def.ConflictsWith(def) || def.ConflictsWith(existing.def))
                {
                    return true;
                }
            }
            return false;
        }

        private void RevokeAt(int index)
        {
            TraitSet traits = pawn.story?.traits;
            Trait trait = traits?.GetTrait(grantedTraitDefs[index], grantedTraitDegrees[index]);
            if (trait != null)
            {
                traits.RemoveTrait(trait);
            }
            grantedTraitDefs.RemoveAt(index);
            grantedTraitDegrees.RemoveAt(index);
        }

        public void RevokeAllTraits()
        {
            for (int i = grantedTraitDefs.Count - 1; i >= 0; i--)
            {
                RevokeAt(i);
            }
        }

        public override void PostRemoved()
        {
            RevokeAllTraits();
            base.PostRemoved();
        }

        public override string LabelInBrackets
        {
            get
            {
                int drones = (int)Severity;
                return drones <= 1 ? "isolated" : drones + " drones";
            }
        }

        public override string TipStringExtra
        {
            get
            {
                StringBuilder sb = new StringBuilder(base.TipStringExtra);
                SlorgCollective collective = GameComponent_SlorgCollective.CollectiveOf(pawn);
                if (collective == null)
                {
                    return sb.ToString();
                }

                sb.AppendLine();
                sb.AppendLine("Collective knowledge:");
                foreach (SkillDef skill in DefDatabase<SkillDef>.AllDefsListForReading)
                {
                    Pawn source = collective.SourceFor(skill);
                    if (source != null)
                    {
                        sb.AppendLine($"  - {skill.LabelCap}: {collective.LevelFor(skill)} (from {source.LabelShortCap})");
                    }
                }

                if (grantedTraitDefs.Count > 0)
                {
                    sb.AppendLine("Traits lent by the collective:");
                    for (int i = 0; i < grantedTraitDefs.Count; i++)
                    {
                        sb.AppendLine("  - " + grantedTraitDefs[i].DataAtDegree(grantedTraitDegrees[i]).GetLabelCapFor(pawn));
                    }
                }
                return sb.ToString().TrimEndNewlines();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref grantedTraitDefs, "grantedTraitDefs", LookMode.Def);
            Scribe_Collections.Look(ref grantedTraitDegrees, "grantedTraitDegrees", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                grantedTraitDefs ??= new List<TraitDef>();
                grantedTraitDegrees ??= new List<int>();
                // Drop anything whose def was removed by a mod change.
                for (int i = grantedTraitDefs.Count - 1; i >= 0; i--)
                {
                    if (grantedTraitDefs[i] == null || i >= grantedTraitDegrees.Count)
                    {
                        grantedTraitDefs.RemoveAt(i);
                        if (i < grantedTraitDegrees.Count)
                        {
                            grantedTraitDegrees.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }
}

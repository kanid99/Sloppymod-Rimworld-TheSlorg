using RimWorld;
using Verse;

namespace TheSlorg
{
    public class CompProperties_AbilityAssimilate : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityAssimilate()
        {
            compClass = typeof(CompAbilityEffect_Assimilate);
        }
    }

    /// <summary>
    /// Injects a downed or imprisoned humanlike with nanoprobes. Assimilation then plays out as a staged infection,
    /// except when the queen does it: her drones rise at once.
    /// </summary>
    public class CompAbilityEffect_Assimilate : CompAbilityEffect
    {
        public static bool CanAssimilate(Pawn caster, Pawn victim, bool throwMessages)
        {
            if (victim == null || !victim.RaceProps.Humanlike || victim.genes == null || victim.Dead)
            {
                return false;
            }
            if (SlorgUtility.HasLinkGene(victim) || victim.health.hediffSet.HasHediff(SlorgDefOf.Slorg_NanoprobeInfection))
            {
                if (throwMessages)
                {
                    Messages.Message($"{victim.LabelShortCap} is already part of the collective, or soon will be.", victim, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            bool helpless = victim.Downed || (victim.IsPrisoner && victim.HostFaction == caster.Faction);
            if (!helpless)
            {
                if (throwMessages)
                {
                    Messages.Message($"{victim.LabelShortCap} must be downed or a prisoner to be assimilated.", victim, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return true;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return CanAssimilate(parent.pawn, target.Pawn, throwMessages) && base.Valid(target, throwMessages);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn victim = target.Pawn;
            Pawn caster = parent.pawn;
            if (victim == null || caster.Faction == null)
            {
                return;
            }

            InfectionUtility.Infect(victim, caster.Faction, caster);
        }
    }
}

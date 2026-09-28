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
    /// Injects a downed or imprisoned humanlike with nanoprobes: they become a Slorg drone and join the caster's collective.
    /// </summary>
    public class CompAbilityEffect_Assimilate : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn victim = target.Pawn;
            if (victim == null || !victim.RaceProps.Humanlike || victim.genes == null)
            {
                return false;
            }
            if (SlorgUtility.IsDrone(victim))
            {
                if (throwMessages)
                {
                    Messages.Message($"{victim.LabelShortCap} is already part of the collective.", victim, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            bool helpless = victim.Downed || (victim.IsPrisoner && victim.HostFaction == parent.pawn.Faction);
            if (!helpless)
            {
                if (throwMessages)
                {
                    Messages.Message($"{victim.LabelShortCap} must be downed or a prisoner to be assimilated.", victim, MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
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

            victim.genes.SetXenotype(SlorgDefOf.Slorg_Drone);

            if (victim.Faction != caster.Faction)
            {
                RecruitUtility.Recruit(victim, caster.Faction, caster);
            }

            Messages.Message($"{victim.LabelShortCap} has been assimilated. Resistance was futile.", victim, MessageTypeDefOf.PositiveEvent);
            GameComponent_SlorgCollective.RefreshNow();
        }
    }
}

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
    /// Injects a downed or imprisoned humanlike with nanoprobes. Assimilation then plays out as an infection.
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

            Hediff infection = HediffMaker.MakeHediff(SlorgDefOf.Slorg_NanoprobeInfection, victim);
            infection.TryGetComp<HediffComp_NanoprobeInfection>().sourceFaction = caster.Faction;
            victim.health.AddHediff(infection);
            // Assimilation tubules inject a far bigger dose.
            float boosted = SlorgImplants.AssimilationStartSeverity(caster);
            if (boosted > infection.Severity)
            {
                infection.Severity = boosted;
            }

            if (victim.Faction != null && victim.Faction.IsPlayer)
            {
                Find.LetterStack.ReceiveLetter("Nanoprobe infection",
                    $"{caster.LabelShortCap} has injected {victim.LabelShortCap} with Slorg nanoprobes.\n\n"
                    + $"In about three days {victim.LabelShortCap} will become a Slorg drone. Normal tending only slows the infection. "
                    + "To purge it you need tends of exceptional quality: glitterworld medicine and a skilled doctor.",
                    LetterDefOf.ThreatBig, victim);
            }
            else
            {
                Messages.Message($"{victim.LabelShortCap} has been injected with nanoprobes. Resistance is futile.", victim, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}

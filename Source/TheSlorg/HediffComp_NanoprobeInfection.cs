using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TheSlorg
{
    public class HediffCompProperties_NanoprobeInfection : HediffCompProperties
    {
        /// <summary>Severity at which stage 2 starts: implants and Slorg genes form, and tending no longer cures.</summary>
        public float stage2Severity = 0.35f;

        /// <summary>During stage 1, a tend at least this good purges the nanoprobes completely.</summary>
        public float stage1CureTendQuality = 0.5f;

        /// <summary>Genes the nanoprobes write into the host at stage 2.</summary>
        public List<GeneDef> stage2Genes = new List<GeneDef>();

        /// <summary>Implants the nanoprobes grow at stage 2.</summary>
        public List<ImplantEntry> stage2Implants = new List<ImplantEntry>();

        public HediffCompProperties_NanoprobeInfection()
        {
            compClass = typeof(HediffComp_NanoprobeInfection);
        }
    }

    /// <summary>
    /// Assimilation in three stages:
    ///  1. Neural takeover (first 1-3 hours): the host fights for the Slorg but has no implants. A decent tend cures it.
    ///  2. Implants forming: implants and Slorg genes appear. Only the purge surgery cures it, and implants stay behind.
    ///  3. Complete (4-6 hours in): the host is a full drone. No cure; only disconnection from the hive frees them.
    /// Bleeding stops for the whole infection (see the hediff stages), so the host doesn't die before the collective gets them.
    /// </summary>
    public class HediffComp_NanoprobeInfection : HediffComp
    {
        public Faction sourceFaction;
        public Faction originalFaction;
        private bool stage2Done;
        private bool queued;

        public HediffCompProperties_NanoprobeInfection Props => (HediffCompProperties_NanoprobeInfection)props;

        public bool InStage2 => parent.Severity >= Props.stage2Severity;

        private bool Held => Pawn.IsPrisonerOfColony || Pawn.IsSlaveOfColony;

        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.CompTended(quality, maxQuality, batchPosition);
            if (InStage2)
            {
                Messages.Message($"{Pawn.LabelShortCap}'s nanoprobes have started building implants. Tending can't stop them now; only the purge surgery can.",
                    Pawn, MessageTypeDefOf.NegativeEvent, historical: false);
                return;
            }
            if (quality >= Props.stage1CureTendQuality)
            {
                GameComponent_SlorgCollective.Instance?.QueueInfectionAction(this, InfectionAction.Cure);
            }
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (queued)
            {
                return;
            }
            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            float next = parent.Severity + severityAdjustment;
            // Changing the pawn's hediffs here would break the health tracker's loop, so the work is queued.
            if (next >= parent.def.maxSeverity - 0.001f)
            {
                severityAdjustment = 0f;
                queued = true;
                collective?.QueueInfectionAction(this, InfectionAction.Complete);
            }
            else if (!stage2Done && next >= Props.stage2Severity)
            {
                queued = true;
                collective?.QueueInfectionAction(this, InfectionAction.Stage2);
            }
        }

        public void Run(InfectionAction action)
        {
            queued = false;
            if (Pawn == null || Pawn.Dead || !Pawn.health.hediffSet.hediffs.Contains(parent))
            {
                return;
            }
            switch (action)
            {
                case InfectionAction.Stage2:
                    if (!stage2Done)
                    {
                        EnterStage2();
                    }
                    break;
                case InfectionAction.Cure:
                    Cure(announce: true);
                    break;
                case InfectionAction.Complete:
                    Complete();
                    break;
            }
        }

        /// <summary>Stage 1 starts: the host turns on its own side, unless it's being held captive.</summary>
        public void OnInfected(Pawn caster)
        {
            Pawn pawn = Pawn;
            originalFaction = pawn.Faction;
            if (Held || sourceFaction == null || pawn.Faction == sourceFaction)
            {
                return;
            }
            pawn.GetLord()?.RemovePawn(pawn);
            pawn.SetFaction(sourceFaction);
            if (!pawn.Spawned)
            {
                return;
            }
            Lord lord = caster?.GetLord();
            if (lord != null && lord.faction == sourceFaction)
            {
                lord.AddPawn(pawn);
            }
            else if (pawn.Map.IsPlayerHome)
            {
                SlorgUtility.TurnOnColony(new List<Pawn> { pawn }, sourceFaction, pawn.Map);
            }
        }

        /// <summary>Debug and tests: jump straight to stage 2.</summary>
        public void ForceStage2()
        {
            if (!stage2Done)
            {
                parent.Severity = UnityEngine.Mathf.Max(parent.Severity, Props.stage2Severity);
                EnterStage2();
            }
        }

        private void EnterStage2()
        {
            stage2Done = true;
            Pawn pawn = Pawn;
            if (pawn.genes != null)
            {
                foreach (GeneDef gene in Props.stage2Genes)
                {
                    if (!pawn.genes.HasActiveGene(gene))
                    {
                        pawn.genes.AddGene(gene, xenogene: true);
                    }
                }
            }
            foreach (ImplantEntry entry in Props.stage2Implants)
            {
                SlorgImplants.Install(pawn, entry.hediff, entry.part);
            }
            SlorgUtility.MakeHairless(pawn);
            Messages.Message($"Implants are forming in {pawn.LabelShortCap}. Only the purge surgery can stop the nanoprobes now.",
                pawn, MessageTypeDefOf.ThreatSmall);
            GameComponent_SlorgCollective.RefreshNow();
        }

        /// <summary>Purges the infection. Slorg genes are stripped; implants stay until cut out.</summary>
        public void Cure(bool announce)
        {
            Pawn pawn = Pawn;
            pawn.health.RemoveHediff(parent);
            if (pawn.genes != null)
            {
                foreach (Gene gene in pawn.genes.Xenogenes.ToList())
                {
                    if (gene.def.defName.StartsWith("Slorg_"))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
            }
            RestoreFaction(pawn);
            if (announce)
            {
                string implants = SlorgImplants.HasAnyImplant(pawn) ? " The implants they grew are still there and will have to be cut out." : "";
                Messages.Message($"{pawn.LabelShortCap}'s nanoprobe infection has been completely purged.{implants}", pawn, MessageTypeDefOf.PositiveEvent);
            }
            // ...or so it seems. Some nanoprobes go dormant and leave a hidden sleeper agent.
            if (pawn.Faction != null && pawn.Faction.IsPlayer && Rand.Chance(SlorgDefOf.Slorg_Collective.dormantChanceOnPurge))
            {
                HediffComp_DormantNanoprobes.Implant(pawn, sourceFaction);
            }
            GameComponent_SlorgCollective.RefreshNow();
        }

        private void RestoreFaction(Pawn pawn)
        {
            if (originalFaction == null || pawn.Faction == originalFaction)
            {
                return;
            }
            if (pawn.IsPrisoner)
            {
                if (!originalFaction.IsPlayer)
                {
                    return;
                }
                // Captured while under Slorg control: they come straight back to the colony.
                pawn.guest.SetGuestStatus(null);
            }
            pawn.GetLord()?.RemovePawn(pawn);
            pawn.SetFaction(originalFaction);
        }

        /// <summary>Stage 3: a full drone. Also used directly by the queen's instant assimilation.</summary>
        public void Complete()
        {
            Pawn pawn = Pawn;
            pawn.health.RemoveHediff(parent);
            if (pawn.Dead)
            {
                return;
            }

            Faction faction = SlorgUtility.ResolveSlorgFaction(sourceFaction, pawn.Tile);
            if (faction == null)
            {
                SlorgUtility.MakeDisconnected(pawn);
                Messages.Message($"The nanoprobes have finished with {pawn.LabelShortCap}, but there is no collective left to answer. They are a disconnected drone.",
                    pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }

            bool held = Held;
            if (held && Rand.Chance(SlorgDefOf.Slorg_Collective.captiveSleeperChance))
            {
                // The nanoprobes hide instead: strip what they built and wait to be recruited into the colony.
                foreach (Gene gene in pawn.genes.Xenogenes.ToList())
                {
                    if (gene.def.defName.StartsWith("Slorg_"))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
                foreach (Hediff implant in pawn.health.hediffSet.hediffs.Where(SlorgImplants.IsSlorgImplant).ToList())
                {
                    pawn.health.RemoveHediff(implant);
                }
                HediffComp_DormantNanoprobes.Implant(pawn, faction);
                Messages.Message($"{pawn.LabelShortCap}'s nanoprobe infection has run its course. The implants have dissolved, and {pawn.LabelShortCap} seems... unchanged.",
                    pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }

            bool wasPlayers = (originalFaction != null && originalFaction.IsPlayer) || (pawn.Faction != null && pawn.Faction.IsPlayer);
            SlorgUtility.MakeFullDrone(pawn);

            if (held || (pawn.Spawned && pawn.Map.IsPlayerHome))
            {
                if (held)
                {
                    pawn.guest?.SetGuestStatus(null);
                }
                if (pawn.Spawned)
                {
                    SlorgUtility.TurnOnColony(new List<Pawn> { pawn }, faction, pawn.Map);
                }
                else if (pawn.Faction != faction)
                {
                    pawn.SetFaction(faction);
                }
            }
            else
            {
                if (pawn.Faction != faction)
                {
                    pawn.SetFaction(faction);
                }
                if (pawn.Spawned && pawn.GetLord() == null)
                {
                    LordMaker.MakeNewLord(faction, new LordJob_ExitMapBest(Verse.AI.LocomotionUrgency.Jog, canDig: true, canDefendSelf: true),
                        pawn.Map, new[] { pawn });
                }
            }

            if (wasPlayers || held)
            {
                Find.LetterStack.ReceiveLetter("Assimilated",
                    $"The nanoprobes have finished their work. {pawn.LabelShortCap} is now a Slorg drone, and can't be cured.\n\n"
                    + "Only cutting them off from the hive (killing or capturing their queen, or destroying the queen core) can free them now.",
                    LetterDefOf.ThreatBig, pawn);
            }
            GameComponent_SlorgCollective.RefreshNow();
        }

        public override string CompTipStringExtra
        {
            get
            {
                return InStage2
                    ? "Implants are forming. Only the Purge nanoprobes surgery can cure this now, and the implants will stay."
                    : $"Any tend of at least {Props.stage1CureTendQuality.ToStringPercent()} quality purges the nanoprobes.";
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref sourceFaction, "sourceFaction");
            Scribe_References.Look(ref originalFaction, "originalFaction");
            Scribe_Values.Look(ref stage2Done, "stage2Done");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // The action queue isn't saved; the next tick re-queues anything pending.
                queued = false;
            }
        }
    }

    public enum InfectionAction
    {
        Stage2,
        Cure,
        Complete
    }

    public static class InfectionUtility
    {
        /// <summary>Injects nanoprobes. The queen's injection finishes the job on the spot.</summary>
        public static void Infect(Pawn victim, Faction source, Pawn caster)
        {
            Hediff infection = HediffMaker.MakeHediff(SlorgDefOf.Slorg_NanoprobeInfection, victim);
            HediffComp_NanoprobeInfection comp = infection.TryGetComp<HediffComp_NanoprobeInfection>();
            comp.sourceFaction = source;
            victim.health.AddHediff(infection);

            // Assimilation tubules inject a far bigger dose.
            float boosted = caster != null ? SlorgImplants.AssimilationStartSeverity(caster) : -1f;
            if (boosted > infection.Severity)
            {
                infection.Severity = boosted;
            }

            bool wasPlayers = victim.Faction != null && victim.Faction.IsPlayer;
            comp.OnInfected(caster);

            if (caster != null && SlorgUtility.IsQueen(caster))
            {
                comp.Complete();
                return;
            }

            if (wasPlayers)
            {
                Find.LetterStack.ReceiveLetter("Nanoprobe infection",
                    $"{(caster != null ? caster.LabelShortCap : "A drone")} has injected {victim.LabelShortCap} with Slorg nanoprobes. {victim.LabelShortCap} now serves the collective.\n\n"
                    + "- For the first 1-3 hours, down them and tend them: any decent tend purges the nanoprobes and brings them back.\n"
                    + "- After that, implants start forming. Only the Purge nanoprobes surgery (glitterworld medicine) can save them, and the implants must be cut out afterwards.\n"
                    + "- After 4-6 hours, assimilation is complete and can't be undone.",
                    LetterDefOf.ThreatBig, victim);
            }
            else
            {
                Messages.Message($"{victim.LabelShortCap} has been injected with nanoprobes. Resistance is futile.", victim, MessageTypeDefOf.NeutralEvent);
            }
        }
    }

    /// <summary>Stage 1 or 2: purges the nanoprobes. Implants already grown stay behind.</summary>
    public class Recipe_PurgeNanoprobes : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn pawn && pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_NanoprobeInfection) && base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_NanoprobeInfection)?.TryGetComp<HediffComp_NanoprobeInfection>()?.Cure(announce: true);
        }
    }
}

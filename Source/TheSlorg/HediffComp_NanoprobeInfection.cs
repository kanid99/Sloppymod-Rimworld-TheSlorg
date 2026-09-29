using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace TheSlorg
{
    public class HediffCompProperties_NanoprobeInfection : HediffCompProperties
    {
        /// <summary>A tend at or above this quality purges nanoprobes. Normal medicine caps at 100%, so this needs glitterworld medicine and a good doctor.</summary>
        public float purgeTendQuality = 1.05f;

        /// <summary>How much severity a purging tend removes.</summary>
        public float purgeAmount = 0.35f;

        public HediffCompProperties_NanoprobeInfection()
        {
            compClass = typeof(HediffComp_NanoprobeInfection);
        }
    }

    /// <summary>
    /// Assimilation as an infection. Severity climbs towards 1; ordinary tending only slows it, an exceptional tend purges
    /// some of it. At full severity the pawn becomes a drone of the faction that infected them.
    /// </summary>
    public class HediffComp_NanoprobeInfection : HediffComp
    {
        public Faction sourceFaction;

        public HediffCompProperties_NanoprobeInfection Props => (HediffCompProperties_NanoprobeInfection)props;

        public override void CompTended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.CompTended(quality, maxQuality, batchPosition);
            if (quality < Props.purgeTendQuality)
            {
                return;
            }
            parent.Severity -= Props.purgeAmount;
            if (parent.Severity <= 0.01f)
            {
                // Zero severity makes the health tracker remove the hediff on its own.
                parent.Severity = 0f;
                Messages.Message($"{Pawn.LabelShortCap}'s nanoprobe infection has been completely purged.", Pawn, MessageTypeDefOf.PositiveEvent);
                // ...or so it seems. Some nanoprobes go dormant and turn the pawn into a hidden sleeper agent.
                if (Pawn.Faction != null && Pawn.Faction.IsPlayer && Rand.Chance(SlorgDefOf.Slorg_Collective.dormantChanceOnPurge))
                {
                    HediffComp_DormantNanoprobes.Implant(Pawn, sourceFaction);
                }
            }
            else
            {
                Messages.Message($"The tend purged part of {Pawn.LabelShortCap}'s nanoprobe infection.", Pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        private bool queuedForCompletion;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!queuedForCompletion && parent.Severity + severityAdjustment >= parent.def.maxSeverity - 0.001f)
            {
                // Converting here would change the pawn's hediffs while the health tracker is iterating them.
                queuedForCompletion = true;
                GameComponent_SlorgCollective.Instance?.QueueAssimilation(this);
            }
        }

        /// <summary>Called by the collective outside the health tick.</summary>
        public void Complete()
        {
            Pawn pawn = Pawn;
            if (pawn == null || !pawn.health.hediffSet.hediffs.Contains(parent))
            {
                return;
            }
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

            bool held = pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
            if (held && Rand.Chance(SlorgDefOf.Slorg_Collective.captiveSleeperChance))
            {
                // The nanoprobes hide instead: a sleeper waiting to be recruited into the colony.
                HediffComp_DormantNanoprobes.Implant(pawn, faction);
                Messages.Message($"{pawn.LabelShortCap}'s nanoprobe infection has run its course. {pawn.LabelShortCap} seems... unchanged.",
                    pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }

            bool wasPlayers = pawn.Faction != null && pawn.Faction.IsPlayer && !held;
            SlorgUtility.MakeThrall(pawn);

            if (held)
            {
                // Breaks out as a thrall and turns on the colony.
                pawn.guest?.SetGuestStatus(null);
                if (pawn.Spawned)
                {
                    SlorgUtility.TurnOnColony(new List<Pawn> { pawn }, faction, pawn.Map);
                }
                else if (pawn.Faction != faction)
                {
                    pawn.SetFaction(faction);
                }
                Find.LetterStack.ReceiveLetter("Captive assimilated",
                    $"The nanoprobes have finished with {pawn.LabelShortCap}. They are a Slorg thrall now, and they have turned on their captors.",
                    LetterDefOf.ThreatBig, pawn);
            }
            else if (pawn.Spawned && pawn.Map.IsPlayerHome)
            {
                // Rescued but not saved: the new thrall turns on the colony and tries to assimilate it from inside.
                SlorgUtility.TurnOnColony(new List<Pawn> { pawn }, faction, pawn.Map);
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

            if (wasPlayers)
            {
                Find.LetterStack.ReceiveLetter("Assimilated",
                    $"{pawn.LabelShortCap} could not be saved. The nanoprobes have finished their work, and {pawn.LabelShortCap} is now a Slorg thrall, "
                    + "with the collective's genes if not yet its implants.\n\n"
                    + (pawn.Spawned && pawn.Map.IsPlayerHome
                        ? $"{pawn.LabelShortCap} has turned on the colony and will try to down and inject anyone they can."
                        : "Everything they knew now belongs to the collective, for as long as they live."),
                    LetterDefOf.ThreatBig, pawn);
            }
            GameComponent_SlorgCollective.RefreshNow();
        }

        public override string CompTipStringExtra
        {
            get
            {
                return $"Only a tend of at least {Props.purgeTendQuality.ToStringPercent()} quality purges nanoprobes. That needs glitterworld medicine and a skilled doctor.";
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref sourceFaction, "sourceFaction");
            Scribe_Values.Look(ref queuedForCompletion, "queuedForCompletion");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && queuedForCompletion)
            {
                // The queue itself isn't saved; let the next tick re-queue it.
                queuedForCompletion = false;
            }
        }
    }
}

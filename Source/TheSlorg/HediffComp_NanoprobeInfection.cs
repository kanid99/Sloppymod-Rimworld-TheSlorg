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

            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            bool collectiveGone = sourceFaction == null
                || sourceFaction.defeated
                || (collective != null && collective.SurfaceControlLost(sourceFaction) && !SlorgUtility.IsSpace(pawn.Tile));
            if (collectiveGone || pawn.genes == null)
            {
                SlorgUtility.MakeDisconnected(pawn);
                Messages.Message($"The nanoprobes have rebuilt {pawn.LabelShortCap}, but there is no collective left to answer. They are a disconnected drone.",
                    pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }

            bool wasPlayers = pawn.Faction != null && pawn.Faction.IsPlayer;
            pawn.genes.SetXenotype(SlorgDefOf.Slorg_Drone);
            if (pawn.Faction != sourceFaction)
            {
                pawn.SetFaction(sourceFaction);
            }

            if (pawn.Spawned && pawn.GetLord() == null)
            {
                LordMaker.MakeNewLord(sourceFaction, new LordJob_ExitMapBest(Verse.AI.LocomotionUrgency.Jog, canDig: true, canDefendSelf: true),
                    pawn.Map, new[] { pawn });
            }

            if (wasPlayers)
            {
                Find.LetterStack.ReceiveLetter("Assimilated",
                    $"{pawn.LabelShortCap} could not be saved. The nanoprobes have finished their work, and {pawn.LabelShortCap} is now a Slorg drone.\n\n"
                    + "Everything they knew now belongs to the collective, for as long as they live.",
                    LetterDefOf.NegativeEvent, pawn);
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

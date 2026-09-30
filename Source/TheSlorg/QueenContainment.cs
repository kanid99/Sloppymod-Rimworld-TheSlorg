using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TheSlorg
{
    /// <summary>
    /// A powered containment platform for a captive queen, in the spirit of an Anomaly holding platform. While powered,
    /// she is held in neural stasis: fully suppressed, needing no wardens and not ageing or starving. If the power fails
    /// her suppression drains, and sooner or later she breaks free.
    /// </summary>
    public class Building_QueenContainment : Building, IThingHolder
    {
        private ThingOwner<Pawn> innerContainer;
        private int unpoweredTicks;

        private const int CheckInterval = 250;

        public Building_QueenContainment()
        {
            innerContainer = new ThingOwner<Pawn>(this, oneStackOnly: true);
        }

        public Pawn HeldQueen => innerContainer.Count > 0 ? innerContainer[0] : null;

        public bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public static Building_QueenContainment HolderOf(Pawn pawn)
        {
            return pawn?.ParentHolder as Building_QueenContainment;
        }

        public static bool IsContained(Pawn pawn)
        {
            return HolderOf(pawn) != null;
        }

        /// <summary>A queen this platform can take: downed, and either already our prisoner or still an enemy.</summary>
        public static bool IsCandidate(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead && pawn.Downed && SlorgUtility.IsQueen(pawn)
                && (pawn.IsPrisonerOfColony || (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer)));
        }

        public void Contain(Pawn queen, Pawn carrier)
        {
            if (HeldQueen != null)
            {
                return;
            }
            if (!queen.IsPrisonerOfColony)
            {
                queen.guest?.CapturedBy(Faction.OfPlayer, carrier);
            }
            ThingOwner from = queen.holdingOwner;
            if (from == null || !from.TryTransferToContainer(queen, innerContainer))
            {
                return;
            }
            unpoweredTicks = 0;
            QueenSuppression.Maintain(queen);
            HoldSuppressed(queen);
            Messages.Message($"{queen.LabelShortCap} is locked in neural stasis. While the platform has power she is fully suppressed and needs no warden.",
                this, MessageTypeDefOf.PositiveEvent);
            GameComponent_SlorgCollective.RefreshNow();
        }

        private static void HoldSuppressed(Pawn queen)
        {
            Hediff suppression = QueenSuppression.HediffOf(queen);
            if (suppression != null)
            {
                suppression.Severity = 1f;
            }
        }

        /// <summary>Lets her out as an ordinary prisoner, or as an escapee when she breaks free.</summary>
        public Pawn Release()
        {
            Pawn queen = HeldQueen;
            if (queen == null || !innerContainer.TryDrop(queen, Position, Map, ThingPlaceMode.Near, out Thing dropped))
            {
                return null;
            }
            unpoweredTicks = 0;
            return dropped as Pawn;
        }

        protected override void Tick()
        {
            base.Tick();
            Pawn queen = HeldQueen;
            if (queen == null || !this.IsHashIntervalTick(CheckInterval))
            {
                return;
            }
            if (Powered)
            {
                unpoweredTicks = 0;
                HoldSuppressed(queen);
                return;
            }

            if (unpoweredTicks == 0)
            {
                Messages.Message($"The containment platform holding {queen.LabelShortCap} has lost power. Her suppression is slipping.",
                    this, MessageTypeDefOf.ThreatBig);
            }
            unpoweredTicks += CheckInterval;
            Hediff suppression = QueenSuppression.HediffOf(queen);
            if (suppression != null)
            {
                suppression.Severity = Mathf.Max(0.001f, suppression.Severity - SlorgDefOf.Slorg_Collective.containmentUnpoweredDrainPerDay * CheckInterval / GenDate.TicksPerDay);
            }

            // After an hour without power she starts testing the restraints. The better suppressed, the longer they hold.
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            if (unpoweredTicks >= GenDate.TicksPerHour)
            {
                float mtbDays = Mathf.Lerp(tuning.containmentBreakoutMtbDaysAtZero, tuning.containmentBreakoutMtbDaysAtFull, QueenSuppression.Level(queen));
                if (Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, CheckInterval))
                {
                    BreakOut();
                }
            }
        }

        private void BreakOut()
        {
            Pawn queen = Release();
            if (queen == null)
            {
                return;
            }
            // Stasis has had time to knit her together.
            foreach (Hediff hediff in queen.health.hediffSet.hediffs.ToList())
            {
                if ((hediff is Hediff_Injury injury && !injury.IsPermanent()) || hediff.def == HediffDefOf.Anesthetic)
                {
                    queen.health.RemoveHediff(hediff);
                }
            }
            PrisonBreakUtility.StartPrisonBreak(queen);
            Find.LetterStack.ReceiveLetter("The queen has broken containment",
                $"Without power, the stasis field around {queen.LabelShortCap} collapsed and she has torn herself free. "
                + "If she gets away, every drone she called will turn on the colony.",
                LetterDefOf.ThreatBig, queen);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(selPawn))
            {
                yield return option;
            }
            if (HeldQueen != null)
            {
                yield break;
            }
            foreach (Pawn queen in Map.mapPawns.AllPawnsSpawned.Where(IsCandidate).ToList())
            {
                string label = $"Contain {queen.LabelShort} on the platform";
                if (!Powered)
                {
                    yield return new FloatMenuOption(label + " (no power)", null);
                }
                else if (!selPawn.CanReserveAndReach(queen, PathEndMode.ClosestTouch, Danger.Deadly) || !selPawn.CanReserveAndReach(this, PathEndMode.Touch, Danger.Deadly))
                {
                    yield return new FloatMenuOption(label + " (can't reach or reserve)", null);
                }
                else
                {
                    Pawn target = queen;
                    yield return new FloatMenuOption(label, () =>
                    {
                        Job job = JobMaker.MakeJob(SlorgDefOf.Slorg_ContainQueen, target, this);
                        job.count = 1;
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            Pawn queen = HeldQueen;
            if (queen == null)
            {
                yield break;
            }
            if (CaptiveQueen.IsCaptiveQueen(queen))
            {
                yield return CaptiveQueen.SummonCommand(queen);
            }
            yield return new Command_Action
            {
                defaultLabel = "Release from stasis",
                defaultDesc = $"Take {queen.LabelShortCap} off the platform. She stays your prisoner, but wardens will have to keep her suppressed from now on.",
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Open"),
                action = () => Release()
            };
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            Pawn queen = HeldQueen;
            string held = queen == null
                ? "Empty. Right-click it with a colonist selected to contain a downed queen."
                : $"Holding: {queen.LabelShortCap} (suppression {QueenSuppression.Level(queen).ToStringPercent()})"
                  + (Powered ? "" : $"\nNO POWER for {unpoweredTicks.ToStringTicksToPeriod()}. She will break free.");
            return text.NullOrEmpty() ? held : text + "\n" + held;
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            Pawn queen = HeldQueen;
            if (queen != null)
            {
                Vector3 loc = drawLoc;
                loc.y = AltitudeLayer.Pawn.AltitudeFor();
                queen.Drawer.renderer.DynamicDrawPhaseAt(phase, loc, Rot4.South, neverAimWeapon: true);
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (HeldQueen != null && Spawned)
            {
                Pawn queen = Release();
                if (queen != null)
                {
                    Messages.Message($"{queen.LabelShortCap} has been released from the destroyed containment platform.", queen, MessageTypeDefOf.ThreatBig);
                }
            }
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref unpoweredTicks, "unpoweredTicks");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                innerContainer = new ThingOwner<Pawn>(this, oneStackOnly: true);
            }
        }
    }

    /// <summary>Carry a downed queen to a containment platform and lock her in.</summary>
    public class JobDriver_ContainQueen : JobDriver
    {
        private Pawn Queen => (Pawn)job.targetA.Thing;

        private Building_QueenContainment Platform => (Building_QueenContainment)job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => Platform.HeldQueen != null || !Platform.Powered);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A)
                .FailOn(() => !Building_QueenContainment.IsCandidate(Queen));
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            Toil contain = ToilMaker.MakeToil("ContainQueen");
            contain.initAction = () => Platform.Contain(Queen, pawn);
            contain.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return contain;
        }
    }
}

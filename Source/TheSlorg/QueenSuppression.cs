using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheSlorg
{
    /// <summary>
    /// A captive queen has to be kept suppressed. The suppression hediff's severity (0-1) drains over time and wardens
    /// push it back up. Low suppression lets her bound drones seed hidden sleepers through the colony.
    /// </summary>
    public static class QueenSuppression
    {
        public static Hediff HediffOf(Pawn queen)
        {
            return queen?.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_QueenSuppression);
        }

        public static float Level(Pawn queen)
        {
            return HediffOf(queen)?.Severity ?? 0f;
        }

        /// <summary>Adds the meter to newly captured queens and removes it from queens who are no longer captive.</summary>
        public static void Maintain(Pawn pawn)
        {
            Hediff hediff = HediffOf(pawn);
            bool captive = CaptiveQueen.IsCaptiveQueen(pawn);
            if (captive && hediff == null)
            {
                hediff = HediffMaker.MakeHediff(SlorgDefOf.Slorg_QueenSuppression, pawn);
                hediff.Severity = SlorgDefOf.Slorg_Collective.queenSuppressionStart;
                pawn.health.AddHediff(hediff);
            }
            else if (!captive && hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void Suppress(Pawn queen, Pawn warden)
        {
            Hediff hediff = HediffOf(queen);
            if (hediff == null)
            {
                return;
            }
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            int social = warden.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            hediff.Severity = UnityEngine.Mathf.Min(1f, hediff.Severity + tuning.queenSuppressionPerSession + social * tuning.queenSuppressionPerSocialLevel);
            warden.skills?.Learn(SkillDefOf.Social, 150f);
        }

        /// <summary>
        /// Runs each collective refresh on colony maps. A poorly suppressed queen's drones quietly implant dormant
        /// nanoprobes into colonists: invisible, silent, and only caught by a nanoprobe scan.
        /// </summary>
        public static void TickMap(Map map)
        {
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            foreach (Pawn queen in map.mapPawns.PrisonersOfColony.ToList())
            {
                if (!CaptiveQueen.IsCaptiveQueen(queen))
                {
                    continue;
                }
                float level = Level(queen);
                if (level >= tuning.queenSuppressionDangerLevel)
                {
                    continue;
                }
                List<Pawn> drones = CaptiveQueen.BoundTo(queen).Where(d => d.Spawned && d.Map == map && !d.Downed).ToList();
                if (drones.Count == 0)
                {
                    continue;
                }
                // The less suppressed she is, the more often it happens.
                float fraction = 1f - level / tuning.queenSuppressionDangerLevel;
                float mtbDays = UnityEngine.Mathf.Lerp(tuning.queenSeedingMtbDaysAtDanger, tuning.queenSeedingMtbDaysAtZero, fraction);
                if (!Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, tuning.refreshIntervalTicks))
                {
                    continue;
                }
                Pawn victim = map.mapPawns.FreeColonistsSpawned
                    .Where(p => !CaptiveQueen.IsBound(p) && SleeperUtility.IsValidVictim(p))
                    .RandomElementWithFallback();
                if (victim != null)
                {
                    HediffComp_DormantNanoprobes.Implant(victim, queen.Faction);
                }
            }
        }
    }

    /// <summary>Wardens keep a captive queen suppressed.</summary>
    public class WorkGiver_Warden_SuppressQueen : WorkGiver_Warden
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ShouldTakeCareOfPrisoner(pawn, t, forced) || !(t is Pawn queen) || !CaptiveQueen.IsCaptiveQueen(queen))
            {
                return null;
            }
            float threshold = forced ? 0.99f : SlorgDefOf.Slorg_Collective.queenSuppressionWorkBelow;
            if (QueenSuppression.Level(queen) >= threshold || !pawn.CanReserve(queen, 1, -1, null, forced))
            {
                return null;
            }
            return JobMaker.MakeJob(SlorgDefOf.Slorg_SuppressQueen, queen);
        }
    }

    public class JobDriver_SuppressQueen : JobDriver
    {
        private Pawn Queen => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !CaptiveQueen.IsCaptiveQueen(Queen));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil work = Toils_General.Wait(400, TargetIndex.A);
            work.WithProgressBarToilDelay(TargetIndex.A);
            yield return work;
            Toil finish = ToilMaker.MakeToil("SuppressQueen");
            finish.initAction = () => QueenSuppression.Suppress(Queen, pawn);
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}

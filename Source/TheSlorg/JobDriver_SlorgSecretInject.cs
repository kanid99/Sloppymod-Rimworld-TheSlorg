using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheSlorg
{
    /// <summary>A sleeper agent quietly injects another colonist. Shows up as "checking on X".</summary>
    public class JobDriver_SlorgSecretInject : JobDriver
    {
        private Pawn Victim => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.Drafted);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(120, TargetIndex.A);

            Toil inject = ToilMaker.MakeToil("SlorgSecretInject");
            inject.initAction = () =>
            {
                Pawn victim = Victim;
                HediffComp_DormantNanoprobes own = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_DormantNanoprobes)?.TryGetComp<HediffComp_DormantNanoprobes>();
                if (own == null || victim == null || victim.Dead || !SleeperUtility.IsValidVictim(victim))
                {
                    return;
                }
                HediffComp_DormantNanoprobes.Implant(victim, own.sourceFaction);
                SleeperUtility.MaybeWitnessed(pawn, victim);
            };
            inject.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return inject;
        }
    }
}

using RimWorld;
using Verse;
using Verse.AI;

namespace TheSlorg
{
    /// <summary>
    /// Slorg weapons are built to take people, not kill them. Neural shock builds up in the target (see the
    /// Slorg_NeuralDisruption hediff) until they collapse, then wears off. It does no physical damage.
    /// </summary>
    public class DamageWorker_SlorgNeural : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();
            if (!(victim is Pawn pawn) || pawn.Dead || def.hediff == null || pawn.health == null)
            {
                return result;
            }
            // Machines and Slorg shrug it off.
            if (!pawn.RaceProps.IsFlesh || SlorgUtility.HasLinkGene(pawn))
            {
                return result;
            }
            Hediff shock = pawn.health.hediffSet.GetFirstHediffOfDef(def.hediff);
            if (shock == null)
            {
                shock = HediffMaker.MakeHediff(def.hediff, pawn);
                shock.Severity = dinfo.Amount;
                pawn.health.AddHediff(shock, null, dinfo);
            }
            else
            {
                shock.Severity += dinfo.Amount;
            }
            result.AddHediff(shock);
            return result;
        }
    }

    /// <summary>Drones with a beam emitter fire it at the nearest standing enemy they can see.</summary>
    public class JobGiver_SlorgBeam : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!SlorgUtility.IsLinkedDrone(pawn) || pawn.Downed || !pawn.Spawned || pawn.abilities == null)
            {
                return null;
            }
            Ability beam = pawn.abilities.GetAbility(SlorgDefOf.Slorg_CuttingBeam);
            if (beam == null || !beam.CanCast)
            {
                return null;
            }
            float range = beam.verb?.verbProps.range ?? 25f;
            Pawn best = null;
            float bestDist = range * range;
            foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (other.Downed || other.Dead || !pawn.HostileTo(other))
                {
                    continue;
                }
                float dist = pawn.Position.DistanceToSquared(other.Position);
                if (dist >= bestDist || !GenSight.LineOfSight(pawn.Position, other.Position, pawn.Map))
                {
                    continue;
                }
                best = other;
                bestDist = dist;
            }
            return best == null ? null : beam.GetJob(best, best);
        }
    }
}

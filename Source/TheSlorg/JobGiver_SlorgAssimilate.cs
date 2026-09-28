using RimWorld;
using Verse;
using Verse.AI;

namespace TheSlorg
{
    /// <summary>Enemy drones go for downed enemies nearby and inject them.</summary>
    public class JobGiver_SlorgAssimilate : ThinkNode_JobGiver
    {
        private const float MaxDistance = 40f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!SlorgUtility.IsLinkedDrone(pawn) || pawn.Downed || !pawn.Spawned || pawn.abilities == null)
            {
                return null;
            }
            Ability ability = pawn.abilities.GetAbility(SlorgDefOf.Slorg_Assimilate);
            if (ability == null || !ability.CanCast)
            {
                return null;
            }

            Pawn best = null;
            float bestDist = MaxDistance * MaxDistance;
            foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == pawn || !other.Downed || !pawn.HostileTo(other))
                {
                    continue;
                }
                float dist = pawn.Position.DistanceToSquared(other.Position);
                if (dist >= bestDist
                    || !CompAbilityEffect_Assimilate.CanAssimilate(pawn, other, throwMessages: false)
                    || !pawn.CanReserveAndReach(other, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }
                best = other;
                bestDist = dist;
            }
            return best == null ? null : ability.GetJob(best, best);
        }
    }
}

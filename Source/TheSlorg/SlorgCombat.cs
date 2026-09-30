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

    /// <summary>
    /// Drones fire their arm weapons at the nearest enemy in sight. The disruptor (non-lethal) is the default; the plasma
    /// lance (lethal) is used against machines and turrets, or when the drone or the whole assault is in trouble.
    /// </summary>
    public class JobGiver_SlorgBeam : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!SlorgUtility.IsLinkedDrone(pawn) || pawn.Downed || !pawn.Spawned || pawn.abilities == null)
            {
                return null;
            }
            Ability disruptor = Ready(pawn, SlorgDefOf.Slorg_CuttingBeam);
            Ability plasma = Ready(pawn, SlorgDefOf.Slorg_PlasmaLance);
            if (disruptor == null && plasma == null)
            {
                return null;
            }
            bool desperate = plasma != null && (pawn.health.summaryHealth.SummaryHealthPercent < 0.5f || AssaultFailing(pawn));

            Thing best = null;
            Ability bestAbility = null;
            float bestDist = float.MaxValue;
            foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (other.Downed || other.Dead || !pawn.HostileTo(other))
                {
                    continue;
                }
                bool machine = !other.RaceProps.IsFlesh;
                Ability use = machine || desperate ? plasma ?? disruptor : disruptor;
                Consider(pawn, other, use, ref best, ref bestAbility, ref bestDist);
            }
            if (plasma != null)
            {
                foreach (Building turret in pawn.Map.listerBuildings.allBuildingsColonist)
                {
                    if (turret is Building_Turret && pawn.HostileTo(turret))
                    {
                        Consider(pawn, turret, plasma, ref best, ref bestAbility, ref bestDist);
                    }
                }
            }
            return best == null ? null : bestAbility.GetJob(best, best);
        }

        private static Ability Ready(Pawn pawn, AbilityDef def)
        {
            Ability ability = pawn.abilities.GetAbility(def);
            return ability != null && ability.CanCast ? ability : null;
        }

        private static void Consider(Pawn pawn, Thing target, Ability ability, ref Thing best, ref Ability bestAbility, ref float bestDist)
        {
            if (ability == null)
            {
                return;
            }
            float range = ability.verb?.verbProps.range ?? 25f;
            float dist = pawn.Position.DistanceToSquared(target.Position);
            if (dist > range * range || dist >= bestDist || !GenSight.LineOfSight(pawn.Position, target.Position, pawn.Map))
            {
                return;
            }
            best = target;
            bestAbility = ability;
            bestDist = dist;
        }

        /// <summary>Half or more of the Slorg on this map are down: stop taking prisoners.</summary>
        private static bool AssaultFailing(Pawn pawn)
        {
            int total = 0;
            int down = 0;
            foreach (Pawn other in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
            {
                if (!SlorgUtility.HasLinkGene(other))
                {
                    continue;
                }
                total++;
                if (other.Downed)
                {
                    down++;
                }
            }
            return total >= 2 && down * 2 >= total;
        }
    }
}

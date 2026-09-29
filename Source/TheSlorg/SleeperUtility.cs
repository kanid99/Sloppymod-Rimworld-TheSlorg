using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TheSlorg
{
    /// <summary>Sleeper agents: dormant nanoprobes spreading quietly through a colony until they rise up.</summary>
    public static class SleeperUtility
    {
        private const float ObservationRadius = 12f;

        public static HediffComp_DormantNanoprobes SleeperComp(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(SlorgDefOf.Slorg_DormantNanoprobes)?.TryGetComp<HediffComp_DormantNanoprobes>();
        }

        public static bool IsValidVictim(Pawn pawn)
        {
            return pawn.RaceProps.Humanlike
                && pawn.genes != null
                && !SlorgUtility.IsSlorg(pawn)
                && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_DormantNanoprobes)
                && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_NanoprobeInfection);
        }

        /// <summary>Runs every collective refresh for each colony map.</summary>
        public static void TickMap(Map map)
        {
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned.ToList();
            List<Pawn> sleepers = colonists.Where(p => SleeperComp(p) != null).ToList();
            if (sleepers.Count == 0)
            {
                return;
            }

            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            if (sleepers.Count >= tuning.uprisingMinSleepers && sleepers.Count >= colonists.Count * tuning.uprisingColonistFraction)
            {
                StartUprising(map, sleepers);
                return;
            }

            int now = Find.TickManager.TicksGame;
            foreach (Pawn sleeper in sleepers)
            {
                HediffComp_DormantNanoprobes comp = SleeperComp(sleeper);
                if (!comp.IsActive || now < comp.nextInjectTick || !CanActNow(sleeper))
                {
                    continue;
                }
                Pawn victim = FindVictim(sleeper, colonists);
                if (victim == null)
                {
                    comp.nextInjectTick = now + GenDate.TicksPerHour;
                    continue;
                }
                Job job = JobMaker.MakeJob(SlorgDefOf.Slorg_SecretInject, victim);
                sleeper.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true);
                comp.nextInjectTick = now + (int)(tuning.sleeperInjectIntervalDays.RandomInRange * GenDate.TicksPerDay);
            }
        }

        private static bool CanActNow(Pawn pawn)
        {
            return pawn.Spawned
                && !pawn.Downed
                && !pawn.Drafted
                && !pawn.InMentalState
                && pawn.Awake()
                && pawn.CurJobDef != SlorgDefOf.Slorg_SecretInject;
        }

        /// <summary>Prefers sleeping or downed colonists, then anyone who is alone.</summary>
        private static Pawn FindVictim(Pawn sleeper, List<Pawn> colonists)
        {
            Pawn best = null;
            int bestScore = 0;
            foreach (Pawn other in colonists)
            {
                if (other == sleeper || !IsValidVictim(other) || !sleeper.CanReserveAndReach(other, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }
                int score;
                if (!other.Awake() || other.Downed)
                {
                    score = 2;
                }
                else if (!other.Drafted && !IsObserved(other, sleeper, other))
                {
                    score = 1;
                }
                else
                {
                    continue;
                }
                if (score > bestScore)
                {
                    best = other;
                    bestScore = score;
                }
            }
            return best;
        }

        /// <summary>Someone other than the two involved is awake nearby and can see the spot.</summary>
        private static bool IsObserved(Pawn at, Pawn sleeper, Pawn victim)
        {
            foreach (Pawn other in at.Map.mapPawns.FreeColonistsSpawned)
            {
                if (other == sleeper || other == victim || !other.Awake() || SleeperComp(other) != null)
                {
                    continue;
                }
                if (other.Position.InHorDistOf(at.Position, ObservationRadius) && GenSight.LineOfSight(other.Position, at.Position, at.Map))
                {
                    return true;
                }
            }
            return false;
        }

        public static void MaybeWitnessed(Pawn sleeper, Pawn victim)
        {
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            float chance = IsObserved(victim, sleeper, victim) ? tuning.witnessChanceObserved : tuning.witnessChanceUnobserved;
            if (Rand.Chance(chance))
            {
                string state = victim.Awake() ? "" : " sleeping";
                Messages.Message($"{sleeper.LabelShortCap} was seen leaning over{state} {victim.LabelShortCap} for a long moment. Something about it seemed wrong.",
                    new LookTargets(sleeper), MessageTypeDefOf.CautionInput);
            }
        }

        public static void StartUprising(Map map, List<Pawn> sleepers)
        {
            Faction faction = SlorgUtility.ResolveSlorgFaction(SleeperComp(sleepers[0])?.sourceFaction, map.Tile);
            if (faction == null)
            {
                // No collective left to call them: the nanoprobes burn out and leave them free.
                foreach (Pawn sleeper in sleepers)
                {
                    Hediff dormant = sleeper.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_DormantNanoprobes);
                    if (dormant != null)
                    {
                        sleeper.health.RemoveHediff(dormant);
                    }
                }
                return;
            }

            foreach (Pawn sleeper in sleepers)
            {
                if (sleeper.Drafted)
                {
                    sleeper.drafter.Drafted = false;
                }
                SlorgUtility.MakeThrall(sleeper);
            }
            // A captive queen's drones join the rising, and she breaks out of her cell.
            List<Pawn> risers = new List<Pawn>(sleepers);
            Pawn captiveQueen = map.mapPawns.PrisonersOfColonySpawned.FirstOrDefault(CaptiveQueen.IsCaptiveQueen);
            if (captiveQueen != null)
            {
                foreach (Pawn drone in CaptiveQueen.BoundTo(captiveQueen).Where(d => d.Spawned && d.Map == map).ToList())
                {
                    Hediff bond = drone.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_QueenBound);
                    if (bond != null)
                    {
                        drone.health.RemoveHediff(bond);
                    }
                    if (drone.Drafted)
                    {
                        drone.drafter.Drafted = false;
                    }
                    risers.Add(drone);
                }
            }
            SlorgUtility.TurnOnColony(risers, faction, map);
            if (captiveQueen != null && !captiveQueen.Downed)
            {
                PrisonBreakUtility.StartPrisonBreak(captiveQueen);
            }

            string names = string.Join(", ", risers.Select(p => p.LabelShortCap));
            string text = $"Dormant Slorg nanoprobes have woken. {names} have revealed themselves as Slorg thralls and turned on the colony.\n\n"
                + "They will try to down and inject everyone they can.";

            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            if (Rand.Chance(tuning.uprisingRaidChance))
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                parms.faction = faction;
                parms.forced = true;
                parms.points *= tuning.uprisingRaidPointsFactor;
                Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + tuning.uprisingRaidDelayTicks, parms);
                text += "\n\nThe collective heard them. A Slorg force is on its way.";
            }

            Find.LetterStack.ReceiveLetter("Slorg uprising", text, LetterDefOf.ThreatBig, new LookTargets(sleepers));
            GameComponent_SlorgCollective.RefreshNow();
        }

        /// <summary>For the nanoprobe scan surgery.</summary>
        public static bool Purge(Pawn pawn)
        {
            Hediff dormant = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_DormantNanoprobes);
            if (dormant == null)
            {
                return false;
            }
            pawn.health.RemoveHediff(dormant);
            return true;
        }
    }
}

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// One collective's adaptation to one kind of damage. Every hit a linked drone takes teaches the whole hive, and
    /// each level makes every drone in the faction shrug off more of it. It fades when that weapon stops being used.
    /// </summary>
    public class SlorgAdaptation : IExposable
    {
        public Faction faction;
        public DamageDef damage;
        public int hits;
        public int level;
        public int lastHitTick;

        public float Resistance
        {
            get
            {
                List<float> levels = SlorgDefOf.Slorg_Collective.adaptationResistance;
                return level <= 0 || levels.Count == 0 ? 0f : levels[Mathf.Min(level, levels.Count) - 1];
            }
        }

        public string DamageLabel => damage.label.NullOrEmpty() ? damage.defName : damage.label;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Defs.Look(ref damage, "damage");
            Scribe_Values.Look(ref hits, "hits");
            Scribe_Values.Look(ref level, "level");
            Scribe_Values.Look(ref lastHitTick, "lastHitTick");
        }
    }

    public static class SlorgAdaptations
    {
        private static List<SlorgAdaptation> All => GameComponent_SlorgCollective.Instance?.adaptations;

        public static IEnumerable<SlorgAdaptation> For(Faction faction)
        {
            List<SlorgAdaptation> all = All;
            if (all == null)
            {
                yield break;
            }
            foreach (SlorgAdaptation adaptation in all)
            {
                if (adaptation.faction == faction && adaptation.level > 0)
                {
                    yield return adaptation;
                }
            }
        }

        private static SlorgAdaptation Get(Faction faction, DamageDef damage)
        {
            List<SlorgAdaptation> all = All;
            if (all == null)
            {
                return null;
            }
            foreach (SlorgAdaptation adaptation in all)
            {
                if (adaptation.faction == faction && adaptation.damage == damage)
                {
                    return adaptation;
                }
            }
            SlorgAdaptation created = new SlorgAdaptation { faction = faction, damage = damage };
            all.Add(created);
            return created;
        }

        /// <summary>The hive learns from real weapons, not from surgery, its own weapons or its own drones.</summary>
        private static bool Teaches(Pawn pawn, DamageInfo dinfo)
        {
            DamageDef def = dinfo.Def;
            return def != null && def.harmsHealth && dinfo.Amount > 0f
                && def != DamageDefOf.SurgicalCut && def != DamageDefOf.ExecutionCut
                && !def.defName.StartsWith("Slorg_")
                && dinfo.Instigator?.Faction != pawn.Faction;
        }

        /// <summary>Reduces the damage by what the collective has already learned, then learns from the hit.</summary>
        public static void Apply(Pawn pawn, ref DamageInfo dinfo)
        {
            if (!SlorgUtility.IsLinkedDrone(pawn) || !Teaches(pawn, dinfo))
            {
                return;
            }
            SlorgAdaptation adaptation = Get(pawn.Faction, dinfo.Def);
            if (adaptation == null)
            {
                return;
            }
            float resistance = adaptation.Resistance;
            if (resistance > 0f)
            {
                dinfo.SetAmount(dinfo.Amount * (1f - resistance));
                if (pawn.Spawned && Rand.Chance(0.2f))
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Adapted", new Color(0.4f, 1f, 0.5f));
                }
            }
            Learn(pawn, adaptation);
        }

        private static void Learn(Pawn pawn, SlorgAdaptation adaptation)
        {
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            adaptation.hits++;
            adaptation.lastHitTick = Find.TickManager.TicksGame;
            int level = Mathf.Min(tuning.adaptationResistance.Count, adaptation.hits / Mathf.Max(1, tuning.adaptationHitsPerLevel));
            if (level <= adaptation.level)
            {
                return;
            }
            adaptation.level = level;
            string text = $"The Slorg have adapted to {adaptation.DamageLabel} damage. Every drone of {pawn.Faction.Name} now shrugs off "
                + $"{adaptation.Resistance.ToStringPercent()} of it. Switch weapons, or stop using this one for a day and they'll forget.";
            if (pawn.MapHeld != null && pawn.MapHeld.mapPawns.FreeColonistsSpawnedCount > 0)
            {
                Messages.Message(text, pawn, MessageTypeDefOf.ThreatSmall);
            }
        }

        /// <summary>Adaptations to weapons nobody uses any more fade away. Runs with each collective refresh.</summary>
        public static void Decay()
        {
            List<SlorgAdaptation> all = All;
            if (all == null)
            {
                return;
            }
            int fadeTicks = (int)(SlorgDefOf.Slorg_Collective.adaptationFadeDays * GenDate.TicksPerDay);
            int now = Find.TickManager.TicksGame;
            all.RemoveAll(a => a.faction == null || a.damage == null || now - a.lastHitTick > fadeTicks);
        }

        /// <summary>Forces a collective to its highest adaptation to a damage type. For testing.</summary>
        public static void ForceMax(Faction faction, DamageDef damage)
        {
            SlorgAdaptation adaptation = Get(faction, damage);
            if (adaptation == null)
            {
                return;
            }
            SlorgCollectiveDef tuning = SlorgDefOf.Slorg_Collective;
            adaptation.level = tuning.adaptationResistance.Count;
            adaptation.hits = adaptation.level * tuning.adaptationHitsPerLevel;
            adaptation.lastHitTick = Find.TickManager.TicksGame;
        }
    }

    /// <summary>Hits a shield didn't stop are reduced by what the collective has learned.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Pawn_PreApplyDamage_Adaptation_Patch
    {
        public static void Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (!absorbed)
            {
                SlorgAdaptations.Apply(__instance, ref dinfo);
            }
        }
    }
}

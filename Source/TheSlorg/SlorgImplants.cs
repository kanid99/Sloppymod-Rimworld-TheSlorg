using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheSlorg
{
    /// <summary>Marks a HediffDef as a Slorg implant and describes what removing it costs.</summary>
    public class SlorgImplantExtension : DefModExtension
    {
        /// <summary>Flat skill bonuses while installed (brain implants).</summary>
        public List<SkillOffset> skillOffsets = new List<SkillOffset>();

        /// <summary>Faster assimilation: new infections from this pawn start at this severity.</summary>
        public float assimilationStartSeverity = -1f;

        // Removal: always destroys the implant and wounds the pawn.
        public int removalWounds = 3;
        public FloatRange removalWoundSize = new FloatRange(4f, 8f);
        public float removalScarChance = 0.6f;
        public float removalDeathChance = 0.02f;
        /// <summary>Wound random places all over the body instead of the implant's own body part.</summary>
        public bool removalWoundsWholeBody;
    }

    public class SkillOffset
    {
        public SkillDef skill;
        public int offset;
    }

    /// <summary>The implants a pawn kind is generated with.</summary>
    public class SlorgImplantSetExtension : DefModExtension
    {
        public List<ImplantEntry> implants = new List<ImplantEntry>();

        /// <summary>Slorg arrive with no clothes and no weapons: their implants are their gear.</summary>
        public bool stripGear = true;
    }

    public class ImplantEntry
    {
        public HediffDef hediff;
        /// <summary>Body part to install into; null means the whole body.</summary>
        public BodyPartDef part;
        public float chance = 1f;
    }

    public static class SlorgImplants
    {
        private static readonly Dictionary<Pawn, int[]> skillBonusCache = new Dictionary<Pawn, int[]>();

        public static bool IsSlorgImplant(Hediff hediff)
        {
            return hediff?.def.GetModExtension<SlorgImplantExtension>() != null;
        }

        public static bool HasAnyImplant(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null && pawn.health.hediffSet.hediffs.Any(IsSlorgImplant);
        }

        public static void Install(Pawn pawn, HediffDef def, BodyPartDef partDef)
        {
            BodyPartRecord part = null;
            if (partDef != null)
            {
                // Prefer a part with no Slorg implant yet (so the beam and the lash end up on different arms),
                // otherwise any part that doesn't already have this implant.
                List<BodyPartRecord> parts = pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def == partDef).ToList();
                part = parts.FirstOrDefault(p => !pawn.health.hediffSet.hediffs.Any(h => h.Part == p && IsSlorgImplant(h)))
                    ?? parts.FirstOrDefault(p => !pawn.health.hediffSet.hediffs.Any(h => h.Part == p && h.def == def));
                if (part == null)
                {
                    return;
                }
            }
            if (pawn.health.hediffSet.hediffs.Any(h => h.def == def && h.Part == part))
            {
                return;
            }
            pawn.health.AddHediff(def, part);
            UpdateSkillBonus(pawn);
        }

        public static void InstallSet(Pawn pawn, SlorgImplantSetExtension set)
        {
            foreach (ImplantEntry entry in set.implants)
            {
                if (entry.hediff != null && Rand.Chance(entry.chance))
                {
                    Install(pawn, entry.hediff, entry.part);
                }
            }
        }

        // ---------------------------------------------------------------- Skill bonuses (brain implants)

        public static int SkillBonus(Pawn pawn, SkillDef skill)
        {
            return pawn != null && skillBonusCache.TryGetValue(pawn, out int[] bonus) && skill.index < bonus.Length ? bonus[skill.index] : 0;
        }

        public static void UpdateSkillBonus(Pawn pawn)
        {
            int[] bonus = null;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                SlorgImplantExtension ext = hediff.def.GetModExtension<SlorgImplantExtension>();
                if (ext == null)
                {
                    continue;
                }
                foreach (SkillOffset offset in ext.skillOffsets)
                {
                    bonus ??= new int[DefDatabase<SkillDef>.DefCount];
                    bonus[offset.skill.index] += offset.offset;
                }
            }
            if (bonus == null)
            {
                skillBonusCache.Remove(pawn);
            }
            else
            {
                skillBonusCache[pawn] = bonus;
            }
        }

        public static void PruneSkillCache()
        {
            foreach (Pawn pawn in skillBonusCache.Keys.ToList())
            {
                if (pawn.Destroyed || pawn.Dead)
                {
                    skillBonusCache.Remove(pawn);
                }
            }
        }

        // ---------------------------------------------------------------- Assimilation boost

        public static float AssimilationStartSeverity(Pawn caster)
        {
            float best = -1f;
            foreach (Hediff hediff in caster.health.hediffSet.hediffs)
            {
                SlorgImplantExtension ext = hediff.def.GetModExtension<SlorgImplantExtension>();
                if (ext != null && ext.assimilationStartSeverity > best)
                {
                    best = ext.assimilationStartSeverity;
                }
            }
            return best;
        }

        // ---------------------------------------------------------------- Removal

        /// <summary>Tears an implant out. It is always destroyed, and the pawn is always hurt.</summary>
        public static void RemoveWithTrauma(Pawn pawn, Hediff implant)
        {
            SlorgImplantExtension ext = implant.def.GetModExtension<SlorgImplantExtension>() ?? new SlorgImplantExtension();
            BodyPartRecord implantPart = implant.Part;
            string implantLabel = implant.LabelBase;
            pawn.health.RemoveHediff(implant);
            UpdateSkillBonus(pawn);

            HashSet<Hediff> before = new HashSet<Hediff>(pawn.health.hediffSet.hediffs);
            List<BodyPartRecord> targets;
            if (ext.removalWoundsWholeBody || implantPart == null)
            {
                targets = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside)
                    .Where(p => !p.IsInGroup(BodyPartGroupDefOf.FullHead))
                    .ToList();
            }
            else
            {
                // Internal parts (brain) are reached through the part around them.
                BodyPartRecord around = implantPart.depth == BodyPartDepth.Inside && implantPart.parent != null ? implantPart.parent : implantPart;
                targets = new List<BodyPartRecord> { around };
                if (!pawn.health.hediffSet.PartIsMissing(implantPart))
                {
                    targets.Add(implantPart);
                }
            }

            for (int i = 0; i < ext.removalWounds && targets.Count > 0 && !pawn.Dead; i++)
            {
                BodyPartRecord part = targets.RandomElement();
                if (pawn.health.hediffSet.PartIsMissing(part))
                {
                    continue;
                }
                DamageDef damage = Rand.Bool ? DamageDefOf.Cut : DamageDefOf.Burn;
                pawn.TakeDamage(new DamageInfo(damage, ext.removalWoundSize.RandomInRange, 999f, -1f, null, part));
            }

            if (!pawn.Dead)
            {
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
                {
                    if (!before.Contains(hediff) && hediff is Hediff_Injury && Rand.Chance(ext.removalScarChance))
                    {
                        HediffComp_GetsPermanent permanent = hediff.TryGetComp<HediffComp_GetsPermanent>();
                        if (permanent != null)
                        {
                            permanent.IsPermanent = true;
                        }
                    }
                }
                if (Rand.Chance(ext.removalDeathChance))
                {
                    pawn.Kill(null);
                }
            }

            if (pawn.Dead)
            {
                Find.LetterStack.ReceiveLetter("Implant removal fatal",
                    $"{pawn.LabelShortCap} did not survive having the {implantLabel} torn out.", LetterDefOf.Death, pawn.Corpse ?? (LookTargets)pawn);
            }
            else
            {
                Messages.Message($"The {implantLabel} has been cut out of {pawn.LabelShortCap}. It was destroyed, and {pawn.LabelShortCap} is badly hurt.",
                    pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }

    /// <summary>Removes one Slorg implant (the recipe's removesHediff). Destroys it and injures the patient.</summary>
    public class Recipe_RemoveSlorgImplant : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn pawn
                && recipe.removesHediff != null
                && pawn.health.hediffSet.HasHediff(recipe.removesHediff)
                && base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            Hediff implant = pawn.health.hediffSet.GetFirstHediffOfDef(recipe.removesHediff);
            if (implant != null)
            {
                SlorgImplants.RemoveWithTrauma(pawn, implant);
            }
        }
    }

    /// <summary>Slorg pawn kinds get their implants when generated.</summary>
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        public static void Postfix(PawnGenerationRequest request, Pawn __result)
        {
            SlorgImplantSetExtension set = request.KindDef?.GetModExtension<SlorgImplantSetExtension>();
            if (set != null && __result?.health != null && !__result.Dead)
            {
                if (set.stripGear)
                {
                    __result.apparel?.DestroyAll();
                    __result.equipment?.DestroyAllEquipment();
                    __result.inventory?.DestroyAll();
                }
                SlorgImplants.InstallSet(__result, set);
                SlorgUtility.MakeHairless(__result);
            }
        }
    }

    // ---------------------------------------------------------------- Shield emitter

    public class HediffCompProperties_SlorgShield : HediffCompProperties
    {
        public float maxEnergy = 60f;
        public float rechargePerTick = 0.1f;
        public int resetDelayTicks = 900;

        public HediffCompProperties_SlorgShield()
        {
            compClass = typeof(HediffComp_SlorgShield);
        }
    }

    /// <summary>A personal shield grown under the skin. Stops ranged and explosive damage until it overloads.</summary>
    public class HediffComp_SlorgShield : HediffComp
    {
        private float energy = -1f;
        private int resetAtTick;

        private HediffCompProperties_SlorgShield Props => (HediffCompProperties_SlorgShield)props;

        private bool Down => Find.TickManager.TicksGame < resetAtTick;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (energy < 0f)
            {
                energy = Props.maxEnergy;
            }
            if (!Down && energy < Props.maxEnergy)
            {
                energy = Mathf.Min(Props.maxEnergy, energy + Props.rechargePerTick * delta);
            }
        }

        public bool TryAbsorb(DamageInfo dinfo)
        {
            if (energy < 0f)
            {
                energy = Props.maxEnergy;
            }
            if (Down || energy <= 0f || dinfo.Def == null || !(dinfo.Def.isRanged || dinfo.Def.isExplosive))
            {
                return false;
            }
            energy -= dinfo.Amount;
            if (Pawn.Spawned)
            {
                FleckMaker.ThrowLightningGlow(Pawn.TrueCenter(), Pawn.Map, energy > 0f ? 0.8f : 2f);
            }
            if (energy <= 0f)
            {
                energy = 0f;
                resetAtTick = Find.TickManager.TicksGame + Props.resetDelayTicks;
                if (Pawn.Spawned)
                {
                    MoteMaker.ThrowText(Pawn.DrawPos, Pawn.Map, "Shield overloaded", Color.cyan);
                }
            }
            return true;
        }

        public override string CompLabelInBracketsExtra => Down ? "overloaded" : Mathf.RoundToInt(100f * Mathf.Max(energy, 0f) / Props.maxEnergy) + "%";

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref energy, "energy", -1f);
            Scribe_Values.Look(ref resetAtTick, "resetAtTick");
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Pawn_PreApplyDamage_Patch
    {
        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            List<Hediff> hediffs = __instance.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                return true;
            }
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is HediffWithComps withComps)
                {
                    HediffComp_SlorgShield shield = withComps.TryGetComp<HediffComp_SlorgShield>();
                    if (shield != null && shield.TryAbsorb(dinfo))
                    {
                        absorbed = true;
                        return false;
                    }
                }
            }
            return true;
        }
    }

    // ---------------------------------------------------------------- Dermal plating render

    /// <summary>Draws bolted-on plating over the body, with one texture per body type: texPath_BodyType_facing.</summary>
    public class PawnRenderNode_SlorgPlating : PawnRenderNode
    {
        public PawnRenderNode_SlorgPlating(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            BodyTypeDef bodyType = pawn.story?.bodyType;
            if (bodyType == null || props.texPath.NullOrEmpty())
            {
                return null;
            }
            return GraphicDatabase.Get<Graphic_Multi>(props.texPath + "_" + bodyType.defName, ShaderDatabase.Cutout, Vector2.one, Color.white);
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            return HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn);
        }
    }
}

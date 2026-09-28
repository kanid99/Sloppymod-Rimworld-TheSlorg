using HarmonyLib;
using RimWorld;
using Verse;

namespace TheSlorg
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            new Harmony("theslorg.collective").PatchAll();
        }
    }

    /// <summary>
    /// A linked drone uses the collective's level for any skill where the collective knows more than the drone.
    /// The drone's own level and XP are untouched, so the bonus disappears the moment the source drone is lost.
    /// </summary>
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.GetLevel))]
    public static class SkillRecord_GetLevel_Patch
    {
        public static void Postfix(SkillRecord __instance, ref int __result)
        {
            ApplyCollectiveLevel(__instance, ref __result);
        }

        internal static void ApplyCollectiveLevel(SkillRecord __instance, ref int __result)
        {
            if (GameComponent_SlorgCollective.readingNaturalSkills || __instance.TotallyDisabled)
            {
                return;
            }
            SlorgCollective collective = GameComponent_SlorgCollective.CollectiveOf(__instance.Pawn);
            if (collective == null)
            {
                return;
            }
            int shared = collective.LevelFor(__instance.def);
            if (shared > __result)
            {
                __result = shared;
            }
        }
    }

    /// <summary>Also hook the Level getter directly in case GetLevel gets inlined into it.</summary>
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Level), MethodType.Getter)]
    public static class SkillRecord_Level_Patch
    {
        public static void Postfix(SkillRecord __instance, ref int __result)
        {
            SkillRecord_GetLevel_Patch.ApplyCollectiveLevel(__instance, ref __result);
        }
    }

    /// <summary>Show the collective level on the skills tab.</summary>
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.GetLevelForUI))]
    public static class SkillRecord_GetLevelForUI_Patch
    {
        public static void Postfix(SkillRecord __instance, ref int __result)
        {
            SkillRecord_GetLevel_Patch.ApplyCollectiveLevel(__instance, ref __result);
        }
    }
}

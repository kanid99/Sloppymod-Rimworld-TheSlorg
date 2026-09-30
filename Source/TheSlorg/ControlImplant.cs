using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// Every queen carries a control implant. It lets her command drones, can be extracted intact, and in any
    /// pawn outside the hive it can trace the collective's signal back to the Unicomplex.
    /// </summary>
    public static class ControlImplant
    {
        public static bool Has(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_ControlImplant);
        }

        /// <summary>Colonists (including a freed queen) with the implant can trace the signal. Hive members can't.</summary>
        public static bool CanUse(Pawn pawn)
        {
            return Has(pawn) && pawn.Faction != null && pawn.Faction.IsPlayer && !pawn.IsPrisoner && !SlorgUtility.HasLinkGene(pawn);
        }

        private static Faction TargetFaction()
        {
            GameComponent_SlorgCollective component = GameComponent_SlorgCollective.Instance;
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (SlorgUtility.IsSlorgFaction(faction) && !faction.defeated && component != null && !component.SurfaceControlLost(faction)
                    && GameComponent_SlorgCollective.UnicomplexOf(faction) != null)
                {
                    return faction;
                }
            }
            return null;
        }

        public static AcceptanceReport CanTrace()
        {
            Faction faction = TargetFaction();
            if (faction == null)
            {
                return "There is no hive signal left on this world to trace.";
            }
            if (GameComponent_SlorgCollective.Instance.UnicomplexRevealed(faction))
            {
                return "The Unicomplex has already been located.";
            }
            return true;
        }

        public static void TraceHiveSignal(Pawn holder)
        {
            Faction faction = TargetFaction();
            if (faction == null)
            {
                return;
            }
            GameComponent_SlorgCollective.Instance.RevealUnicomplex(faction);
            Settlement unicomplex = GameComponent_SlorgCollective.UnicomplexOf(faction);
            Find.LetterStack.ReceiveLetter("Unicomplex located",
                $"{holder.LabelShortCap} has followed the hive signal back to its source: {unicomplex.Label}, the Unicomplex of {faction.Name}.\n\n"
                + "The queen core is there. Assault the Unicomplex and destroy the core, and every Slorg drone on this world will be cut loose.",
                LetterDefOf.PositiveEvent, new LookTargets(unicomplex));
        }
    }

    /// <summary>Extracts a control implant intact, unlike other Slorg implants which are destroyed when cut out.</summary>
    public class Recipe_ExtractControlImplant : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn pawn && ControlImplant.Has(pawn) && base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            Hediff implant = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_ControlImplant);
            if (implant == null)
            {
                return;
            }
            BodyPartRecord brainCase = implant.Part?.parent ?? implant.Part;
            pawn.health.RemoveHediff(implant);
            if (brainCase != null && !pawn.health.hediffSet.PartIsMissing(brainCase))
            {
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, 3f, 999f, -1f, null, brainCase));
            }
            if (pawn.MapHeld != null)
            {
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(SlorgDefOf.Slorg_ControlImplantItem), pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
            }
            if (SlorgUtility.IsQueen(pawn) && !pawn.Dead)
            {
                // A queen's mind is built around her command node. Without it she dies.
                pawn.Kill(null);
                Find.LetterStack.ReceiveLetter("The queen is dead",
                    $"{pawn.LabelShortCap}'s mind was built around her control implant. With it gone, she is dead.\n\n"
                    + "The implant survived the extraction. Install it in a colonist, or have a colonist use it on themselves, to trace the hive signal to the Unicomplex.",
                    LetterDefOf.NeutralEvent, pawn.Corpse ?? (LookTargets)pawn);
            }
            else
            {
                Messages.Message($"The control implant has been extracted from {pawn.LabelShortCap}.", pawn, MessageTypeDefOf.PositiveEvent);
            }
            GameComponent_SlorgCollective.RefreshNow();
        }
    }
}

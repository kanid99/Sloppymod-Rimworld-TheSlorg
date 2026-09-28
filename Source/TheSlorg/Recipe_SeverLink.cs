using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// Cuts a severed drone's link for good, turning them into a disconnected drone. Only possible while the drone is
    /// severed (their queen fell nearby), because a live link fights back.
    /// </summary>
    public class Recipe_SeverLink : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn pawn
                && SlorgUtility.HasLinkGene(pawn)
                && pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_Severance)
                && base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            SlorgUtility.MakeDisconnected(pawn);
            Messages.Message($"{pawn.LabelShortCap}'s link to the collective has been cut. They are now a disconnected drone.", pawn, MessageTypeDefOf.PositiveEvent);
            GameComponent_SlorgCollective.RefreshNow();
        }
    }
}

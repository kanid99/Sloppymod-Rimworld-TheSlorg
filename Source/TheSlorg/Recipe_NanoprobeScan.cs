using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>Finds and purges dormant Slorg nanoprobes. The only way to catch a sleeper agent before they rise up.</summary>
    public class Recipe_NanoprobeScan : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return thing is Pawn pawn && pawn.RaceProps.Humanlike && !SlorgUtility.HasLinkGene(pawn) && base.AvailableOnNow(thing, part);
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }
            if (SleeperUtility.Purge(pawn))
            {
                Find.LetterStack.ReceiveLetter("Sleeper found",
                    $"The scan found dormant Slorg nanoprobes in {pawn.LabelShortCap} and purged them. {pawn.LabelShortCap} had been a sleeper agent for the collective.\n\n"
                    + "Anyone they spent time with may be infected too.",
                    LetterDefOf.PositiveEvent, pawn);
            }
            else
            {
                Messages.Message($"The scan found no nanoprobes in {pawn.LabelShortCap}.", pawn, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}

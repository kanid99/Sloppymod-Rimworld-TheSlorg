using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace TheSlorg
{
    public static class SlorgUtility
    {
        public static bool IsSlorgFaction(Faction faction)
        {
            return faction != null && faction.def == SlorgDefOf.Slorg_CollectiveFaction;
        }

        /// <summary>Carries the collective link gene (drones and queens, linked or not).</summary>
        public static bool HasLinkGene(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(SlorgDefOf.Slorg_CollectiveLink);
        }

        public static bool IsQueen(Pawn pawn)
        {
            return pawn?.genes != null && pawn.genes.HasActiveGene(SlorgDefOf.Slorg_HiveSovereign);
        }

        /// <summary>Any Slorg pawn at all, including disconnected drones.</summary>
        public static bool IsSlorg(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return false;
            }
            foreach (Gene gene in pawn.genes.GenesListForReading)
            {
                if (gene.def.defName.StartsWith("Slorg_"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>A living pawn with the link gene, in a non-player faction.</summary>
        public static bool IsDrone(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.Faction != null
                && !pawn.Faction.IsPlayer
                && HasLinkGene(pawn);
        }

        /// <summary>A drone currently able to hear the collective: not captured, enslaved or severed.</summary>
        public static bool IsLinkedDrone(Pawn pawn)
        {
            return IsDrone(pawn)
                && !pawn.IsPrisoner
                && !pawn.IsSlave
                && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_Severance);
        }

        public static bool IsSpace(PlanetTile tile)
        {
            return tile.Valid && tile.LayerDef != null && tile.LayerDef.isSpace;
        }

        /// <summary>
        /// Turns any Slorg into a disconnected drone. Full drones keep their implants; thralls, who never had any,
        /// lose their Slorg genes and keep only the severed-link marker.
        /// </summary>
        public static void MakeDisconnected(Pawn pawn)
        {
            if (pawn?.genes == null || pawn.genes.Xenotype == SlorgDefOf.Slorg_DisconnectedDrone || pawn.genes.Xenotype == SlorgDefOf.Slorg_FreedQueen)
            {
                return;
            }
            if (IsQueen(pawn))
            {
                MakeFreedQueen(pawn);
                return;
            }
            if (pawn.genes.Xenotype == SlorgDefOf.Slorg_Thrall)
            {
                foreach (Gene gene in pawn.genes.Xenogenes.ToList())
                {
                    if (gene.def.defName.StartsWith("Slorg_"))
                    {
                        pawn.genes.RemoveGene(gene);
                    }
                }
                pawn.genes.AddGene(SlorgDefOf.Slorg_Severed, xenogene: true);
                pawn.genes.SetXenotypeDirect(SlorgDefOf.Slorg_DisconnectedDrone);
            }
            else
            {
                pawn.genes.SetXenotype(SlorgDefOf.Slorg_DisconnectedDrone);
            }
            GrowHairBack(pawn);
            if (pawn.guest != null)
            {
                // Without the collective's voice in their head, a freed drone is open to persuasion.
                pawn.guest.resistance = UnityEngine.Mathf.Min(pawn.guest.resistance, 8f);
                pawn.guest.will = UnityEngine.Mathf.Min(pawn.guest.will, 2f);
            }
            Hediff link = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff);
            if (link != null)
            {
                pawn.health.RemoveHediff(link);
            }
        }

        /// <summary>
        /// A queen cut from the hive keeps her gifts: her implants can't call her back, and they work as a mechlink,
        /// making her a natural commander of machines.
        /// </summary>
        public static void MakeFreedQueen(Pawn pawn)
        {
            pawn.genes.SetXenotype(SlorgDefOf.Slorg_FreedQueen);
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            if (brain != null && !pawn.health.hediffSet.HasHediff(HediffDefOf.MechlinkImplant))
            {
                pawn.health.AddHediff(HediffDefOf.MechlinkImplant, brain);
            }
            Hediff link = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_CollectiveLinkHediff);
            if (link != null)
            {
                pawn.health.RemoveHediff(link);
            }
            GrowHairBack(pawn);
            if (pawn.guest != null)
            {
                pawn.guest.resistance = UnityEngine.Mathf.Min(pawn.guest.resistance, 8f);
                pawn.guest.will = UnityEngine.Mathf.Min(pawn.guest.will, 2f);
            }
            Find.LetterStack.ReceiveLetter("A queen set free",
                $"{pawn.LabelShortCap} has been cut from the hive. For the first time she is alone in her own head.\n\n"
                + "She keeps her implants with no risk of the collective calling her back, and they now work as a mechlink: "
                + "she is a natural commander of machines, with extra mech bandwidth and control groups. She also keeps a queen's resilience.\n\n"
                + "She's open to recruitment.",
                LetterDefOf.PositiveEvent, pawn);
        }

        /// <summary>
        /// Half-assimilated: the collective's genes without the cybernetics. Keeps the pawn's own body and looks.
        /// </summary>
        public static void MakeThrall(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return;
            }
            pawn.genes.SetXenotype(SlorgDefOf.Slorg_Thrall);
            MakeHairless(pawn);
        }

        /// <summary>Slorg are completely hairless.</summary>
        public static void MakeHairless(Pawn pawn)
        {
            bool changed = false;
            if (pawn.story != null && pawn.story.hairDef != HairDefOf.Bald)
            {
                pawn.story.hairDef = HairDefOf.Bald;
                changed = true;
            }
            if (pawn.style != null && pawn.style.beardDef != BeardDefOf.NoBeard)
            {
                pawn.style.beardDef = BeardDefOf.NoBeard;
                changed = true;
            }
            if (changed)
            {
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
        }

        /// <summary>A freed drone is an individual again: the hive's enforced baldness ends and hair grows back.</summary>
        public static void GrowHairBack(Pawn pawn)
        {
            if (pawn.story == null)
            {
                return;
            }
            if (pawn.story.hairDef == HairDefOf.Bald)
            {
                pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
            }
            if (pawn.style != null && pawn.style.beardDef == BeardDefOf.NoBeard)
            {
                pawn.style.beardDef = PawnStyleItemChooser.RandomBeardFor(pawn);
            }
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }

        /// <summary>Fully assimilated: drone genes, the standard drone implants, and nanoprobes that mend the body.</summary>
        public static void MakeFullDrone(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return;
            }
            pawn.genes.SetXenotype(SlorgDefOf.Slorg_Drone);
            DropAllGear(pawn);
            SlorgImplantSetExtension set = SlorgDefOf.Slorg_DroneKind.GetModExtension<SlorgImplantSetExtension>();
            if (set != null)
            {
                foreach (ImplantEntry entry in set.implants)
                {
                    if (entry.chance >= 1f)
                    {
                        SlorgImplants.Install(pawn, entry.hediff, entry.part);
                    }
                }
            }
            Hediff dormant = pawn.health.hediffSet.GetFirstHediffOfDef(SlorgDefOf.Slorg_DormantNanoprobes);
            if (dormant != null)
            {
                pawn.health.RemoveHediff(dormant);
            }
            MakeHairless(pawn);
            NanoprobeHeal(pawn);
        }

        /// <summary>A new drone sheds everything it carried: clothes, weapons and inventory.</summary>
        public static void DropAllGear(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                IntVec3 pos = pawn.Position;
                pawn.apparel?.DropAll(pos, forbid: false, dropLocked: true);
                pawn.equipment?.DropAllEquipment(pos, forbid: false);
                pawn.inventory?.DropAllNearPawn(pos);
            }
            else
            {
                pawn.apparel?.DestroyAll();
                pawn.equipment?.DestroyAllEquipment();
                pawn.inventory?.DestroyAll();
            }
        }

        /// <summary>Linked nanoprobes clear out chronic conditions: dementia, cataracts, bad backs, blocked arteries and the like.</summary>
        public static void NanoprobeHeal(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || pawn.Dead)
            {
                return;
            }
            List<HediffDef> cures = SlorgDefOf.Slorg_Collective.nanoprobeCures;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
            {
                if (hediff.def.chronic || cures.Contains(hediff.def))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        /// <summary>The Slorg faction to hand a new thrall to, or null if the collective is gone from this world.</summary>
        public static Faction ResolveSlorgFaction(Faction preferred, PlanetTile tile)
        {
            GameComponent_SlorgCollective collective = GameComponent_SlorgCollective.Instance;
            bool Usable(Faction f) => f != null && !f.defeated && IsSlorgFaction(f)
                && !(collective != null && collective.SurfaceControlLost(f) && !IsSpace(tile));
            if (Usable(preferred))
            {
                return preferred;
            }
            Faction any = Find.FactionManager.FirstFactionOfDef(SlorgDefOf.Slorg_CollectiveFaction);
            return Usable(any) ? any : null;
        }

        /// <summary>New thralls on a colony map turn on it instead of leaving.</summary>
        public static void TurnOnColony(List<Pawn> pawns, Faction faction, Map map)
        {
            foreach (Pawn pawn in pawns)
            {
                if (pawn.Faction != faction)
                {
                    pawn.SetFaction(faction);
                }
                pawn.GetLord()?.RemovePawn(pawn);
            }
            LordMaker.MakeNewLord(faction,
                new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: false,
                    useAvoidGridSmart: false, canSteal: false, breachers: false, canPickUpOpportunisticWeapons: true),
                map, pawns);
        }

        public static void AddSeverance(Pawn pawn)
        {
            if (!pawn.Dead && !pawn.health.hediffSet.HasHediff(SlorgDefOf.Slorg_Severance))
            {
                pawn.health.AddHediff(SlorgDefOf.Slorg_Severance);
            }
        }
    }
}

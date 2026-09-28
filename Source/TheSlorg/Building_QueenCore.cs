using RimWorld;
using Verse;

namespace TheSlorg
{
    /// <summary>The heart of a Slorg world. Destroying it ends the faction's control of the planet surface.</summary>
    public class Building_QueenCore : Building
    {
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Faction owner = Faction;
            Map map = MapHeld;
            bool counts = mode == DestroyMode.KillFinalize || mode == DestroyMode.Deconstruct;
            base.Destroy(mode);
            if (counts && SlorgUtility.IsSlorgFaction(owner))
            {
                GameComponent_SlorgCollective.Instance?.Notify_QueenCoreDestroyed(owner, map);
            }
        }
    }
}

using UnityEngine;
using Verse;

namespace TheSlorg
{
    /// <summary>
    /// The mod settings page. For now it only shows the installed build - About.xml's modVersion,
    /// 0.9.(commit count) like every SloppyMods mod - so a bug report can name it.
    /// </summary>
    public class TheSlorgMod : Mod
    {
        public TheSlorgMod(ModContentPack content) : base(content)
        {
        }

        public override string SettingsCategory() => "The Slorg";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.Label("Build " + Content.ModMetaData.ModVersion);
            list.End();
        }
    }
}

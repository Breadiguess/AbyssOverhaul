using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace AbyssOverhaul.Common
{
    //An attempt was made, but music display insisted that we already registered music slots for these, which caused loading errors.
    public class MusicDisplayCompat : ModSystem
    {
        public override void PostAddRecipes()
        {
            if (!ModLoader.TryGetMod("MusicDisplay", out Mod display))
                return;


            LocalizedText ModName = Language.GetText($"Mods.AbyssOverhaul.Music.ModName");

            void AddMusic(string path, string name)
            {
                LocalizedText author = Language.GetText("Mods.AbyssOverhaul.Music" + name + ".Author");
                LocalizedText displayName = Language.GetText("Mods.AbyssOverhaul.Music" + name + ".Name");
                display.Call("AddMusic", (short)MusicLoader.GetMusicSlot(Mod, path), displayName, author, ModName);
            }

            //AddMusic("AbyssOverhaul/Sounds/Music/FossilShaleOst", "FossilShale");
            //AddMusic("AbyssOverhaul/Sounds/Music/TenebrousMarshOst", "TenebrousMarsh");
            //AddMusic("AbyssOverhaul/Sounds/Music/VeilOst", "Veil");
        }

    }
}

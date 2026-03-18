using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.TheVeil.Tiles
{
    internal class VoidstoneMantle_Item : ModItem
    {
        public override string LocalizationCategory => "Tiles";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 100;
            ItemID.Sets.ExtractinatorMode[Type] = Item.type;

            // Mods can be translated to any of the languages tModLoader supports. See https://github.com/tModLoader/tModLoader/wiki/Localization
            // Translations go in localization files (.hjson files), but these are listed here as an example to help modders become aware of the possibility that users might want to use your mod in other languages:
            // English: "Example Block", "This is a modded tile."
            // German: "Beispielblock", "Dies ist ein modded Block"
            // Italian: "Blocco di esempio", "Questo è un blocco moddato"
            // French: "Bloc d'exemple", "C'est un bloc modgé"
            // Spanish: "Bloque de ejemplo", "Este es un bloque modded"
            // Russian: "Блок примера", "Это модифицированный блок"
            // Chinese: "例子块", "这是一个修改块"
            // Portuguese: "Bloco de exemplo", "Este é um bloco modded"
            // Polish: "Przykładowy blok", "Jest to modded blok"
        }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<VoidstoneMantle>());
            Item.width = 12;
            Item.height = 12;
        }
      
    }
}

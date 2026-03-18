using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles
{
    internal class CarbonShale_Item : ModItem
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
            Item.DefaultToPlaceableTile(ModContent.TileType<CarbonShale_Tile>());
            Item.width = 12;
            Item.height = 12;
        }
        public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack)
        { // Calls upon use of an extractinator. Below is the chance you will get ExampleOre from the extractinator.
            if (Main.rand.NextBool(3))
            {
                //resultType = ModContent.ItemType<ExampleOre>();  // Get this from the extractinator with a 1 in 3 chance.
                if (Main.rand.NextBool(5))
                {
                    resultStack += Main.rand.Next(2); // Add a chance to get more than one of ExampleOre from the extractinator.
                }
            }
        }
    }
}

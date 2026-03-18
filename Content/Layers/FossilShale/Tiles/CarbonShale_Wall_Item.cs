using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles
{
    internal class CarbonShale_Wall_Item : ModItem
    {
        public override string LocalizationCategory => "Tiles";
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            // ModContent.WallType<Walls.ExampleWall>() retrieves the id of the wall that this item should place when used.
            // DefaultToPlaceableWall handles setting various Item values that placeable wall items use.
            // Hover over DefaultToPlaceableWall in Visual Studio to read the documentation!
            Item.DefaultToPlaceableWall(ModContent.WallType<CarbonShale_Wall>());
        }

        // Please see Content/ExampleRecipes.cs for a detailed explanation of recipe creation.
        public override void AddRecipes()
        {
            CreateRecipe(4)
                .AddIngredient<CarbonShale_Item>()
                .AddTile(TileID.WorkBenches)
                .Register();
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

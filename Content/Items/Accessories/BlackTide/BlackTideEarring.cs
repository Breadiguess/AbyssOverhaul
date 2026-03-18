using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideEarring : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToAccessory(24, 24);

        }

        public override void UpdateEquip(Player player)
        {
            if(player.TryGetModPlayer<BlackTideEarringPlayer>(out var black))
            {
                black.Active = true;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<BatholithBangle>())
                .AddIngredient(ModContent.ItemType<ScionsCurio>())
                .AddIngredient(ModContent.ItemType<ReaperTooth>(), 12)
                .AddIngredient(ModContent.ItemType<RuinousSoul>(), 24).AddTile(ModContent.TileType<CosmicAnvil>());
        }
    }
}

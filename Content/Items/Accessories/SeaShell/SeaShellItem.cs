using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Accessories.SeaShell
{
    internal class SeaShellItem : ModItem
    {
        public override string LocalizationCategory => "Items";
        public override void SetDefaults()
        {
            Item.DefaultToAccessory();
            Item.rare = ItemRarityID.Green;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SeashellPlayer>().Active = true;

            player.GetModPlayer<SeashellPlayer>().Visible = !hideVisual;


            if (player.wet)
                player.breathMax += 2;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<GiantShell>())
                .AddIngredient(ModContent.ItemType<SeaRemains>(), 12)
                .AddIngredient(ItemID.PurpleMucos)
                .AddTile(TileID.Furnaces);
        }
    }
}

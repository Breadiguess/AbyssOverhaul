using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Enums;

namespace AbyssOverhaul.Content.Items.Accessories.PetriDish
{
    public class PetriDishItem : ModItem
    {
        public override void SetDefaults()
        {
            //Basics
            Item.width = 34;
            Item.height = 26;
            Item.accessory = true;

            //Shop
            Item.SetShopValues(ItemRarityColor.Blue1, 16);
        }

        public override void UpdateEquip(Player player)
        {
            if (player.TryGetModPlayer<PetriDishPlayer>(out var P))
            {
                P.Active = true;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<CyanobacteriaSludge_Item>(8)
                .AddIngredient(ItemID.Glass, 12)
                .Register();
        }
    }
}

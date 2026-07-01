using AbyssOverhaul.Content.Rarities;
using CalamityMod.Items.LoreItems;
using CalamityMod.Rarities;

namespace AbyssOverhaul.Content.Items.LoreItems
{
	public class LoreTerminus : LoreItem
	{
		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.consumable = false;
			Item.rare = ModContent.RarityType<PrimordialYellow>();
		}

		// Add when primordial wyrm has a trophy
		/* public override void AddRecipes()
		{
			CreateRecipe().
				AddIngredient<PrimordialWyrmTrophy>().
				AddTile(TileID.Bookcases).
				Register();
		} */
	}
}

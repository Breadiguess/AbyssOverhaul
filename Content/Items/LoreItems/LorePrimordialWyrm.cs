using AbyssOverhaul.Content.Rarities;
using CalamityMod.Items.LoreItems;

namespace AbyssOverhaul.Content.Items.LoreItems
{
	public class LorePrimordialWyrm : LoreItem
	{
		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.consumable = false;
			Item.rare = ModContent.RarityType<AbyssalRarity>();
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

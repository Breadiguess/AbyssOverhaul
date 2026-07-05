using AbyssOverhaul.Content.Rarities;
using CalamityMod.Items.LoreItems;

namespace AbyssOverhaul.Content.Items.LoreItems
{
	// I actually hate lore items so much
	// Ozzatron, oh ozzatron, please bless us with a better system rather than useless items please :lotus:
	public class LoreSilva : LoreItem
	{
		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.consumable = false;
			Item.rare = ModContent.RarityType<SilvaLime>();
		}

		// Add when silva has a trophy
		/* public override void AddRecipes()
		{
			CreateRecipe().
				AddIngredient<SilvaTrophy>().
				AddTile(TileID.Bookcases).
				Register();
		} */
	}
}

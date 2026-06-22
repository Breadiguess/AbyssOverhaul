using CalamityMod.Items.LoreItems;
using CalamityMod.Rarities;

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
			Item.rare = ModContent.RarityType<PureGreen>(); // TODO: Silva rarity for her items/related items
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

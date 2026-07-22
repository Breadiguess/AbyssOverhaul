using AbyssOverhaul.Content.Rarities;

namespace AbyssOverhaul.Content.Items.Armor.AncientSilva
{
	[AutoloadEquip(EquipType.Legs)]
	public class AncientSilvaLeggings : ModItem
	{
		public override string Texture => "CalamityMod/Items/Armor/Silva/SilvaLeggings";

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 18;
			Item.vanity = true;
			Item.value = Item.buyPrice(gold: 10);
			Item.rare = ModContent.RarityType<SilvaLime>();
		}
	}
}

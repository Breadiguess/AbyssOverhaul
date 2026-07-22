using AbyssOverhaul.Content.Rarities;

namespace AbyssOverhaul.Content.Items.Armor.AncientSilva
{
	[AutoloadEquip(EquipType.Body)]
	public class AncientSilvaArmor : ModItem
	{
		public override string Texture => "CalamityMod/Items/Armor/Silva/SilvaArmor";

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.vanity = true;
			Item.value = Item.buyPrice(gold: 10);
			Item.rare = ModContent.RarityType<SilvaLime>();
		}
	}
}

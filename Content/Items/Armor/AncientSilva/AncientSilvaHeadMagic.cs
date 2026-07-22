using AbyssOverhaul.Content.Rarities;

namespace AbyssOverhaul.Content.Items.Armor.AncientSilva
{
	[AutoloadEquip(EquipType.Head)]
	public class AncientSilvaHeadMagic : ModItem
	{
		public override string Texture => "CalamityMod/Items/Armor/Silva/SilvaHeadMagic";

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 22;
			Item.vanity = true;
			Item.value = Item.buyPrice(gold: 10);
			Item.rare = ModContent.RarityType<SilvaLime>();
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) => body.type == ModContent.ItemType<AncientSilvaArmor>() && legs.type == ModContent.ItemType<AncientSilvaLeggings>();

		public override void ArmorSetShadows(Player player) => player.armorEffectDrawShadow = true;
	}
}

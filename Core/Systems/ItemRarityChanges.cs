using AbyssOverhaul.Content.Rarities;
using CalamityMod.Items.Accessories.Wings;
using CalamityMod.Items.Armor.Silva;
using CalamityMod.Items.Armor.Vanity;
using CalamityMod.Items.Placeables;
using CalamityMod.Items.Placeables.Furniture.Monoliths;
using CalamityMod.Items.SummonItems;

namespace AbyssOverhaul.Core.Systems
{
	internal class ItemRarityChanges : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public override bool AppliesToEntity(Item entity, bool lateInstantiation)
		{
			//Maybe change these to itemID sets?
			return entity.type == ModContent.ItemType<Terminus>() || entity.type == ModContent.ItemType<Rock>() || entity.type == ModContent.ItemType<BossRushMonolith>()
				|| entity.type == ModContent.ItemType<SilvaArmor>() || entity.type == ModContent.ItemType<SilvaLeggings>() || entity.type == ModContent.ItemType<SilvaMask>() || entity.type == ModContent.ItemType<SilvaHornedHelm>() || entity.type == ModContent.ItemType<SilvaHeadMagic>() || entity.type == ModContent.ItemType<SilvaHeadSummon>() || entity.type == ModContent.ItemType<SilvaWings>() || entity.type == ModContent.ItemType<SilvaHelm>();
		}

		public override void SetDefaults(Item entity)
		{
			if (entity.type == ModContent.ItemType<Terminus>() ||
				entity.type == ModContent.ItemType<Rock>() ||
				entity.type == ModContent.ItemType<BossRushMonolith>())
			{
				entity.rare = ModContent.RarityType<PrimordialYellow>();
			}

			if (entity.type == ModContent.ItemType<SilvaArmor>() ||
				entity.type == ModContent.ItemType<SilvaLeggings>() ||
				entity.type == ModContent.ItemType<SilvaMask>() ||
				entity.type == ModContent.ItemType<SilvaHornedHelm>() ||
				entity.type == ModContent.ItemType<SilvaHeadMagic>() ||
				entity.type == ModContent.ItemType<SilvaHeadSummon>() ||
				entity.type == ModContent.ItemType<SilvaWings>() ||
				entity.type == ModContent.ItemType<SilvaHelm>())
			{
				entity.rare = ModContent.RarityType<SilvaLime>();
			}
		}
	}
}

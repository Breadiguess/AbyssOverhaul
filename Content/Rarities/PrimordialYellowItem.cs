using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Placeables;
using CalamityMod.Items.Placeables.Furniture.Monoliths;
using CalamityMod.Items.SummonItems;

namespace AbyssOverhaul.Content.Rarities
{
	internal class PrimordialYellowItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public override bool AppliesToEntity(Item entity, bool lateInstantiation)
		{
			return entity.type == ModContent.ItemType<Terminus>() || entity.type == ModContent.ItemType<Rock>() | entity.type == ModContent.ItemType<BossRushMonolith>();
		}

		public override void SetDefaults(Item entity)
		{
			entity.rare = ModContent.RarityType<PrimordialYellow>();
		}
	}
}

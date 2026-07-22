using CalamityMod;
using CalamityMod.Items.Armor.Silva;
using CalamityMod.Items.Armor.Vanity;
using MonoMod.Cil;
using System.Reflection;

namespace AbyssOverhaul.Content.Items.Armor.AncientSilva
{
	public class SilvaVanityEdits : ILoadable
	{
		public void Load(Mod mod)
		{
			LoadSilvaIsArmorSetDetour<SilvaMask>(nameof(SilvaMask.IsArmorSet));
			LoadSilvaIsArmorSetDetour<SilvaHelm>(nameof(SilvaHelm.IsArmorSet));
			LoadSilvaIsArmorSetDetour<SilvaHornedHelm>(nameof(SilvaHornedHelm.IsArmorSet));

			MonoModHooks.Modify(
					typeof(ItemLoader).GetMethod(nameof(ItemLoader.FinishSetup), BindingFlags.Static | BindingFlags.NonPublic),
					ModifyItemNamesILEdit
				);
		}

		private static void LoadSilvaIsArmorSetDetour<T>(string silvaIsArmorSetName) where T : ModItem
		{
			MonoModHooks.Add(
					typeof(T).GetMethod(silvaIsArmorSetName, BindingFlags.Public | BindingFlags.Instance),
					(Func<T, Item, Item, Item, bool> orig, T self, Item head, Item body, Item legs) =>
					{
						orig.Invoke(self, head, body, legs);
						return ReplaceIsArmorSet(body, legs);
					}
				);
		}

		internal static bool ReplaceIsArmorSet(Item body, Item legs) => body.type == ModContent.ItemType<AncientSilvaArmor>() && legs.type == ModContent.ItemType<AncientSilvaLeggings>();

		internal static void ModifyItemNamesILEdit(ILContext il)
		{
			ILCursor c = new(il);

			int modItem_varNum = -1;

			if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc(out modItem_varNum), i => i.MatchCallvirt<ModItem>("get_" + nameof(ModItem.DisplayName))))
				Log?.Warn("IL Edit: Failed to find where Modded Items set their names");
			else
			{
				c.EmitLdloc(modItem_varNum);
				c.EmitDelegate((LocalizedText originalDisplayName, ModItem item) =>
				{
					if (item.Name == nameof(SilvaHelm) || item.Name == nameof(SilvaHornedHelm) || item.Name == nameof(SilvaMask))
						return Language.GetOrRegister("Mods." + nameof(AbyssOverhaul) + ".Items.Ancient" + item.Name + ".DisplayName", item.PrettyPrintName);
					return originalDisplayName;
				});
			}
		}

		public void Unload() { }
	}

	public class SilvaShopEditor : GlobalNPC
	{
		public override void ModifyShop(NPCShop shop)
		{
			if (shop.NpcType == NPCID.Clothier)
			{
				shop.InsertBefore(ModContent.ItemType<SilvaHelm>(), ModContent.ItemType<AncientSilvaLeggings>(), CalamityConditions.DownedDevourerOfGods);
				shop.InsertAfter(ModContent.ItemType<AncientSilvaLeggings>(), ModContent.ItemType<AncientSilvaArmor>(), CalamityConditions.DownedDevourerOfGods);
				shop.InsertAfter(ModContent.ItemType<SilvaMask>(), ModContent.ItemType<AncientSilvaHeadMagic>(), CalamityConditions.DownedDevourerOfGods);
				shop.InsertAfter(ModContent.ItemType<AncientSilvaHeadMagic>(), ModContent.ItemType<AncientSilvaHeadSummon>(), CalamityConditions.DownedDevourerOfGods);
			}
		}
	}
}

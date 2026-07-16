using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.Summon;
using CalamityMod.Cooldowns;
using CalamityMod.Items.Armor.Silva;
using CalamityMod.Projectiles.Summon;
using MonoMod.Cil;
using System.Reflection;
using Terraria.ModLoader;

namespace AbyssOverhaul.Content.Items.Armor.Silva
{
	public class SilvaArmorRetextures : GlobalItem
	{
		internal static Asset<Texture2D> SilvaArmorArms;
		internal static Asset<Texture2D> SilvaArmorMannequinArms;
		internal static Asset<Texture2D> SilvaLeggingsSkirt;
		internal static Asset<Texture2D> SilvaLeggingsSkirtBack;

		public override void Load()
		{
			if (!Main.dedServ)
			{
				SilvaArmorArms = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaArmor_Arms");
				SilvaArmorMannequinArms = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaArmor_MannequinArms");
				SilvaLeggingsSkirt = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaLeggings_Skirt");
				SilvaLeggingsSkirtBack = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaLeggings_SkirtBack");
			}

			IL_PlayerDrawLayers.DrawPlayer_12_SkinComposite_BackArmShirt += SilvaArmorReplaceArms;
			IL_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite += SilvaArmorReplaceArms;

			MonoModHooks.Modify(
				typeof(SilvaRevive).GetProperty(
					nameof(SilvaRevival.Texture),
					BindingFlags.Instance | BindingFlags.Public
				).GetGetMethod(),
				SilvaCooldownReplacement
			);
		}

		private void SilvaCooldownReplacement(ILContext il)
		{
			ILCursor c = new(il);

			c.GotoNext(MoveType.After, i => i.MatchLdstr(out _));
			c.EmitDelegate((string originalPath) =>
			{
				return nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaRevive";
			});
		}

		private void SilvaArmorReplaceArms(ILContext il)
		{
			ILCursor c = new(il);

			while (c.TryGotoNext(MoveType.After, 
				i => i.MatchLdsfld(typeof(TextureAssets), nameof(TextureAssets.Players)),
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<PlayerDrawSet>(nameof(PlayerDrawSet.skinVar)),
				i => i.MatchLdcI4(7),
				i => i.MatchCall<Asset<Texture2D>[,]>("Get"),
				i => i.MatchCallvirt<Asset<Texture2D>>("get_Value")
				))
			{
				c.EmitLdarg(0);
				c.EmitDelegate((Texture2D baseArmSkin, ref PlayerDrawSet drawinfo) =>
				{
					Texture2D armSkin = baseArmSkin;
					Player player = drawinfo.drawPlayer;
					if (player.body == ModContent.GetInstance<SilvaArmor>().Item.bodySlot)
					{
						if (drawinfo.skinVar == 10 || drawinfo.skinVar == 11)
							armSkin = SilvaArmorMannequinArms.Value;
						else
							armSkin = SilvaArmorArms.Value;
					}
					return armSkin;
				});
			}
		}

		public override void SetStaticDefaults()
		{
			TextureAssets.Buff[ModContent.BuffType<SilvaCrystalBuff>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaCrystalBuff");
			TextureAssets.Buff[ModContent.BuffType<SilvaRevival>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaRevival");
			TextureAssets.Projectile[ModContent.ProjectileType<SilvaCrystal>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaCrystal");

			TextureAssets.Item[ModContent.ItemType<SilvaArmor>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaArmor");
			TextureAssets.Item[ModContent.ItemType<SilvaHeadMagic>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaHeadMagic");
			TextureAssets.Item[ModContent.ItemType<SilvaHeadSummon>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaHeadSummon");
			TextureAssets.Item[ModContent.ItemType<SilvaLeggings>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaLeggings");

			TextureAssets.ArmorBodyComposite[ModContent.GetInstance<SilvaArmor>().Item.bodySlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaArmor_Body");
			TextureAssets.ArmorHead[ModContent.GetInstance<SilvaHeadMagic>().Item.headSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaHeadMagic_Head");
			TextureAssets.ArmorHead[ModContent.GetInstance<SilvaHeadSummon>().Item.headSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaHeadSummon_Head");
			TextureAssets.ArmorLeg[ModContent.GetInstance<SilvaLeggings>().Item.legSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaLeggings_Legs");

			ArmorIDs.Head.Sets.IsTallHat[ModContent.GetInstance<SilvaHeadSummon>().Item.headSlot] = true;
		}


		public static void RenderSilvaLegs(ref PlayerDrawSet drawInfo, Texture2D assetToUse)
		{
			if (drawInfo.drawPlayer.legs != ModContent.GetInstance<SilvaLeggings>().Item.legSlot)
				return;

			if (drawInfo.isSitting)
			{
				if (!PlayerDrawLayers.ShouldOverrideLegs_CheckShoes(ref drawInfo) || drawInfo.drawPlayer.wearsRobe)
				{
					if (!drawInfo.drawPlayer.invis)
					{
						PlayerDrawLayers.DrawSittingLegs(ref drawInfo, assetToUse, drawInfo.colorArmorLegs, drawInfo.cLegs);
					}
				}
			}
			else
			{
				if (!PlayerDrawLayers.ShouldOverrideLegs_CheckShoes(ref drawInfo))
				{
					if (drawInfo.drawPlayer.invis)
					{
						return;
					}
					DrawData item2 = new DrawData(assetToUse, drawInfo.legsOffset + new Vector2((float)(int)(drawInfo.Position.X - Main.screenPosition.X - (float)(drawInfo.drawPlayer.legFrame.Width / 2) + (float)(drawInfo.drawPlayer.width / 2)), (float)(int)(drawInfo.Position.Y - Main.screenPosition.Y + (float)drawInfo.drawPlayer.height - (float)drawInfo.drawPlayer.legFrame.Height + 4f)) + drawInfo.drawPlayer.legPosition + drawInfo.legVect, drawInfo.drawPlayer.legFrame, drawInfo.colorArmorLegs, drawInfo.drawPlayer.legRotation, drawInfo.legVect, 1f, drawInfo.playerEffect);
					item2.shader = drawInfo.cLegs;
					drawInfo.DrawDataCache.Add(item2);
				}
			}
		}
	}

	public class SilvaLeggingSkirt : PlayerDrawLayer
	{
		public override Position GetDefaultPosition()
		{
			return new AfterParent(PlayerDrawLayers.Leggings);
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			SilvaArmorRetextures.RenderSilvaLegs(ref drawInfo, SilvaArmorRetextures.SilvaLeggingsSkirt.Value);
		}
	}

	public class SilvaLeggingSkirtBack : PlayerDrawLayer
	{
		public override Position GetDefaultPosition()
		{
			return new BeforeParent(PlayerDrawLayers.Leggings);
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			SilvaArmorRetextures.RenderSilvaLegs(ref drawInfo, SilvaArmorRetextures.SilvaLeggingsSkirtBack.Value);
		}
	}
}

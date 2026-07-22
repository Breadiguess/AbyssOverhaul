using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.Summon;
using CalamityMod.Cooldowns;
using CalamityMod.Items.Accessories.Wings;
using CalamityMod.Items.Armor.Silva;
using CalamityMod.Projectiles.Summon;
using MonoMod.Cil;
using System.Reflection;

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
			On_PlayerDrawLayers.DrawPlayer_09_Wings += RerenderSilvaWings;

			PropertyInfo? cooldownTextureGetter = typeof(SilvaRevive).GetProperty(nameof(SilvaRevival.Texture), BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo? cooldownOutlineColorGetter = typeof(SilvaRevive).GetProperty("OutlineColor", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo? cooldownStartColoreGetter = typeof(SilvaRevive).GetProperty("CooldownStartColor", BindingFlags.Instance | BindingFlags.Public);
			PropertyInfo? cooldownEndColoreGetter = typeof(SilvaRevive).GetProperty("CooldownEndColor", BindingFlags.Instance | BindingFlags.Public);

			ApplyGetPropertyDetour(cooldownTextureGetter, SilvaCooldownTexture);
			ApplyGetPropertyDetour(cooldownOutlineColorGetter, SilvaCooldownOutline);
			ApplyGetPropertyDetour(cooldownStartColoreGetter, SilvaCooldownStartColor);
			ApplyGetPropertyDetour(cooldownEndColoreGetter, SilvaCooldownEndColor);
		}

		internal static void ApplyGetPropertyDetour(PropertyInfo? propertyToDetour, Delegate detourDelegate)
		{
			if (propertyToDetour != null)
				MonoModHooks.Add(
					propertyToDetour.GetGetMethod(),
					detourDelegate
				);
		}

		// Replaces the silva cooldown properties for texture and colors
		// Normally, you use orig to avoid completely replacing the original code, but in this instance we're swapping out the whole method code with our own single line
		// For future reference, we're doing something thats normally HEAVILY DISCOURAGED. Make sure you always call orig in your detours, these are the exception
		internal static string SilvaCooldownTexture(Func<SilvaRevive, string> orig, SilvaRevive self) => nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaRevive";

		internal static Color SilvaCooldownOutline(Func<SilvaRevive, Color> orig, SilvaRevive self) => new(244, 254, 117);

		internal static Color SilvaCooldownStartColor(Func<SilvaRevive, Color> orig, SilvaRevive self) => new(182, 132, 64);

		internal static Color SilvaCooldownEndColor(Func<SilvaRevive, Color> orig, SilvaRevive self) => new(244, 254, 117);

		internal static void RerenderSilvaWings(On_PlayerDrawLayers.orig_DrawPlayer_09_Wings orig, ref PlayerDrawSet drawinfo)
		{
			// Renders the custom wing animation
			// Based off of the normal player wing rendering
			// Removes the need of having a "_Real" asset for wings by injecting directly into the wing rendering on the player
			// Honestly I'm surprised Calamity devs don't already do this, especially since it cuts the wing rendering calcs/time in half
			if (drawinfo.drawPlayer.wings == ModContent.GetInstance<SilvaWings>().Item.wingSlot)
			{
				if (drawinfo.drawPlayer.dead || drawinfo.hideEntirePlayer)
				{
					return;
				}

				Vector2 directions = drawinfo.drawPlayer.Directions;
				Vector2 playerOffset = new(0f, 7f);
				Vector2 playerPos = drawinfo.Position - Main.screenPosition + new Vector2(drawinfo.drawPlayer.width / 2, drawinfo.drawPlayer.height - drawinfo.drawPlayer.bodyFrame.Height / 2) + playerOffset;
				Main.instance.LoadWings(drawinfo.drawPlayer.wings);

				Asset<Texture2D> wingTexture = TextureAssets.Wings[drawinfo.drawPlayer.wings];

				Vector2 wingPosition = playerPos + new Vector2(-9, 2) * directions;
				wingPosition = wingPosition.Floor();

				const int frameCount = 9;

				Rectangle? wingFrame = (Rectangle?)new Rectangle(
					0,
					wingTexture.Height() / frameCount * drawinfo.drawPlayer.wingFrame,
					wingTexture.Width(),
					wingTexture.Height() / frameCount
					);

				Vector2 wingOrigin = new(
					wingTexture.Width() / 2, 
					wingTexture.Height() / frameCount / 2
					);

				Color wingColor = drawinfo.colorArmorBody;

				DrawData item = new(wingTexture.Value, wingPosition, wingFrame, wingColor, drawinfo.drawPlayer.bodyRotation, wingOrigin, 1f, drawinfo.playerEffect, 0f)
				{
					shader = drawinfo.cWings
				};
				drawinfo.DrawDataCache.Add(item);
				return;
			}
			orig.Invoke(ref drawinfo);
		}

		// Replaces the arm graphic on players when they wear the silva armor by injecting into all cases where arms are rendered
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
			TextureAssets.Item[ModContent.ItemType<SilvaWings>()] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaWings");

			TextureAssets.ArmorBodyComposite[ModContent.GetInstance<SilvaArmor>().Item.bodySlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaArmor_Body");
			TextureAssets.ArmorHead[ModContent.GetInstance<SilvaHeadMagic>().Item.headSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaHeadMagic_Head");
			TextureAssets.ArmorHead[ModContent.GetInstance<SilvaHeadSummon>().Item.headSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaHeadSummon_Head");
			TextureAssets.ArmorLeg[ModContent.GetInstance<SilvaLeggings>().Item.legSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaLeggings_Legs");
			TextureAssets.Wings[ModContent.GetInstance<SilvaWings>().Item.wingSlot] = ModContent.Request<Texture2D>(nameof(AbyssOverhaul) + "/Content/Items/Armor/Silva/SilvaWings_Wings");

			ArmorIDs.Head.Sets.IsTallHat[ModContent.GetInstance<SilvaHeadSummon>().Item.headSlot] = true;
		}


		public override bool WingUpdate(int wings, Player player, bool inUse)
		{
			if (wings == ModContent.GetInstance<SilvaWings>().Item.wingSlot)
			{
				int frameDuration = 3; // FPS before the wings change frame
				int frameCount = 9; // Max frame count of the wings

				const int startingFlapFrame = 1;
				const int standingFrame = 0;
				const int fallingFrame = 1;
				const int glidingFrame = 2; // For when the player is holding space/gliding
				const int floatingFrame = 0; // Frame for when the player is floating ontop of water

				// Flying
				if (inUse || player.jump > 0)
				{
					player.wingFrameCounter++;
					if (player.wingFrameCounter > frameDuration)
					{
						player.wingFrame++;
						player.wingFrameCounter = 0;
						if (player.wingFrame >= frameCount)
						{
							player.wingFrame = startingFlapFrame;
						}
					}
				}
				// Gliding/Falling
				else if (player.velocity.Y != 0f)
				{
					player.wingFrame = fallingFrame;

					if (player.controlJump && player.velocity.Y > 0)
						player.wingFrame = glidingFrame;

					if (player.ShouldFloatInWater && player.wet)
						player.wingFrame = floatingFrame;
				}
				// Standing
				else
					player.wingFrame = standingFrame;

				return true;
			}

			return base.WingUpdate(wings, player, inUse);
		}

		public static void RenderSilvaLegs(ref PlayerDrawSet drawInfo, Texture2D assetToUse)
		{
			if (drawInfo.drawPlayer.legs != ModContent.GetInstance<SilvaLeggings>().Item.legSlot || drawInfo.drawPlayer.invis)
				return;

			if (drawInfo.isSitting)
			{
				if (!PlayerDrawLayers.ShouldOverrideLegs_CheckShoes(ref drawInfo) || drawInfo.drawPlayer.wearsRobe)
				{
					PlayerDrawLayers.DrawSittingLegs(ref drawInfo, assetToUse, drawInfo.colorArmorLegs, drawInfo.cLegs);
				}
			}
			else
			{
				if (!PlayerDrawLayers.ShouldOverrideLegs_CheckShoes(ref drawInfo))
				{
					// position offset casts to int as the legging rendering also does so to clamp to where the player is positioned. Otherwise the skirt will lag behind
					Vector2 legPosition = drawInfo.legsOffset + new Vector2(
						(int)(drawInfo.Position.X - Main.screenPosition.X - (drawInfo.drawPlayer.legFrame.Width / 2) + (drawInfo.drawPlayer.width / 2)), 
						(int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.legFrame.Height + 4f)
						)
					+ drawInfo.drawPlayer.legPosition + drawInfo.legVect;

					DrawData item = new(assetToUse, legPosition, drawInfo.drawPlayer.legFrame, drawInfo.colorArmorLegs, drawInfo.drawPlayer.legRotation, drawInfo.legVect, 1f, drawInfo.playerEffect)
					{
						shader = drawInfo.cLegs
					};
					drawInfo.DrawDataCache.Add(item);
				}
			}
		}
	}

	public class SilvaLeggingSkirt : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Torso);

		protected override void Draw(ref PlayerDrawSet drawInfo) => SilvaArmorRetextures.RenderSilvaLegs(ref drawInfo, SilvaArmorRetextures.SilvaLeggingsSkirt.Value);
	}

	public class SilvaLeggingSkirtBack : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Leggings);

		protected override void Draw(ref PlayerDrawSet drawInfo) => SilvaArmorRetextures.RenderSilvaLegs(ref drawInfo, SilvaArmorRetextures.SilvaLeggingsSkirtBack.Value);
	}
}

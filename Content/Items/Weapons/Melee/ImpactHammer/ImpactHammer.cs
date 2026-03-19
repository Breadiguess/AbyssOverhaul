using AbyssOverhaul.Content.Rarities;
using CalamityMod;
using CalamityMod.CustomRecipes;
using CalamityMod.Dusts;
using CalamityMod.Items;
using CalamityMod.Items.BaseItems;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rail;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    public class ImpactHammer : ModProjectile, ILocalizedModType
    {
        public ref float TimeAlive => ref Projectile.ai[1];
        public ref Player Owner => ref Main.player[Projectile.owner];
        public static string Path => ModContent.GetInstance<ImpactHammer>().GetPath();
        public new string LocalizationCategory => "Items.Weapons.Melee";

        public int ShakingLevel = 0;
        public static Asset<Texture2D> HeadTex;
        public static Asset<Texture2D> ArmTex;
        public enum State
        {
            Idle,
            Charging,
            Hitting,
        }
        public State CurrentState = State.Idle;

        public override void Load()
        {
            HeadTex = ModContent.Request<Texture2D>($"{ImpactHammer.Path}Head");
            ArmTex = ModContent.Request<Texture2D>($"{ImpactHammer.Path}Arm");
        }
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 24; 
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return base.PreDraw(ref lightColor);
        }
        public override void OnSpawn(IEntitySource source) 
        {
            base.OnSpawn(source);
        }
        public override void AI()
        {
            TimeAlive++;
            DoPlayerCheck();
            Vector2 TruePosition = Owner.Center;
            Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.Center.AngleTo(Owner.Calamity().mouseWorld), 0.5f);

            if (ShakingLevel > 0)
            {
                Projectile.position = TruePosition + new Vector2(ShakingLevel * 0.5f, 0).RotatedByRandom(Math.PI * 2);
            }
            else
            {
                Projectile.position = TruePosition;
            }

        }

        private void DoPlayerCheck()
        {
            if (Owner.HeldItem.type == ModContent.ItemType<ImpactHammerItem>())
            {
                Projectile.timeLeft = 2;
            }
        }
    }
}
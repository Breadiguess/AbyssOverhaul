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
    public class ImpactHammerItem : ModItem, ILocalizedModType
    {
        public static string Path => ModContent.GetInstance<ImpactHammerItem>().GetPath();
        public new string LocalizationCategory => "Items.Weapons.Melee";
        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 36;
            Item.damage = 50;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useTurn = true;
            Item.knockBack = 12f;
            Item.autoReuse = true;
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.rare = ModContent.RarityType<AbyssalRarity>();
            Item.useStyle = ItemUseStyleID.MowTheLawn;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<ImpactHammer>();
            Item.noUseGraphic = true;
            Item.noMelee = true;
        }
        public override bool MeleePrefix() => true;
        //public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0; not sure if this is needed
        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[Item.shoot] <= 0 && player.HeldItem.type == ModContent.ItemType<ImpactHammerItem>())
            {
                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<ImpactHammer>(), 0, 0);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(12)
                .AddIngredient<DubiousPlating>(12)
                //.AddIngredient(ItemID. TIER-APPROPRIATE MATERIAL ,12)
                //.AddCondition(ArsenalTierGatedRecipe.ConstructRecipeCondition( THE NEW TIER THAT WE MIGHT ADD, out Func<bool> condition), condition)
                .AddTile(TileID.MythrilAnvil);
        }
      
        
    }
}

using CalamityMod;
using CalamityMod.Items.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.BrigandsCalling
{
    public class BrigandsCalling_Item : ModItem
    {
        public override string LocalizationCategory => "Items.Weapons.Ranged";
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.damage = 120;

            Item.crit = 5;
            Item.useAmmo = AmmoID.Bullet;

            Item.shoot = ProjectileID.Bullet;
            Item.useTime = 7;
            Item.useAnimation = 7;

            Item.useStyle = ItemUseStyleID.HiddenAnimation;
            Item.noMelee = true;
            Item.noUseGraphic = true;


        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            
            return base.CanConsumeAmmo(ammo, player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new("Reference", Language.GetTextValue("Mods.AbyssOverhaul.Items.Weapons.Ranged.BrigandsCalling_Item.FunReference")));
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            BrigandsCalling_Player modPlayer = player.GetModPlayer<BrigandsCalling_Player>();

            if (player.altFunctionUse == 2)
            {
                Item.useTime = 12;
                Item.useAnimation = 12;
                Item.noUseGraphic = true;
                Item.noMelee = true;
                Item.shoot = ProjectileID.None;

                return modPlayer.TryStartFlipDashFromMouse();
            }


            return base.AltFunctionUse(player);
        }
        public override void HoldItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;
            player.Calamity().mouseWorldListener = true;




            if (player.whoAmI != Main.myPlayer)
                return;

            if (player.dead || !player.active)
                return;

          


            int heldType = ModContent.ProjectileType<BrigandsCalling_Held>();

            bool hasRightHand = false;
            bool hasLeftHand = false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];

                if (!proj.active)
                    continue;

                if (proj.owner != player.whoAmI)
                    continue;

                if (proj.type != heldType)
                    continue;

                if (proj.ai[1] == 0f)
                    hasRightHand = true;
                else if (proj.ai[1] == 1f)
                    hasLeftHand = true;
            }

            if (player.HeldItem.type == Type)
            {
                if (!hasRightHand)
                {
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        player.Center,
                        Vector2.Zero,
                        heldType,
                        Item.damage,
                        Item.knockBack,
                        player.whoAmI,
                        0f,
                        0f
                    );
                }

                if (!hasLeftHand)
                {
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        player.Center,
                        Vector2.Zero,
                        heldType,
                        Item.damage,
                        Item.knockBack,
                        player.whoAmI,
                        0f,
                        1f
                    );
                }
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<MysteriousCircuitry>(12)
                .AddIngredient<DubiousPlating>(12)
                .AddIngredient<PerennialBar>(18)
                //.AddIngredient(ItemID. TIER-APPROPRIATE MATERIAL ,12)
                //.AddCondition(ArsenalTierGatedRecipe.ConstructRecipeCondition( THE NEW TIER THAT WE MIGHT ADD, out Func<bool> condition), condition)
                .AddTile(TileID.MythrilAnvil);
        }
    }
}

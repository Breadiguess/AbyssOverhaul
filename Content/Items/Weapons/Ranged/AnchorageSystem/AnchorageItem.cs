using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.GameContent.UI;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.AnchorageSystem
{
    internal class AnchorageItem : ModItem
    {

        public new string LocalizationCategory => "Items.Weapon.Ranged";

        // SetDefaults sets up the values of the item (i.e rarity, damage, weapon class, what projectiles it shoots etc)
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ItemRarityID.Green;
            Item.shoot = ModContent.ProjectileType<AnchorageHeld>();
        }

        public override bool RangedPrefix() => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return false;
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            if (player.dead || !player.active)
                return;

            int hammerType = Item.shoot;

            if (player.HeldItem.type == Type && player.ownedProjectileCounts[hammerType] <= 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    hammerType,
                    Item.damage,
                    Item.knockBack,
                    player.whoAmI
                );
            }
        }
    }
}

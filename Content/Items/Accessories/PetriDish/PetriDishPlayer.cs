using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Accessories.PetriDish
{
    public class PetriDishPlayer : ModPlayer
    {
        public bool Active = false;

        public override void ResetEffects()
        {
            Active = false;
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if(Active)
            if (item.DamageType == DamageClass.Ranged && Main.rand.NextBool())
            {
                Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.2f), ModContent.ProjectileType<MicrobeGlob>(), damage / 4, 1, Player.whoAmI);
            }
            return base.Shoot(item, source, position, velocity, type, damage, knockback);
        }

    }
}

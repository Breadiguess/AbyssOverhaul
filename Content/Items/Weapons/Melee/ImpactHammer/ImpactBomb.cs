using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactBomb : ModProjectile
    {
        public bool isArmed
        {
            get => Projectile.ai[0] > ArmingTime;
        }
        public const int ArmingTime = 40;
        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.2f;
            if (Projectile.ai[0]++ > ArmingTime)
            {
                Projectile.tileCollide = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (isArmed)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis("ImpactBombExplosion"), Projectile.Center, Vector2.zeroVector, ModContent.ProjectileType<ImpactBomb_Explosion>(), this.Projectile.damage, 0);
                Projectile.active = false;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (isArmed)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis("ImpactBombExplosion"), Projectile.Center, Vector2.zeroVector, ModContent.ProjectileType<ImpactBomb_Explosion>(), this.Projectile.damage, 0);
                Projectile.active = false;
            }   
        }



        public override bool PreDraw(ref Color lightColor)
        {
            //var tex = Assets.Textures


            return base.PreDraw(ref lightColor);
        }

    }
}

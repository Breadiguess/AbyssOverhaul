using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.AnchorageSystem
{
    internal class AnchorageHeld_Drill : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }

        private Projectile OwnerDrill;
        public override void OnSpawn(IEntitySource source)
        {
            if (source.Context is not null && source.Context.Equals("AnchorageDrill"))
            {
                if (source is EntitySource_Parent parent && parent.Entity is not null)
                {
                    OwnerDrill = (Projectile)parent.Entity;
                }
            }
        }

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.Size = new(20);
            Projectile.DamageType = DamageClass.Ranged;

        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (OwnerDrill is not null && OwnerDrill.type == ModContent.ProjectileType<AnchorageHeld>())
            {
                if(OwnerDrill.As<AnchorageHeld>().rope is not null)
                {
                    OwnerDrill.As<AnchorageHeld>().rope.Positions[^1] = Projectile.Center;
                }
            }

            var tex = TextureAssets.Projectile[Type].Value;

            Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, (int)(Main.GameUpdateCount % Main.projFrames[Type]));

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos, frame, lightColor, Projectile.rotation - MathHelper.PiOver2, frame.Size()/2, Projectile.scale, 0);

            return false;
        }

    }
}

using CalamityMod;
using CalamityMod.Projectiles.Magic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.GameContent.Events;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.AnchorageSystem
{
    internal class AnchorageHeld : ModProjectile
    {

        public ref Player Owner => ref Main.player[Projectile.owner];
        public bool hasDrill;
        public static Asset<Texture2D> drillTex;
        public enum state
        {
            Idle,
            Charging,
            Firing,
            Recovering,
            Rest
        }

        public state currentState;

        public override void Load()
        {
            string path = this.GetPath();
            drillTex = ModContent.Request<Texture2D>($"{path}_Drill");

            base.Load();
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void AI()
        {
            CheckPlayer();

            Projectile.Center = Owner.Center;
            Projectile.velocity = Owner.DirectionTo(Owner.Calamity().mouseWorld);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        private void CheckPlayer()
        {
            if (Owner.HeldItem.type == ModContent.ItemType<AnchorageItem>() && !Owner.dead)
            {
                Projectile.timeLeft = 2;
                Owner.heldProj = this.Projectile.whoAmI;
                Owner.direction = Projectile.velocity.X.DirectionalSign();
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = TextureAssets.Projectile[Type].Value;
            Vector2 offset = new(0, tex.Height);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, Projectile.rotation, tex.Size()/2, Projectile.scale, 0);

            drawHead(ref lightColor, offset);

            return false;
        }

        private void drawHead(ref Color lightColor, Vector2 offset)
        {
            var tex = drillTex.Value;

            Rectangle frame = tex.Frame(1, Main.projFrames[ModContent.ProjectileType<AnchorageHeld_Drill>()]); 

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos + offset.RotatedBy(Projectile.rotation), frame, lightColor, Projectile.rotation, frame.Size()/2, Projectile.scale, 0);
        }

    }
}

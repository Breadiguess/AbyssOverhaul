using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.BehaviorOverrides.GooGazerNPC
{
    internal class ConcussiveBlast : ModProjectile, IDrawPixellated
    {
        public override string Texture => Assets.Textures.Extra.HollowCircleHardEdge.KEY;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.Size = new(40);
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.friendly = false;
        }
        public override void AI()
        { 
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = 1-Utilities.InverseLerp(0, 60, Projectile.timeLeft);//Utilities.InverseLerpBump(0, 35, 35, 60, Projectile.timeLeft);
            Projectile.Opacity = Utilities.InverseLerp(0, 60, Projectile.timeLeft);
        }
        public PixelLayer PixelLayer => PixelLayer.AboveProjectiles;

        public override bool PreDraw(ref Color lightColor)
        {
            // Utils.DrawRect(Main.spriteBatch, Projectile.Hitbox, Color.White);
            return false;
        }
        public void DrawPixelated(SpriteBatch spriteBatch)
        {

            var tex = TextureAssets.Projectile[Type].Value;


            Vector2 DrawPos = Projectile.Center - Main.screenPosition;
            Color color = Color.Yellow with { A = 0 } * Projectile.Opacity;
            Vector2 Scale = new Vector2(0.4f, 1) * Projectile.scale;

            Main.EntitySpriteDraw(tex, DrawPos, null, color, Projectile.rotation, tex.Size() / 2f, Scale, SpriteEffects.None);


        }
    }
}

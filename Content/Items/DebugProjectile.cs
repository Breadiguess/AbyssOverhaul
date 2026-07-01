using CalamityMod;

namespace AbyssOverhaul.Content.Items
{
    internal class DebugProjectile : ModProjectile
    {
        public ref Player Owner => ref Main.player[Projectile.owner];
        public override string Texture => Assets.Textures.T_VoronoiNoiseCA001.KEY;


        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.Size = new Vector2(50, 50);
        }

        public override void AI()
        {
            Projectile.timeLeft++;
            Projectile.Center = Owner.Center;
            Projectile.velocity = Owner.Center.DirectionTo(Owner.Calamity().mouseWorld)*40;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {


            Vector2 DrawPos = Projectile.Center - Main.screenPosition;
      
          
            return false;
        }
    } 
}

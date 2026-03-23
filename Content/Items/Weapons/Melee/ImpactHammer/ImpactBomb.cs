using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactBomb : ModProjectile
    {
        public bool isArmed
        {
            get => Projectile.ai[0] > ArmingTime;
        }
        public const int ArmingTime = 40;

        public bool PlayedArmingSound = false;
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hostile  = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.Size = new Vector2(20);
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.2f;

            if (!PlayedArmingSound && isArmed)
            {
                SoundEngine.PlaySound(Assets.Sounds.Items.Melee.ImpactHammer.BombDeploy.Asset with { MaxInstances = 0, pitchVariance = 0.2f}, Projectile.Center);
                PlayedArmingSound = true;
            }


            if (Projectile.ai[0]++ > ArmingTime)
            {
                Projectile.tileCollide = true;
            }
        }

        private void SpawnBomb()
        {
            if (isArmed)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis("ImpactBombExplosion"), Projectile.Center, Vector2.zeroVector, ModContent.ProjectileType<ImpactBomb_Explosion>(), this.Projectile.damage, 0);
                Projectile.active = false;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SpawnBomb();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnBomb();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnBomb();
            return false;
        }


        public void DrawFlare()
        {
            var tex = Assets.Textures.Flare.T_StarFlare003.Asset.Value;

            float FlareInterpolant = Utilities.InverseLerpBump(ArmingTime - 6, ArmingTime-3, ArmingTime + 6, ArmingTime + 12, Projectile.ai[0]);
            Vector2 DrawPos = Projectile.Center - Main.screenPosition;
            float FlareSpinInterpolant = Utilities.InverseLerp(ArmingTime - 6, ArmingTime + 9, Projectile.ai[0]);


            Utils.DrawBorderString(Main.spriteBatch, FlareInterpolant.ToString(), DrawPos, Color.White);
            Color FlareColor = Color.Yellow with { A = 0 } * FlareInterpolant;
            float rot = MathHelper.Pi * FlareSpinInterpolant;
            Main.EntitySpriteDraw(tex, DrawPos, null, FlareColor, 0, tex.Size() / 2f, new Vector2(0.15f, 0.05f)*FlareSpinInterpolant, 0);

        }
        public override bool PreDraw(ref Color lightColor)
        {
            var tex = TextureAssets.Projectile[Type].Value;

            Vector2 DrawPos = Projectile.Center - Main.screenPosition;


           
            Main.EntitySpriteDraw(tex, DrawPos, null, lightColor, 0, tex.Size() / 2f, Projectile.scale, 0);
            DrawFlare();

            return false;
        }

    }
}

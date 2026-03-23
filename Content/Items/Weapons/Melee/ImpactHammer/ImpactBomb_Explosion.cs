using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.ScreenShake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactBomb_Explosion : ModProjectile
    {
        public bool HasSpawnedParticle
        {
            get => Projectile.ai[0] > 0;
        }
        public override void SetDefaults()
        {
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.Size = new Vector2(400);
            Projectile.tileCollide = false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            SoundEngine.PlaySound(Assets.Sounds.Items.Melee.ImpactHammer.Bomb_Explosion.Asset with { MaxInstances = 0, pitch = 0.5f, pitchVariance = 0.5f}, Projectile.Center);
            SoundEngine.PlaySound(Assets.Sounds.Items.Melee.ImpactHammer.BombDeploy.Asset with { MaxInstances = 0, pitch = 0.5f, pitchVariance = 0.5f }, Projectile.Center);

        }
        public override void AI()
        {
            if (!HasSpawnedParticle)
            {
                ImpactBomb_Particle particle = new();
                particle.Prepare(Projectile.Center);
                ParticleEngine.ShaderParticles.Add(particle);
                Projectile.ai[0]++;
                ScreenShakeSystem.ShakeAt(Projectile.Center, 9, 60, dampingPower:0.9f);
            }
            Projectile.velocity = Vector2.zeroVector;

           
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}

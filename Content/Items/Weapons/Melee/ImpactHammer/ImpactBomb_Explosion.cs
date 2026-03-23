using BreadLibrary.Core.Graphics.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            if (!HasSpawnedParticle)
            {
                ImpactBomb_Particle particle = new();
                particle.Prepare(Projectile.Center);
                ParticleEngine.ShaderParticles.Add(particle);
                    }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}

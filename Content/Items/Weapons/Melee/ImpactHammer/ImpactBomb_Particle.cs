using BreadLibrary.Core.Graphics.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactBomb_Particle : BaseParticle
    {
        public static ParticlePool<ImpactBomb_Particle> pool = new(500, GetNewParticle<ImpactBomb_Particle>);

        public Vector2 Position;
        public void Prepare(Vector2 SpawnPos)
        {
            Position = SpawnPos;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {

        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            
        }
    }
}

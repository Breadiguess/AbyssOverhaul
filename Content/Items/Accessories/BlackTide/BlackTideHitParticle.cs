using BreadLibrary.Core.Graphics.Particles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideHitParticle : BaseParticle
    {
        public static ParticlePool<BlackTideHitParticle> pool = new(500, GetNewParticle<BlackTideHitParticle>);

        public Vector2 Center;
        public int MaxTime = 60;
        public int TimeLeft;
        public void Prepare(Vector2 Pos)
        {
            Center = Pos;
            TimeLeft = MaxTime;
        }
        public override void Update(ref ParticleRendererSettings settings)
        {

            if (TimeLeft-- < 0)
                ShouldBeRemovedFromRenderer = true;
        }
        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            settings.AnchorPosition = Center;
        }
    }
}

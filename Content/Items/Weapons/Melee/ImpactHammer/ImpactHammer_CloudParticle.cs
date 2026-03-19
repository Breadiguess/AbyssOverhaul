using BreadLibrary.Core.Graphics.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactHammer_CloudParticle : BaseParticle
    {
        public static ParticlePool<ImpactHammer_CloudParticle> pool = new(500, GetNewParticle<ImpactHammer_CloudParticle>);

        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public int TimeLeft;
        public int MaxTime;
        public void Prepare(Vector2 Pos, Vector2 Velocity, float Rot, int MaxTime = 60)
        {
            Position = Pos;
            this.Velocity = Velocity;
            Rotation = Rot;
            this.MaxTime = MaxTime;
            TimeLeft = this.MaxTime;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            if (TimeLeft-- <= 0)
                ShouldBeRemovedFromRenderer = true;
            Position += Velocity;
            Velocity *= 0.98f;
        }
        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            var tex = Assets.Textures.Extra.Smoke.Asset.Value;


            Vector2 Scale = new Vector2(0.2f) * Utilities.InverseLerpBump(0, 5, 20, MaxTime, MaxTime - TimeLeft);
            Color color = Color.Gray with { A = 0 } * Utilities.InverseLerpBump(0, 5, 20, MaxTime, MaxTime - TimeLeft);
            Main.EntitySpriteDraw(tex, Position - Main.screenPosition, null, color, Rotation, tex.Size() / 2f, Scale, 0);
        }
    }
}
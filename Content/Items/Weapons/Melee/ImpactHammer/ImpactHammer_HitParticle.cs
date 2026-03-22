using BreadLibrary.Core.Graphics.Particles;
using CalamityMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactHammer_HitParticle: BaseParticle
    {
        public static ParticlePool<ImpactHammer_HitParticle> pool = new(500, GetNewParticle<ImpactHammer_HitParticle>);

        public Vector2 Position;
        public float Rotation;
        public int TimeLeft;
        public int MaxTime;
        public bool IsAReflect = false;
        public void Prepare(Vector2 Pos, float Rot, int MaxTime = 60, bool isAReflect = false)
        {
            Position = Pos;
            Rotation = Rot;
            this.MaxTime = MaxTime;
            TimeLeft = this.MaxTime;
            IsAReflect = isAReflect;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            if (TimeLeft-- <= 0)
                ShouldBeRemovedFromRenderer = true;

            if (IsAReflect)
                Rotation += 0.2f;
        }
        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {

            if (!IsAReflect)
            {
                var tex = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;   


                Vector2 Scale = new Vector2(0.1f, 0.4f) * Utilities.InverseLerpBump(0, 5, 20, MaxTime, MaxTime - TimeLeft);
                Color color = Color.LightGreen with { A = 0 } * Utilities.InverseLerpBump(0, 5, 20, MaxTime, MaxTime - TimeLeft);
                Main.EntitySpriteDraw(tex, Position - Main.screenPosition, null, color, Rotation, tex.Size() / 2f, Scale, 0);

            }
            else
            {
                settings.AnchorPosition = Position;

                var tex = Assets.Textures.Extra.Star.Asset.Value;

                Vector2 Scale = new Vector2(0.1f) * Utilities.InverseLerpBump(0, 5, 20, MaxTime, MaxTime - TimeLeft);
                Color color = Color.Yellow with { A = 0 } * Utilities.InverseLerpBump(0, 5, 20, MaxTime, MaxTime - TimeLeft);


                
                Main.EntitySpriteDraw(tex, Position - Main.screenPosition, null, color, Rotation, tex.Size() / 2f, Scale, 0);
            }
          
        }
    }
}

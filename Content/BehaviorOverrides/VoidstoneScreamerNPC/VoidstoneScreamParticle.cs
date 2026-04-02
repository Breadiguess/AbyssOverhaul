using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Graphics.PixelationShit;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.BehaviorOverrides.VoidstoneScreamerNPC
{
    internal class VoidstoneScreamParticle : BaseParticle<VoidstoneScreamParticle>, IDrawPixellated
    { 
        Vector2 Position;
        Vector2 Velocity;
        Color Color;
        float Scale;
        int TimeLeft = 0;
        int MaxTime;

        public void Prepare(Vector2 position, Vector2 velocity, Color color, float scale, int timeLeft)
        {
            
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            TimeLeft = timeLeft;
            MaxTime = timeLeft;
        }
        public override void Update(ref ParticleRendererSettings settings)
        {
            Position += Velocity;
            TimeLeft--;
            if (TimeLeft <= 0)
                ShouldBeRemovedFromRenderer = true;
        }
        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {

            Texture2D tex = Assets.Textures.Burst.T_Burst048.Asset.Value;

            Vector2 drawPos = Position - Main.screenPosition;

            float Opacity = Utilities.InverseLerp(0, MaxTime, TimeLeft);
            Main.EntitySpriteDraw(tex, drawPos, null, Color with { A = 0 } * Opacity, 0, tex.Size() / 2, Scale*(1-Opacity), SpriteEffects.None, 0);
        }
        PixelLayer IDrawPixellated.PixelLayer => PixelLayer.AboveNPCs;

        void IDrawPixellated.DrawPixelated(SpriteBatch spriteBatch)
        {
            Texture2D tex = Assets.Textures.Burst.T_Burst048.Asset.Value;

            Vector2 drawPos = Position - Main.screenPosition;

            float Opacity = Utilities.InverseLerp(0, MaxTime, TimeLeft);
            Main.EntitySpriteDraw(tex, drawPos, null, Color * Opacity, 0, tex.Size() / 2, Scale, SpriteEffects.None, 0);
        }
    }
}

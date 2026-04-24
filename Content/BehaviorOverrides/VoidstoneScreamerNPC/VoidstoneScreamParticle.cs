using AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness;
using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Graphics.Pixelation;
using Terraria;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.BehaviorOverrides.VoidstoneScreamerNPC
{
    internal class VoidstoneScreamParticle : BaseParticle<VoidstoneScreamParticle>, IDrawPixelated
    {
        public static Texture2D tex => Assets.Textures.Burst.T_Burst006.Asset.Value;
        Vector2 Position;
        Vector2 Velocity;
        Color Color;
        float Scale;
        int TimeLeft = 0;
        int MaxTime;

        public bool HasMadeLight = false;
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

            float Opacity = Utilities.InverseLerp(0, MaxTime / 2, TimeLeft);

            Vector2 Scale = new Vector2(this.Scale * 0.2f, this.Scale) * (1 - Opacity);
            if (!HasMadeLight)
            {
                ReworkedAbyssLighting.AddLight(new()
                {
                    texture = tex,

                    center = this.Position,
                    scale = this.Scale * (1 - Opacity) * 7f,
                    opacity = Opacity
                });
                HasMadeLight = true;
            }
            int index = ReworkedAbyssLighting.lights.FindIndex(a => a.center == this.Position);

            if (index != -1)
            {
                var light = ReworkedAbyssLighting.lights[index];
                light.center = Position;
                light.lifetime = 10;
                light.rotation = Velocity.ToRotation();
                light.color = Color.White*10;
                light.vectorScale = Scale;
                light.opacity = 1;
                light.Origin = light.texture.Size() / 2f;
                ReworkedAbyssLighting.lights[index] = light;
            }
            Lighting.AddLight(Position, TorchID.Ice);
        }
        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {

            Texture2D tex = Assets.Textures.Burst.T_Burst006.Asset.Value;

            Vector2 drawPos = Position - Main.screenPosition;

            float Opacity = Utilities.InverseLerp(0, MaxTime/2, TimeLeft);

            Vector2 Scale = new Vector2(this.Scale*0.2f, this.Scale);
            Main.EntitySpriteDraw(tex, drawPos, null, Color with { A = 0 } * Opacity, Velocity.ToRotation(), tex.Size() / 2, Scale*(1-Opacity), SpriteEffects.None, 0);
        }
     
    }
}

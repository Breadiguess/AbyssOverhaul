using BreadLibrary.Core.Graphics.Particles;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.BrigandsCalling
{
    internal class BrigandsCalling_MuzzleFlash : BaseParticle<BrigandsCalling_MuzzleFlash>
    {
        public Vector2 Position;
        public float Rotation;
        public int TimeLeft;
        public int MaxTime;
        int direction;
        public int MaxFrames => 6;
        public int Variant = 0;

        public void Prepare(Vector2 Pos, float Rot, int MaxTime = 10, int direction = 1, int variant = -1)
        {
            Position = Pos;
            Rotation = Rot;
            this.MaxTime = MaxTime;
            TimeLeft = this.MaxTime;
            this.direction = direction;

            if (variant != -1)
                Variant = variant;
            else
               Variant = Main.rand.Next(1, 4);

        }
        public override void Update(ref ParticleRendererSettings settings)
        {
            if (TimeLeft-- <= 0)
                ShouldBeRemovedFromRenderer = true;
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            var TexString = this.GetPath();
            var Tex = ModContent.Request<Texture2D>(TexString+$"{Variant}").Value;

            Rectangle Frame = Tex.Frame(1, MaxFrames, 0, (int)(6 * (1 - TimeLeft / (float)MaxTime)));


            SpriteEffects Flip = direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Vector2 DrawPos = Position - Main.screenPosition + new Vector2(0, -4*direction).RotatedBy(Rotation);
            Main.EntitySpriteDraw(Tex, DrawPos, Frame, Color.White, Rotation, new Vector2(0, Frame.Height / 2), 1, Flip);
        }
    }
}

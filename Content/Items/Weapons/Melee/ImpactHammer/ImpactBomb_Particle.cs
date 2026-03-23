using BreadLibrary.Core.Graphics.Particles;
using CalamityMod;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    internal class ImpactBomb_Particle : BaseParticle
    {
        public static ParticlePool<ImpactBomb_Particle> pool = new(500, GetNewParticle<ImpactBomb_Particle>);

        public Vector2 Position;
        public int TimeLeft;
        public float MaxTime;
        public void Prepare(Vector2 SpawnPos, int MaxTime = 60)
        {
            Position = SpawnPos;
            this.MaxTime = MaxTime;
            TimeLeft = MaxTime;
        }

        public override void Update(ref ParticleRendererSettings settings)
        {
            if (TimeLeft-- <= 0)
                ShouldBeRemovedFromRenderer = true;
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive, SamplerState.LinearClamp, default, default, null, Main.GameViewMatrix.TransformationMatrix); ;
            var tex = Assets.Textures.Burst.T_Burst005
                .Asset.Value;


            var SecondTex = Assets.Textures.Burst.T_Burst042.Asset.Value;
            Vector2 DrawPos = Position - Main.screenPosition;

            float LifeTimeInterp = CalamityUtils.ExpInEasing(TimeLeft / MaxTime, 1);


            float rot = MathHelper.Pi * LifeTimeInterp;
            Color ExplosionColor = Color.Yellow * (LifeTimeInterp);
            for (int i = 0; i< 4; i++)
            {
                Main.EntitySpriteDraw(tex, DrawPos, null, ExplosionColor, rot, tex.Size() / 2f, 0.4f * (1 - LifeTimeInterp) * 2, 0);

            }

            ExplosionColor = Color.DarkOliveGreen* LifeTimeInterp;
            Main.EntitySpriteDraw(SecondTex, DrawPos, null, ExplosionColor, rot, SecondTex.Size() / 2f, 0.2f * (1 - LifeTimeInterp)*1.5f, 0);


            var ThirdTex = Assets.Textures.Flare.T_Flare007.Asset.Value;
            Main.EntitySpriteDraw(ThirdTex, DrawPos, null, ExplosionColor, rot, ThirdTex.Size() / 2f, 1.4f * (1 - LifeTimeInterp), 0);


            Main.spriteBatch.ResetToDefault();
        }
    }
}

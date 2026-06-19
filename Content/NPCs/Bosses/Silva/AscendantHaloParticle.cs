using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Graphics.Pixelation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva
{
    [PoolCapacity(40)]
    internal class AscendantHaloParticle : BaseParticle<AscendantHaloParticle>
    {
        public int DeathTimer = 70;
        public State CurrentState = State.Appear;
        public enum State
        {
            Appear,

            Normal,

            Destroy
        }
        public NPC Owner;
        public Vector2 Pos = Vector2.Zero;


        public void Prepare(NPC Owner, Vector2 SpawnPos)
        {
            this.Owner = Owner;
            this.Pos = SpawnPos;
        }
        public override void Update(ref ParticleRendererSettings settings)
        {
            if (Owner is null)
                CurrentState = State.Destroy;

            switch (CurrentState)
            {
                case State.Appear:
                    CurrentState = State.Normal;
                    break;

                    case State.Normal:

                    Pos = Owner.Center;


                    break;

                    case State.Destroy:
                    DeathTimer--;
                    break;
            }

            if (DeathTimer < 1)
                ShouldBeRemovedFromRenderer = true;
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            spritebatch.UseBlendState(BlendState.Additive);

            Texture2D tex = ModContent.Request<Texture2D>(this.GetPath()).Value;

            Vector2 drawPos = Pos - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos, null, Color.White, 0, tex.Size() / 2, 1, 0);

            spritebatch.ResetToDefault();
        }

      
    }
}

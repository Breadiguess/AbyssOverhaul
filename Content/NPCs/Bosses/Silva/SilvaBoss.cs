using AbyssOverhaul.Content.NPCs.Bosses.Silva.Attacks;
using CalamityMod;
using System.IO;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva
{
    internal class SilvaBoss : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.Size = new Vector2(30, 65);
            NPC.lifeMax = 1_000_000;
            NPC.damage = 120;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }

        public enum State
        {
            Intro,
            WindingRoots,
            WindingRoots_Vertical,
            SlideAlongRoots,
            GrabBoulderAndCrushYou,
            //throws a bunch of petals, some of which are redirected down into the floor
            //which then turn into seeds that detonate at a later time
            PetalFusillade,

            //Rise up before slamming down again, because ofc.
            FloralDescent,


            Flourish
        }

        public int _Target => NPC.FindClosestPlayer();

        public Player Target => !Main.player[_Target].IsNullOrInactive() ? Main.player[_Target] : null;

        public State CurrentState
        {
            get => (State)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        public int LocalTimer;
        public int CurrentCombo = 1;
        public int CurrentComboIndex = -1;

        private SilvaAttack currentAttack;

        private static readonly State[][] Combos =
        {
            System.Array.Empty<State>(),

            new[]
            {
                State.WindingRoots,
                State.PetalFusillade,
                State.WindingRoots,
                State.Flourish
            },

            new[]
            {
                State.SlideAlongRoots,
                State.GrabBoulderAndCrushYou,
                State.WindingRoots_Vertical,
            },
        };


        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write7BitEncodedInt(LocalTimer);
            writer.Write7BitEncodedInt(CurrentCombo);
            writer.Write7BitEncodedInt(CurrentComboIndex);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {

        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SetState(State.Intro);
            //AscendantHaloParticle particle = new();
            //particle.Prepare(NPC, NPC.Top);
            //ParticleEngine.BehindProjectiles.Add(particle);
        }

        public override void AI()
        {
            HaloOffset = Vector2.Lerp(HaloOffset, NPC.Center+ new Vector2(-1,-7), 0.9f);
            currentAttack ??= SilvaAttackRegistry.Get(CurrentState);
            currentAttack.Update(this);
            LocalTimer++;
        }

        public void SetState(State newState)
        {
            currentAttack?.Exit(this);

            CurrentState = newState;
            LocalTimer = 0;
            currentAttack = SilvaAttackRegistry.Get(newState);
            currentAttack.Enter(this);

            NPC.netUpdate = true;
        }

        public void MoveToNextState()
        {
            if (CurrentCombo <= 0 || CurrentCombo >= Combos.Length || Combos[CurrentCombo].Length == 0)
            {
                SetState(State.Intro);
                return;
            }

            CurrentComboIndex++;
            if (CurrentComboIndex >= Combos[CurrentCombo].Length)
                CurrentComboIndex = 0;

            SetState(Combos[CurrentCombo][CurrentComboIndex]);
        }


        public Vector2 HaloOffset;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var tex = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/Silva/AscendantHaloParticle").Value;
            var texGlow = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/Silva/AscendantHaloParticle_Glow").Value;
            var Glow = Assets.Textures.Flare.T_Flare006.Asset.Value;
            spriteBatch.UseBlendState(BlendState.Additive);


            Main.EntitySpriteDraw(Glow, HaloOffset - screenPos, null, Color.Green, 0, Glow.Size() / 2, NPC.scale * 0.124f, 0);

             const float thing = 3;
            for (int i = 0; i < thing; i++)
            {

                Main.EntitySpriteDraw(texGlow, HaloOffset - screenPos + new Vector2(0.1f, 0).RotatedBy(Main.GlobalTimeWrappedHourly + i / thing * MathHelper.TwoPi), null, Color.White*5, 0, texGlow.Size() / 2, 1, 0);
            }



            spriteBatch.ResetToDefault();

            Main.EntitySpriteDraw(tex, HaloOffset - screenPos, null, Color.White*0.9f, 0, tex.Size() / 2, 1, 0);

            Utils.DrawBorderString(spriteBatch, CurrentState.ToString(), NPC.Center - screenPos, Color.White);
            currentAttack?.Draw(this, spriteBatch, screenPos, drawColor);
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
    }
}
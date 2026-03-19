using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Core.NPCOverrides;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.Abyss;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.BehaviorOverrides.Slammonite
{
    internal class CuttlefishOverride : NPCBehaviorOverride
    {
        public override int NPCType => ModContent.NPCType<Cuttlefish>();



        public static Asset<Texture2D> SlamTex;
        public override void Load()
        {
            SlamTex = ModContent.Request<Texture2D>("AbyssOverhaul/Content/BehaviorOverrides/Slammonite/Slamonite");
            Main.npcFrameCount[NPCType] = 18;
        }
        private const int IdleStart = 0;
        private const int IdleEnd = 3;
        private const int FlingStart = 8;
        private const int FlingEnd = 10;
        private const int ImpactStart = 13;
        private const int ImpactEnd = 16;


        public ModularNpcBrain<CreatureNpcContext> NpcBrain;


        public override void SetDefaults(NPC NPC)
        {
            NPC.friendly = false;
            NPC.damage = 40;
            NPC.lifeMax = 900;
            NPC.Size = new(40);
            InitializeBrain(NPC);
        }
        public void InitializeBrain(NPC npc)
        {
            NpcBrain = new ModularNpcBrain<CreatureNpcContext>(new());
            NpcBrain.Sensors.Add(new ThreatAwarenessSensor<CreatureNpcContext>()
            {
            });
        }
        public override void OnSpawn(NPC NPC, IEntitySource source)
        {
            NPC.Opacity = 1;
        }

        public Entity Target;



        public int Time;
        public bool Windup;
        public bool Dashing;
        public Vector2 DashDirection;
        public int DashCooldown;

        public enum State
        {
            SneakingAround,
            Dashing,
            Idle
        }

        public State CurrentState;
        private int SlowDownTime;
        public override bool OverrideAI(NPC NPC)
        {
            NPC.noGravity = NPC.wet;
            if (NpcBrain is null)
                InitializeBrain(NPC);


            if (NpcBrain.Context.HasThreat)
            {
                if (NpcBrain.Context.ClosestPlayer.Distance(NpcBrain.Context.ThreatPosition) < 12f && !Dashing && !Windup)
                {
                    DashDirection = NPC.Center.DirectionTo(NpcBrain.Context.ThreatPosition);
                    Time = -1;
                    Windup = true;
                }

            }


            if (Windup)
            {
                NPC.rotation = NPC.rotation.AngleLerp(DashDirection.ToRotation() + MathHelper.PiOver2, 0.2f);

                if (Time > 30)
                {
                    Dashing = true;
                    Time = -1;
                    Windup = false;
                }
            }

            if (Dashing)
            {
                NPC.velocity = DashDirection * 30 * Utilities.InverseLerpBump(0, 20, 40, 60, Time);
                if (Time > 60)
                {
                    StopDashing();

                }
            }




            NpcBrain.Update(NPC);
            if (DashCooldown > 0)
                DashCooldown--;
            if (SlowDownTime > 0)
            {
                SlowDownTime--;
                NPC.velocity *= 0.9f;
            }
            Time++;
            return true;
        }

        void StopDashing()
        {
            Dashing = false;
            Time = -1;
            DashCooldown = 120;

        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {

            if (Dashing)
            {

                target.AddBuff(ModContent.BuffType<FishAlert>(), 60 * 6);
                StopDashing();

                //todo: Lumyl Cloud particle

            }
        }

        public override bool OverrideFindFrame(NPC NPC)
        {
            if (Dashing)
            {
                NPC.frame.Y = (int)float.Lerp(FlingStart, FlingEnd, Utilities.InverseLerp(0, 60, Time));
            }
            return true;
        }


        public override void PostDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return;
        }
        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var tex = SlamTex.Value;

            if (tex == null)
                return false;

            Rectangle frame = tex.Frame(1, Main.npcFrameCount[NPCType], 0, NPC.frame.Y);
            Vector2 DrawPos = NPC.Center - screenPos;
            Main.EntitySpriteDraw(tex, DrawPos, frame, drawColor, NPC.rotation, frame.Size() / 2, NPC.scale, NPC.spriteDirection.ToSpriteDirection());

            if (NpcBrain is not null)
            {
                //NpcBrain.DrawContextDebug(spriteBatch, DrawPos);
            }


            return false;
        }
    }
}

using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Core.NPCOverrides;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.Abyss;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;

namespace AbyssOverhaul.Content.BehaviorOverrides.Slammonite
{
    internal class CuttlefishOverride : NPCBehaviorOverride
    {
        public override int NPCType => ModContent.NPCType<Cuttlefish>();

        public override string TexturePath => "AbyssOverhaul/Content/BehaviorOverrides/Slammonite/Slamonite";

        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            typeName = Language.GetOrRegister($"Mods.AbyssOverhaul.NPCOverrides.Cuttlefish").Value;
        }

        public static Asset<Texture2D> SlamTex;
        public override void Load()
        {
            SlamTex = ModContent.Request<Texture2D>("AbyssOverhaul/Content/BehaviorOverrides/Slammonite/Slamonite");
            Main.npcFrameCount[NPCType] = 18;
        }

        private const int IdleStart = 0;
        private const int IdleEnd = 3;
        private const int WindupStart = 4;
        private const int WindupEnd = 7;
        private const int FlingStart = 8;
        private const int FlingEnd = 10;
        private const int RecoverStart = 11;
        private const int RecoverEnd = 12;
        private const int ImpactStart = 13;
        private const int ImpactEnd = 16;
        private const int FleeFrame = 17;

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



        public SlamState CurrentState;
        public int StateTimer;
        public int DashCooldown;
        public int RecoverTimer;

        public Vector2 DashDirection;
        public Vector2 WanderOffset;
        public int WanderRefreshTimer;

        public float Visibility; // 0 invisible, 1 fully visible
        public float LightExposure; // smoothed light value

        public enum SlamState
        {
            Sneak,
            Windup,
            Dash,
            Recover,
            FleeLit
        }
        private int SlowDownTime;
        public override bool OverrideAI(NPC NPC)
        {
          
            if (NpcBrain is null)
                OnSpawn(NPC, null);

            NPC.noGravity = NPC.wet;

            // Update brain FIRST.
           NpcBrain.Update(NPC);

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest();
                target = Main.player[NPC.target];
            }

            UpdateLightAndVisibility(NPC);

            bool brightlyLit = LightExposure >= 0.6f;
            bool canAmbush = LightExposure <= 0.42f;
            float distToTarget = NPC.Distance(target.Center);

            if (DashCooldown > 0)
                DashCooldown--;

            
            StateTimer++;

            switch (CurrentState)
            {
                case SlamState.Sneak:
                    DoSneak(NPC, target, canAmbush, brightlyLit, distToTarget);
                    break;

                case SlamState.Windup:
                    DoWindup(NPC, target, brightlyLit);
                    break;

                case SlamState.Dash:
                    DoDash(NPC, target, brightlyLit);
                    break;

                case SlamState.Recover:
                    DoRecover(NPC, brightlyLit);
                    break;

                case SlamState.FleeLit:
                    DoFlee(NPC, target, brightlyLit);
                    break;
            }

            NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.ToRotation() + MathHelper.PiOver2, 0.16f);

            return true;
        }
        
        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {

           
        }

        private void UpdateLightAndVisibility(NPC npc)
        {
            Point tilePos = npc.Center.ToTileCoordinates();

            Vector3 light =
                Lighting.GetColor(tilePos).ToVector3() +
                Lighting.GetColor(tilePos + new Point(1, 0)).ToVector3() +
                Lighting.GetColor(tilePos + new Point(-1, 0)).ToVector3() +
                Lighting.GetColor(tilePos + new Point(0, 1)).ToVector3() +
                Lighting.GetColor(tilePos + new Point(0, -1)).ToVector3();

            light /= 5f;

            float exposure = light.Length() / 1.732f; 
            // normalize approx from RGB vector length
            LightExposure = MathHelper.Lerp(LightExposure, exposure, 0.12f);

            float targetVisibility = 0.08f;

            switch (CurrentState)
            {
                case SlamState.Sneak:
                    targetVisibility = MathHelper.Lerp(0.06f, 0.45f, LightExposure);
                    break;

                case SlamState.Windup:
                    targetVisibility = MathHelper.Lerp(0.35f, 0.8f, LightExposure);
                    break;

                case SlamState.Dash:
                    targetVisibility = 1f;
                    break;

                case SlamState.Recover:
                    targetVisibility = 0.7f;
                    break;

                case SlamState.FleeLit:
                    targetVisibility = MathHelper.Lerp(0.7f, 1f, LightExposure);
                    break;
            }

            Visibility = MathHelper.Lerp(Visibility, targetVisibility, 0.14f);
            npc.Opacity = MathHelper.Clamp(Visibility, 0.05f, 1f);
        }
        private void ChangeState(NPC npc, SlamState newState)
        {
            CurrentState = newState;
            StateTimer = 0;
            npc.netUpdate = true;
        }

        #region StateMachine

        private void DoSneak(NPC npc, Player target, bool canAmbush, bool brightlyLit, float distToTarget)
        {
            if (brightlyLit)
            {
                ChangeState(npc, SlamState.FleeLit);
                return;
            }

            if (WanderRefreshTimer-- <= 0)
            {
                Vector2 around = Main.rand.NextVector2Circular(90f, 55f);
                around.Y -= 25f;
                WanderOffset = around;
                WanderRefreshTimer = Main.rand.Next(40, 90);
            }

            Vector2 desiredSpot = target.Center + WanderOffset;
            Vector2 toSpot = desiredSpot - npc.Center;

            float speed = 3.8f;
            Vector2 desiredVelocity = toSpot.SafeNormalize(Vector2.UnitY) * speed;

            // Soft sneaky movement
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.06f);

            // A little drift
            npc.velocity += Main.rand.NextVector2Circular(0.02f, 0.02f);

            if (canAmbush && DashCooldown <= 0 && distToTarget < 90f)
            {
                DashDirection = npc.SafeDirectionTo(target.Center);
                ChangeState(npc, SlamState.Windup);
            }
        }

        private void DoWindup(NPC npc, Player target,  bool brightlyLit)
        {
            if (brightlyLit)
            {
                ChangeState(npc, SlamState.FleeLit);
                return;
            }

            npc.velocity *= 0.90f;

            Vector2 aimDir = npc.SafeDirectionTo(target.Center);
            DashDirection = Vector2.Lerp(DashDirection, aimDir, 0.12f);
            npc.rotation = npc.rotation.AngleLerp(DashDirection.ToRotation() + MathHelper.PiOver2, 0.25f);

            // Brief commit phase
            if (StateTimer >= 28)
            {
                ChangeState(npc, SlamState.Dash);
            }
        }

        private void DoDash(NPC npc, Player target, bool brightlyLit)
        {
            // Commit to dash even if lit mid-attack.
            float dashSpeed = 20f;
            float dashCurve = Utilities.InverseLerpBump(0f, 4f, 18f, 28f, StateTimer);
            npc.velocity = DashDirection * MathHelper.Lerp(10f, dashSpeed, dashCurve);

            if (StateTimer >= 30)
            {
                RecoverTimer = 24;
                DashCooldown = 120;
                ChangeState(npc, SlamState.Recover);
            }
        }

        private void DoRecover(NPC npc, bool brightlyLit)
        {
            npc.velocity *= 0.88f;

            if (brightlyLit)
            {
                ChangeState(npc, SlamState.FleeLit);
                return;
            }

            if (StateTimer >= RecoverTimer)
                ChangeState(npc, SlamState.Sneak);
        }

        private void DoFlee(NPC npc, Player target, bool brightlyLit)
        {
            Vector2 away = target.Center.DirectionTo(npc.Center);
            Vector2 desiredVelocity = away * 5.5f + new Vector2(0f, -0.8f);

            npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.08f);

            // Once it reaches darkness again, it can resume stalking.
            if (!brightlyLit && StateTimer >= 35)
            {
                DashCooldown = 90;
                ChangeState(npc, SlamState.Sneak);
            }
        }


        #endregion




        public override bool OverrideFindFrame(NPC npc)
        {

            int frame = 0;

            switch (CurrentState)
            {
                case SlamState.Sneak:
                    frame = IdleStart + (StateTimer / 8) % (IdleEnd - IdleStart + 1);
                    break;

                case SlamState.Windup:
                    frame = (int)MathHelper.Lerp(WindupStart, WindupEnd, Utilities.InverseLerp(0f, 28f, StateTimer));
                    break;

                case SlamState.Dash:
                    frame = (int)MathHelper.Lerp(FlingStart, FlingEnd, Utilities.InverseLerp(0f, 30f, StateTimer));
                    break;

                case SlamState.Recover:
                    frame = RecoverStart + (StateTimer / 6) % (RecoverEnd - RecoverStart + 1);
                    break;

                case SlamState.FleeLit:
                    frame = FleeFrame;
                    break;
            }

            npc.frame.Y = frame;
            return true;
        }



        public override void PostDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return;
        }
        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var tex = TextureAssets.Npc[this.NPCType].Value;    

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

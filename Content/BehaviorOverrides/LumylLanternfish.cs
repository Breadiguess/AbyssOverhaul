using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Core.NPCOverrides;
using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using BreadLibrary.Core.Utilities;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.BehaviorOverrides
{
    public sealed class LumylLanternfish : NPCBehaviorOverride, IDrawPixellated
    {
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.LuminousCorvina>();


        public static readonly SoundStyle ScreamSound = new("CalamityMod/Sounds/Custom/CorvinaScream");
        public static Asset<Texture2D> Tex;

        public ModularNpcBrain<CreatureNpcContext> NpcBrain;
        public override void SetDefaults(NPC npc)
        {
            npc.noGravity = true;
            npc.noTileCollide = false;

            npc.lifeMax = 12_000;

            InitializeBrain();

        }
        public override void Load()
        {
            Tex = ModContent.Request<Texture2D>("AbyssOverhaul/Content/BehaviorOverrides/LumylLanternfish");

            Main.npcFrameCount[NPCType] = 8;
        }
        private void InitializeBrain()
        {
            NpcBrain = new ModularNpcBrain<CreatureNpcContext>(new CreatureNpcContext
            {
                PreferredSpacing = 72f
            });

            NpcBrain.Sensors.Add(new SharedCreatureAwarenessSensor()
            {

            });

            NpcBrain.Modules.Add(new AvoidTilesSwimModule
            {
                Score = 35f,
                ProbeDistance = 42f,
                SideProbeDistance = 34f,
                MoveSpeed = 6f
            });

            NpcBrain.Modules.Add(new AvoidSameTypeModule
            {
                PreferredSeparation = 72f,
                AvoidanceRadius = 120f,
                MaxMoveSpeed = 2.4f,
                BaseScore = 4f,
                CrowdingScoreMultiplier = 5f
            });

            NpcBrain.Modules.Add(new CreatureSwimWanderModule
            {
                VerticalStrength = 20,
                Score = 30f,
                MoveSpeed = 6.2f,
                HomeRadius = 1000f,
                DepthSlack = 64f
            });

            NpcBrain.Modules.Add(new FleeThreatModule<CreatureNpcContext>()
            {
                MoveSpeed = 12
            });



        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            base.OnSpawn(npc, source);

            if (NpcBrain is null)
                InitializeBrain();

            NpcBrain.Context.HomePosition = npc.Center;
        }

        public override bool OverrideAI(NPC npc)
        {

            if (NpcBrain is null)
                InitializeBrain();


            NpcBrain.Update(npc);

            if ((Main.GameUpdateCount * 0.05f) % 10 == 0)
            {
                NpcBrain.Context.TargetPoint = Main.rand.NextVector2Unit() + npc.Center;
                //Main.NewText("Im a stupid fucking fish");   
            }

            if (NpcBrain.Context.HasDisturbance && NpcBrain.Context.ThreatLevel > 0.5f)
            {
                //SoundEngine.PlaySound(ScreamSound with { pitchVariance = 0.2f, pitch= 0.2f, MaxInstances = 0}, npc.Center);

            }


            float accel = 0.12f;

            float desiredVelX = NpcBrain.LastDesiredVelocity.X;

            float steering =
                desiredVelX - npc.velocity.X;

            steering = MathHelper.Clamp(steering, -accel, accel);


            var referenceSpeed = 1f;
            var maxTilt = MathHelper.ToRadians(20f);
            var normalized = MathHelper.Clamp(npc.velocity.X / referenceSpeed, -1f, 1f);
            var targetRotation = normalized * maxTilt;

            npc.rotation = npc.rotation.AngleLerp(targetRotation, 0.65f);
            npc.spriteDirection = npc.velocity.X.DirectionalSign();


            Dust.NewDustPerfect(NpcBrain.Context.HomePosition, DustID.Cloud, Vector2.zeroVector);

            return true;
        }


        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 DrawPos = npc.Center - screenPos;

            var texture = Tex.Value;

            SpriteEffects flip = npc.spriteDirection.ToSpriteDirection();
            Main.EntitySpriteDraw(texture, DrawPos, npc.frame, drawColor, npc.rotation, npc.frame.Size() / 2, npc.scale, flip);


            //NpcBrain.DrawContextDebug(spriteBatch, DrawPos);    
            return false;
        }
        PixelLayer IDrawPixellated.PixelLayer => PixelLayer.AboveTiles;
        void IDrawPixellated.DrawPixelated(SpriteBatch spriteBatch)
        {

        }
    }
}
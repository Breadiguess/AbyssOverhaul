using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles;
using AbyssOverhaul.Core.Ecosystem.TerritorySystem;
using CalamityMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.KarkinosNPC
{
    public partial class Karkinos : ModNPC, IEcologyParticipant
    {

        public bool _TerritoryInitialized = false;


        public Territory territory;
        void IEcologyParticipant.SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.Territorial);
            definition.BaseMaxHunger = 100;

            definition.BaseAggression = 0.3f;
            definition.BaseFear = -0.4f;
        }

        void IEcologyParticipant.SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {

        }
        ModularNpcBrain<CreatureNpcContext> NPCBrain;
        public override void SetDefaults()
        {
            NPC.lifeMax = 50_000;
            NPC.Size = new(50, 50);
            NPC.defense = 120;
            NPC.noTileCollide = false;
            NPC.damage = 120;
        }
        private void InitializeBrain()
        {
            NPCBrain = new ModularNpcBrain<CreatureNpcContext>(new());

            // Use your existing awareness sensor unless ThreatAwarenessSensor is a separate class
            // that exists somewhere else in your project.
            NPCBrain.Sensors.Add(new SharedCreatureAwarenessSensor()
            {
                PlayerThreatRadius = 1200f
            });

            // This must come after the shared threat sensor if it also writes threat/disturbance.
            NPCBrain.Sensors.Add(new TerritoryAwarenessSensor());

            // Use target point as the "found home tile" bootstrap.
            NPCBrain.Sensors.Add(new FindTileSensor(tile =>
                tile.HasTile && tile.TileType == ModContent.TileType<SmoothedBrineCrystal>())
            {
                SearchRadius = 1600f
            });

            // Optional, but useful once HomePosition is set.
            NPCBrain.Modules.Add(new IdleHomeModule()
            {
                MoveSpeed = 1.5f,
                ReturnDistance = 120f
            });
        }

        public override void OnSpawn(IEntitySource source)
        {
            InitializeLegs();
            InitializeBrain();
        }

        public override bool PreAI()
        {
            if (_KarkinosLegs == null)
                InitializeLegs();

            if (NPCBrain is null)
                InitializeBrain();

            return base.PreAI();
        }

        public override void AI()
        {
            // First update the context from sensors.
            NPCBrain.Update(NPC);

            // Bootstrap home from the nearest valid crystal tile.
            // FindTileSensor writes HasTargetPoint / TargetPoint, not HasFoundTile / FoundTileWorld.
            if (!_TerritoryInitialized &&
                NPCBrain.Context.HomePosition == Vector2.Zero &&
                NPCBrain.Context.HasFoundTile)
            {
                NPCBrain.Context.HomePosition = NPCBrain.Context.FoundTileWorld;
            }

            // Create territory once home has been found.
            if (!_TerritoryInitialized && NPCBrain.Context.HomePosition != Vector2.Zero)
            {
                territory = new Territory(NPCBrain.Context.HomePosition, new Rectangle(0, 0, 1200, 400));
                territory.Owner = NPC;
                _TerritoryInitialized = true;
            }

            if (territory is null)
                return;

            float bestAggression = 0f;

            foreach (Player player in Main.ActivePlayers)
            {
                if (player is null || !player.active || player.dead)
                    continue;

                if (!player.Hitbox.Intersects(territory.Bounds))
                    continue;

                float t = Utils.Remap(
                    Vector2.Distance(player.Center, territory.Center),
                    territory.Bounds.Size().Length(),
                    0f,
                    0f,
                    1f);

                bestAggression = Math.Max(bestAggression, t * t);
            }

            NPC.Ecology().Aggression = bestAggression;

            NPC.Ecology().Fear = Utils.Remap(
                Vector2.Distance(NPC.Center, territory.Center),
                territory.Bounds.Size().Length(),
                0f,
                0f,
                1f);

            float moveSpeed = 3.5f;

            Vector2 desiredPos =
                NPC.Ecology().Aggression > 0.4f && NPCBrain.Context.HasThreat
                ? NPCBrain.Context.ThreatPosition
                : territory.Center;

            float desiredVelX = NPC.DirectionTo(desiredPos).X * moveSpeed;
            MoveSmoothlyTo(desiredVelX);
        }

        private void MoveSmoothlyTo(float desiredVelX)
        {
            float accel = 0.12f;

            float steering = desiredVelX - NPC.velocity.X;
            steering = MathHelper.Clamp(steering, -accel, accel);

            NPC.velocity.X += steering;
            NPC.spriteDirection = desiredVelX.NonZeroSign();

            float referenceSpeed = 12f;
            float maxTilt = MathHelper.ToRadians(60f);
            float normalized = MathHelper.Clamp(NPC.velocity.X / referenceSpeed, -1f, 1f);
            float targetRotation = normalized * maxTilt;

            NPC.rotation = NPC.rotation.AngleLerp(targetRotation, 0.2f);
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
        }
        public override void PostAI()
        {
            KarkinosLegUpdate();
            EstimateSurfaceFrame(NPC.Center, out Vector2 normal, out Vector2 tangent);

            this.normal = normal;
            this.tangent = tangent;

            for (int i = 0; i < LimbOffsets.Length; i++)
            {
                ActualLimbOffsets[i] = LimbOffsets[i].RotatedBy(tangent.ToRotation() * 0.6f);
            }


            Vector2 delta = NPC.Center - _lastBodyPos;

            if (delta.LengthSquared() > 0.001f)
            {
                MotionIntent = Vector2.Lerp(
                    MotionIntent,
                    delta.SafeNormalize(Vector2.Zero),
                    0.25f
                );
            }

            _lastBodyPos = NPC.Center;
            HoverOffTheGround();
        }

        private void HoverOffTheGround()
        {

            float maxCheck = 110f;

            float desiredHeight = 98f;
            int hitCount = 0;
            float accumulatedHeight = 0f;

            for (int i = 0; i < 3; i++)
            {
                Vector2 start = NPC.Center;
                Vector2 end = start + Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i / 3f - MathHelper.PiOver2 / 3f - NPC.rotation) * maxCheck;

                Point? hit = LineAlgorithm.RaycastTo(start, end, debug: false);

                if (!hit.HasValue)
                    continue;

                float height =
                    hit.Value.ToWorldCoordinates().Y - NPC.Center.Y;

                accumulatedHeight += height;
                hitCount++;
            }

            if (hitCount < 2)
            {
                NPC.noGravity = false;
                return;
            }

            float actualHeight = accumulatedHeight / hitCount;
            float tolerance = 1.5f;

            float error = desiredHeight - actualHeight;

            if (MathF.Abs(error) < tolerance)
            {
                NPC.velocity.Y = 0f;
                NPC.noGravity = true;
                return;
            }

            float correctionStrength = 0.07f;

            float moveAmount = error * correctionStrength;
            moveAmount = MathHelper.Clamp(moveAmount, -2f, 2f);

            NPC.position.Y -= moveAmount;
            NPC.noGravity = true;
            NPC.velocity.Y = 0f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            if (NPC.IsABestiaryIconDummy)
                return true;
            if (_KarkinosLegs == null)
                return false;

            foreach (var leg in _KarkinosLegs)
            {
                leg.DrawLeg(leg, spriteBatch, screenPos);
            }


            string Aggression = NPC.Ecology().Aggression.ToString();
            Aggression += $"\n{NPC.Ecology().Fear}";
            Utils.DrawBorderString(spriteBatch, Aggression, NPC.Center - screenPos, Color.White, anchory: 1);


            Utilities.DrawLineBetter(spriteBatch, NPC.Center, NPC.Center + this.MotionIntent *500, drawColor, 4);

            return true;
        }

      
    }
}

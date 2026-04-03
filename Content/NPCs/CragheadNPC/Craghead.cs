using AbyssOverhaul.Core.Ecosystem.Simulation;
using AbyssOverhaul.Core.Ecosystem.Simulation.AbyssOverhaul.Core.Ecosystem.Persistence;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;

namespace AbyssOverhaul.Content.NPCs.CragheadNPC
{
    internal class Craghead : ModNPC, IEcologyParticipant
    {
        #region Values

        public void SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.Territorial);
            definition.FoodConsumer = FoodConsumerType.Omnivore;
            definition.BaseMaxHunger = 70;
        }

        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {
            ecology.AddIndividualTrait(NpcTraitFlags.Predator);
            ecology.HungerModifier = Main.rand.Next(-10, 11);
        }

        public enum OreType
        {
            None,
            Iron,
            Scoria,
            IronBoot
        }

        public OreType HeadMaterial
        {
            get => (OreType)NPC.ai[3];
            set => NPC.ai[3] = (float)value;
        }

        public enum Behavior
        {
            Debug,
            DefendTerritory,
            RamEntity
        }

        public Behavior CurrentState
        {
            get => (Behavior)NPC.ai[2];
            set => NPC.ai[2] = (float)value;
        }

        public bool LostHeadMaterial;

        private const float IdleSwimSpeed = 3.2f;
        private const float ChaseSwimSpeed = 6.2f;
        private const float RamSpeed = 12f;

        private const float IdleAcceleration = 0.08f;
        private const float ChaseAcceleration = 0.18f;
        private const float RamAcceleration = 0.42f;

        private const float BiteRange = 46f;
        private const float RamStartDistance = 280f;

        private const int BiteCooldownMax = 24;
        private const int RamCooldownMax = 90;
        private const int RamDurationMax = 34;

        private const float HuntRatio = 0.65f;

        private int BiteCooldown
        {
            get => (int)NPC.localAI[0];
            set => NPC.localAI[0] = value;
        }

        private int RamCooldown
        {
            get => (int)NPC.localAI[1];
            set => NPC.localAI[1] = value;
        }

        private int StateTimer
        {
            get => (int)NPC.localAI[2];
            set => NPC.localAI[2] = value;
        }

        #endregion

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = 34_000;
            NPC.damage = 70;
            NPC.defense = 10;
            NPC.Size = new Vector2(60, 40);

            NPC.noTileCollide = false;
            NPC.noGravity = false;
            NPC.knockBackResist = 0.08f;
            NPC.aiStyle = -1;
        }

        #region AI

        public override bool PreAI()
        {
            if (HeadMaterial == OreType.None && !LostHeadMaterial)
            {
                HeadMaterial = (OreType)Main.rand.Next(1, 4);
                NPC.netUpdate = true;
            }

            if (BiteCooldown > 0)
                BiteCooldown--;

            if (RamCooldown > 0)
                RamCooldown--;

            if (StateTimer > 0)
                StateTimer--;

            ApplyHeadMaterialStats();
            return true;
        }

        public override void AI()
        {
            NPC.noGravity = NPC.wet;
            NPC.waterMovementSpeed = 1f;

            if (!NPC.wet)
            {
                HandleOutOfWater();
                return;
            }

            EcologyGlobalNPC eco = NPC.Ecology();
            bool hasActor = EcologySystem.Instance.TryGetActor(NPC, out EcologyActor actor);

            bool hungry = eco.MaxHunger > 0 && eco.Hunger >= eco.MaxHunger * HuntRatio;
            NPC prey = NPC.FindClosestAbyssPrey(out float preyDistance);

            switch (CurrentState)
            {
                case Behavior.RamEntity:
                    DoRamBehavior(prey, preyDistance, hungry, eco, actor, hasActor);
                    break;

                case Behavior.Debug:
                case Behavior.DefendTerritory:
                default:
                    DoNormalBehavior(prey, preyDistance, hungry, eco, actor, hasActor);
                    break;
            }

            UpdateFacingAndRotation();
        }

        public override void PostAI()
        {
            UpdateVisualEffects(HeadMaterial);
        }

        public override void FindFrame(int frameHeight)
        {
            if (!NPC.wet || NPC.velocity.LengthSquared() < 0.08f)
            {
                NPC.frame.Y = 0;
                NPC.frameCounter = 0;
                return;
            }

            NPC.frameCounter += 0.18 + NPC.velocity.Length() * 0.06f;
            if (NPC.frameCounter >= Main.npcFrameCount[Type])
                NPC.frameCounter = 0;

            NPC.frame.Y = (int)NPC.frameCounter * frameHeight;
        }

        private void DoNormalBehavior(NPC prey, float preyDistance, bool hungry, EcologyGlobalNPC eco, EcologyActor actor, bool hasActor)
        {
            if (hungry && IsValidPrey(prey))
            {
                SwimToward(prey.Center, ChaseAcceleration, ChaseSwimSpeed);

                if (RamCooldown <= 0 && preyDistance <= RamStartDistance && Collision.CanHit(NPC.Center, 1, 1, prey.Center, 1, 1))
                {
                    CurrentState = Behavior.RamEntity;
                    StateTimer = RamDurationMax;
                    NPC.netUpdate = true;
                    return;
                }

                if (BiteCooldown <= 0 && preyDistance <= BiteRange)
                {
                    BiteTarget(prey, NPC.defDamage);
                    ReduceHunger(eco, actor, hasActor, 3);
                    BiteCooldown = BiteCooldownMax;
                }

                return;
            }

            if (StateTimer <= 0)
            {
                StateTimer = Main.rand.Next(50, 100);
                NPC.direction = Main.rand.NextBool() ? 1 : -1;
            }

            float bob = (float)Math.Sin((Main.GameUpdateCount + NPC.whoAmI * 17f) * 0.06f) * 34f;
            Vector2 idlePoint = NPC.Center + new Vector2(NPC.direction * 100f, bob);
            SwimToward(idlePoint, IdleAcceleration, IdleSwimSpeed);
        }

        private void DoRamBehavior(NPC prey, float preyDistance, bool hungry, EcologyGlobalNPC eco, EcologyActor actor, bool hasActor)
        {
            if (!hungry || !IsValidPrey(prey) || StateTimer <= 0)
            {
                ExitRam();
                return;
            }

            Vector2 predictedPosition = prey.Center + prey.velocity * 10f;
            SwimToward(predictedPosition, RamAcceleration, RamSpeed*4);

            Dust.NewDust(NPC.Center, 10, 10, DustID.Firefly);
            if (BiteCooldown <= 0)
            {
                bool hitSomething = TryRamHit();
                if (hitSomething)
                {
                    BiteCooldown = 10;
                    HandleImpactEvent();
                    ExitRam();
                    return;
                }
            }

            if (preyDistance > 500f && StateTimer < 16)
                ExitRam();
        }

        private void ExitRam()
        {
            CurrentState = Behavior.DefendTerritory;
            RamCooldown = RamCooldownMax;
            StateTimer = 0;
            NPC.netUpdate = true;
        }

        private void HandleOutOfWater()
        {
            NPC.noGravity = false;
            NPC.velocity.X *= 0.95f;

            if (NPC.collideY && Math.Abs(NPC.velocity.Y) < 0.1f)
                NPC.velocity.Y = -4.2f;

            float targetRotation = MathHelper.Clamp(NPC.velocity.X * 0.08f, -0.45f, 0.45f);
            NPC.rotation = NPC.rotation.AngleLerp(targetRotation, 0.12f);
        }

        private void SwimToward(Vector2 destination, float acceleration, float maxSpeed)
        {
            Vector2 toDestination = destination - NPC.Center;
            if (toDestination == Vector2.Zero)
                return;

            Vector2 desiredVelocity = toDestination.SafeNormalize(Vector2.UnitX * NPC.direction) * maxSpeed;
            Vector2 steering = desiredVelocity - NPC.velocity;

            if (steering.Length() > acceleration)
                steering = steering.SafeNormalize(Vector2.Zero) * acceleration;

            NPC.velocity += steering;

            if (NPC.velocity.Length() > maxSpeed)
                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * maxSpeed;
        }

        private void UpdateFacingAndRotation()
        {
            if (Math.Abs(NPC.velocity.X) > 0.08f)
            {
                NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
            }

            float targetRotation = MathHelper.Clamp(NPC.velocity.Y * 0.055f, -0.55f, 0.55f);
            NPC.rotation = NPC.rotation.AngleLerp(targetRotation, 0.16f);
        }

        private bool IsValidPrey(NPC prey)
        {
            return prey is not null &&
                   prey.active &&
                   prey.whoAmI != NPC.whoAmI &&
                   !prey.dontTakeDamage &&
                   !prey.friendly;
        }

        private void BiteTarget(NPC prey, int damage)
        {
            prey.SimpleStrikeNPC(damage, NPC.direction);
            prey.velocity += NPC.DirectionTo(prey.Center) * 4.5f;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit)
        {
            if (target.life < 1)
                NPC.Ecology().Hunger -= 10;
        }
        private bool TryRamHit()
        {
            Rectangle hitbox = NPC.Hitbox;
            hitbox.Inflate(14, 10);

            bool hitAnything = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (!other.active || other.whoAmI == NPC.whoAmI || other.type == Type)
                    continue;

                if (other.friendly || other.dontTakeDamage)
                    continue;

                if (!hitbox.Intersects(other.Hitbox))
                    continue;

                other.SimpleStrikeNPC(NPC.defDamage *4, NPC.direction, noPlayerInteraction: true);
                other.velocity += NPC.DirectionTo(other.Center) * 10f;
                hitAnything = true;
            }

            return hitAnything;
        }

        private void ReduceHunger(EcologyGlobalNPC eco, EcologyActor actor, bool hasActor, int amount)
        {
           NPC.Ecology().Metabolism.StomachContent += amount*2;
        }

        private void ApplyHeadMaterialStats()
        {
            switch (HeadMaterial)
            {
                case OreType.Iron:
                    NPC.defense = 18;
                    NPC.knockBackResist = 0.08f;
                    break;

                case OreType.Scoria:
                    NPC.defense = 12;
                    NPC.knockBackResist = 0.12f;
                    break;

                case OreType.IronBoot:
                    NPC.defense = 24;
                    NPC.knockBackResist = 0.03f;
                    break;

                default:
                    NPC.defense = 10;
                    NPC.knockBackResist = 0.15f;
                    break;
            }
        }

        #endregion

        #region DrawCode

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Utils.DrawBorderString(spriteBatch, HeadMaterial.ToString(), NPC.Center - screenPos, drawColor);

            var tex = TextureAssets.Npc[Type].Value;
            Rectangle frame = tex.Frame(1, 3, 0, 1);
            Main.EntitySpriteDraw(tex, NPC.Center - screenPos, frame, drawColor, NPC.rotation, frame.Size() / 2, NPC.scale, (NPC.spriteDirection).ToSpriteDirection());
            return false;
        }

        #endregion

        #region Helpers

        private void UpdateVisualEffects(OreType type)
        {
            switch (type)
            {
                case OreType.Scoria:
                    if (Main.rand.NextBool(4))
                    {
                        Vector2 spawnPos = NPC.Center + new Vector2(NPC.width * 0.5f * NPC.spriteDirection, -10f);
                        Vector2 velocity = new Vector2(0f, -4f);

                        MediumMistParticle mist = new MediumMistParticle(
                            spawnPos,
                            velocity,
                            Main.rand.NextBool(3) ? Color.LightSteelBlue : Color.SteelBlue,
                            Color.LightSlateGray,
                            Main.rand.NextFloat(0.4f, 0.65f),
                            130);

                        GeneralParticleHandler.SpawnParticle(mist);
                    }
                    break;
            }
        }

        public void HandleImpactEvent()
        {
            Collision.HitTiles(NPC.position, NPC.velocity, NPC.width, NPC.height);

            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    HeadMaterial == OreType.Scoria ? DustID.Torch : DustID.Stone,
                    NPC.velocity.X * 0.2f,
                    NPC.velocity.Y * 0.2f);
            }
        }

        #endregion
    }
}
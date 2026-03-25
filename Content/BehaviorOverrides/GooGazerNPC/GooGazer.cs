using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Core.NPCOverrides;
using CalamityMod.NPCs.Abyss;
using CalamityMod.Tiles.Abyss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;

namespace AbyssOverhaul.Content.BehaviorOverrides.GooGazerNPC
{
    internal class GooGazer : NPCBehaviorOverride
    {
        public override int NPCType => ModContent.NPCType<Laserfish>();
        public override string TexturePath => this.GetPath();

        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            typeName = Language.GetOrRegister("Mods.AbyssOverhaul.NPCOverrides.LaserFish").Value;
        }

        private const int TotalFrames = 16;
        private const float BlastDetectDistance = 220f;
        private const int BlastCooldownTime = 120;
        private const int EatRange = 26;

        public ModularNpcBrain<GooGazerContext> NpcBrain;

        private ref float BlastCooldown => ref _npc.ai[0];
        private ref float WanderAngle => ref _npc.ai[1];
        private ref float LocalTimer => ref _npc.ai[2];
        public ref float ChargeWindup => ref _npc.ai[3];
        private NPC _npc;

        public override void SetDefaults(NPC npc)
        {
            _npc = npc;

            npc.width = 52;
            npc.height = 34;
            npc.damage = 40;
            npc.defense = 8;
            npc.lifeMax = 260;
            npc.knockBackResist = 0.1f;
            npc.noGravity = true;
            npc.noTileCollide = false;
            npc.behindTiles = false;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = Item.buyPrice(0, 0, 6, 0);

            Main.npcFrameCount[npc.type] = TotalFrames;

            InitializeBrain();
        }

        public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source)
        {
            _npc = npc;
            BlastCooldown = Main.rand.Next(30, 90);
            WanderAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            LocalTimer = Main.rand.Next(120);

            if (NpcBrain is null)
                InitializeBrain();
        }

        private void InitializeBrain()
        {
            NpcBrain = new ModularNpcBrain<GooGazerContext>(new GooGazerContext());

            NpcBrain.Sensors.Add(new SharedCreatureAwarenessSensor());
            NpcBrain.Sensors.Add(new PlantyMushFoodSensor()
            {
                SearchRadius = 360f,
                FeedOffset = 24f
            });

            NpcBrain.Modules.Add(new FleeThreatModule<GooGazerContext>()
            {
                Score = 30f,
                MoveSpeed = 4.4f
            });

            NpcBrain.Modules.Add(new SeekPlantyMushModule()
            {

                MoveSpeed = 2.25f,
                ArrivalDistance = 26f
            });


            NpcBrain.Modules.Add(new GooGazerWanderModule()
            {
                Score = 5f,
                MoveSpeed = 1.5f,
                TurnInterval = 55
            });
        }

        public override bool OverrideAI(NPC npc)
        {
            _npc = npc;

            if (NpcBrain is null)
                InitializeBrain();

            npc.TargetClosest(false);

            if (BlastCooldown > 0f)
                BlastCooldown--;

            NpcBrain.Context.WanderAngle = WanderAngle;
            NpcBrain.Context.LocalTimer = (int)LocalTimer;

            NpcBrain.Update(npc);

            Main.NewText($"FoodDrive:{NpcBrain.Context.FoodDrive},\nHunger:{NpcBrain.Context.Hunger}");

            WanderAngle = NpcBrain.Context.WanderAngle;
            LocalTimer++;
            UpdateRotationAndDirection(npc);

            TryEatPlantyMush(npc);
            TryFireBlast(npc);


            return true;
        }

        private void TryEatPlantyMush(NPC npc)
        {
            GooGazerContext context = NpcBrain.Context;
            if (!context.HasFoodTile || !context.WantsFood)
                return;

            Vector2 eatSpot = context.TargetPoint;
            if (Vector2.Distance(npc.Center, eatSpot) > EatRange)
                return;

            Point tilePoint = context.FoodTile;
            Tile tile = Framing.GetTileSafely(tilePoint.X, tilePoint.Y);

            if (!tile.HasTile || tile.TileType != ModContent.TileType<PlantyMush>())
                return;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                WorldGen.KillTile(tilePoint.X, tilePoint.Y, effectOnly: true);

                npc.rotation = npc.rotation.AngleLerp(eatSpot.AngleFrom(npc.Center)-MathHelper.PiOver2*npc.spriteDirection, 0.2f);

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: tilePoint.X, number3: tilePoint.Y, number4: 0f, number5: 0, number6: 0, number7: 0);
            }
            context.Hunger -= 0.05f;
            npc.velocity *= 0.8f;
            npc.netUpdate = true;
        }

        private const float ChargeTurnRate = 0.18f;
        private const float ChargeReadyThreshold = 0.96f;

        private void TryFireBlast(NPC npc)
        {
            Player target = Main.player[npc.target];
            if (target is null || !target.active || target.dead)
            {
                ChargeWindup = MathHelper.Lerp(ChargeWindup, 0f, 0.2f);
                return;
            }

            float distanceToTarget = npc.Distance(target.Center);
            if (distanceToTarget > BlastDetectDistance)
            {
                ChargeWindup = MathHelper.Lerp(ChargeWindup, 0f, 0.25f);
                return;
            }

            if (!Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height))
            {
                ChargeWindup = MathHelper.Lerp(ChargeWindup, 0f, 0.2f);
                return;
            }

            Vector2 predictedTarget = target.Center + target.velocity * 10f;
            Vector2 aimDirection = (predictedTarget - npc.Center).SafeNormalize(Vector2.UnitX * npc.direction);

            // Face the target while charging.
            int desiredSpriteDirection = aimDirection.X >= 0f ? -1 : 1;
            npc.spriteDirection = desiredSpriteDirection;

            // Optional: also visually tilt toward the aim direction a bit while charging.
            float desiredRotation = aimDirection.X * 0.12f;
            npc.rotation = MathHelper.Lerp(npc.rotation, desiredRotation, ChargeTurnRate);

            if (BlastCooldown > 0f)
            {
                ChargeWindup = MathHelper.Lerp(ChargeWindup, 0f, 0.15f);
                return;
            }

            ChargeWindup = MathHelper.Lerp(ChargeWindup, 1f, 0.05f);

            if (ChargeWindup < ChargeReadyThreshold)
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 fireDirection = aimDirection;

            for(int i = 0; i< 3; i++)
            {
                Projectile.NewProjectile(
              npc.GetSource_FromThis(),
              npc.Center + fireDirection * 22f,
              fireDirection * 4f * (i+1),
              ModContent.ProjectileType<ConcussiveBlast>(),
              60,
              0f,
              Main.myPlayer
          );
            }
          
            npc.velocity -= fireDirection * 4;

            ChargeWindup = 0f;
            BlastCooldown = BlastCooldownTime;
            SoundEngine.PlaySound(SoundID.Item38 with { Pitch = -0.2f }, npc.Center);
            npc.netUpdate = true;
        }

       

        private void UpdateRotationAndDirection(NPC npc)
        {
            if (npc.velocity.X != 0f)
                npc.spriteDirection = npc.velocity.X > 0f ? -1 : 1;

            float targetRotation = npc.velocity.X * 0.08f;
            npc.rotation = MathHelper.Lerp(npc.rotation, targetRotation, 0.15f);
        }

        public override bool OverrideFindFrame(NPC npc)
        {
            int frame;

            if (BlastCooldown > BlastCooldownTime - 18)
                frame = 8 + (int)(Main.GameUpdateCount / 5 % 4);
            else if (NpcBrain?.Context.HasFoodTile == true)
                frame = 12 + (int)(Main.GameUpdateCount / 6 % 4);
            else
                frame = (int)(Main.GameUpdateCount / 7 % 8);

            npc.frame.Y = frame * npc.frame.Height;
            return true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = tex.Frame(1, TotalFrames, 0, npc.frame.Y / tex.FrameHeight());
            Vector2 drawPos = npc.Center - screenPos + new Vector2(0f, npc.gfxOffY);
            SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(
                tex,
                drawPos,
                frame,
                npc.GetAlpha(drawColor),
                npc.rotation,
                frame.Size() * 0.5f,
                npc.scale,
                effects,
                0
            );
            if (ChargeWindup>0.1f)
            {
                float pulse = 0.65f + 0.35f * (float)System.Math.Sin(Main.GameUpdateCount * 0.35f);
                Color glowColor = Color.Yellow * pulse * 0.75f * ChargeWindup;

                Main.EntitySpriteDraw(
                    tex,
                    drawPos,
                    frame,
                    glowColor,
                    npc.rotation,
                    frame.Size() * 0.5f,
                    npc.scale,
                    effects,
                    0
                );
            }
            //NpcBrain.DrawContextDebug(spriteBatch, drawPos);
         
            return false;
        }
    }
    public sealed class FleeThreatModule : INpcModule<GooGazerContext>
    {
        public float Score = 80f;
        public float MoveSpeed = 4f;

        public NpcDirective Evaluate(GooGazerContext context)
        {
            if (!context.HasThreat)
                return NpcDirective.None;

            Vector2 away = (context.Self.Center - context.ThreatPosition).SafeNormalize(Vector2.UnitX);
            Vector2 desired = away * MoveSpeed;

            desired.Y += (float)System.Math.Sin(Main.GameUpdateCount / 14f + context.Self.whoAmI) * 0.25f;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score + context.ThreatLevel * 20f,
                DesiredVelocity = desired,
                DebugName = "FleeThreat",
                DebugInfo = $"Threat={context.ThreatLevel:0.00}"
            };
        }
    }
    public sealed class SeekPlantyMushModule : INpcModule<GooGazerContext>
    {
        public float BaseScore = 18f;
        public float MaxScoreBonus = 30f;
        public float MoveSpeed = 2.2f;
        public float ArrivalDistance = 24f;

        public NpcDirective Evaluate(GooGazerContext context)
        {
            if (!context.WantsFood)
                return NpcDirective.None;

            if (!context.HasFoodTile || !context.HasTargetPoint)
                return NpcDirective.None;

            Vector2 toTarget = context.TargetPoint - context.Self.Center;
            float dist = toTarget.Length();

            if (dist <= ArrivalDistance)
            {
                return new NpcDirective
                {
                    WantsControl = true,
                    Score = BaseScore + context.FoodDrive * MaxScoreBonus,
                    DesiredVelocity = Vector2.Zero,
                    DebugName = "EatPlantyMush",
                    DebugInfo = $"Energy={context.Energy:0.00}"
                };
            }

            Vector2 desired = toTarget.SafeNormalize(Vector2.Zero) * MoveSpeed;

            return new NpcDirective
            {
                WantsControl = true,
                Score = BaseScore + context.FoodDrive * MaxScoreBonus,
                DesiredVelocity = desired,
                DebugName = "SeekPlantyMush",
                DebugInfo = $"Energy={context.Energy:0.00} Drive={context.FoodDrive:0.00}"
            };
        }
    }
    public sealed class GooGazerWanderModule : INpcModule<GooGazerContext>
    {
        public float Score = 5f;
        public float MoveSpeed = 1.4f;
        public int TurnInterval = 50;

        public NpcDirective Evaluate(GooGazerContext context)
        {
            if (context.LocalTimer % TurnInterval == 0)
                context.WanderAngle += Main.rand.NextFloat(-0.8f, 0.8f);

            Vector2 desired = context.WanderAngle.ToRotationVector2() * MoveSpeed;
            desired.Y += (float)System.Math.Sin(Main.GameUpdateCount / 18f + context.Self.whoAmI * 0.7f) * 0.35f;

            // soft terrain reaction
            Point front = (context.Self.Center + new Vector2((context.Self.spriteDirection == -1 ? 1 : -1) * 22f, 0f)).ToTileCoordinates();
            Point above = (context.Self.Center + new Vector2(0f, -18f)).ToTileCoordinates();
            Point below = (context.Self.Center + new Vector2(0f, 18f)).ToTileCoordinates();

            if (WorldGen.SolidTile(front.X, front.Y))
                context.WanderAngle += MathHelper.Pi * 0.65f;

            if (WorldGen.SolidTile(above.X, above.Y))
                desired.Y += 1.2f;

            if (WorldGen.SolidTile(below.X, below.Y))
                desired.Y -= 1.2f;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = desired,
                DebugName = "Wander",
                DebugInfo = $"Angle={context.WanderAngle:0.00}"
            };
        }
    }
    internal static class Texture2DExtensions
    {
        public static int FrameHeight(this Texture2D texture, int verticalFrames = 16) =>
            texture.Height / verticalFrames;
    }
}


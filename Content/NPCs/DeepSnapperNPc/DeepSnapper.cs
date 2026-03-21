using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.NPCs.DeepSnapperNPC
{
    internal class DeepSnapper : ModNPC
    {
        public ModularNpcBrain<SchoolingNpcContext> NpcBrain;

        private enum DeepSnapperState
        {
            Schooling,
            Reveal,
            Spit,
            Charge,
            Recover
        }
        private bool IsLeader;
        private int LeaderWhoAmI = -1;
        private Vector2 RoamTarget;
        private int RoamRetargetTime;
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(IsLeader);
            writer.Write(LeaderWhoAmI);
            writer.WriteVector2(RoamTarget);
            writer.Write(RoamRetargetTime);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            IsLeader = reader.ReadBoolean();
            LeaderWhoAmI = reader.ReadInt32();
            RoamTarget = reader.ReadVector2();
            RoamRetargetTime = reader.ReadInt32();
        }
        private ref float VariantFlag => ref NPC.ai[0];   // 0 = normal, 1 = infected
        private ref float StateTimer => ref NPC.ai[1];
        private ref float AttackCooldown => ref NPC.ai[2];
        private ref float HomeX => ref NPC.ai[3];
       
        private DeepSnapperState CurrentState
        {
            get => (DeepSnapperState)(int)NPC.localAI[0];
            set => NPC.localAI[0] = (float)value;
        }

        private bool Infected => VariantFlag >= 1f;

        private Player TargetPlayer
        {
            get
            {
                int index = Player.FindClosest(NPC.position, NPC.width, NPC.height);
                if (index < 0 || index >= Main.maxPlayers)
                    return null;

                Player p = Main.player[index];
                return p is { active: true, dead: false } ? p : null;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 2;
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 26;
            NPC.damage = 70;
            NPC.defense = 18;
            NPC.lifeMax = 1200;

            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.knockBackResist = 0.15f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.aiStyle = -1;

            SpawnModBiomes = new int[0];
        }
        #region Spawn A School
        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_Parent parentSource &&
                parentSource.Entity is NPC parentNpc &&
                parentNpc.active &&
                parentNpc.type == Type &&
                parentNpc.ModNPC is DeepSnapper parentSnapper)
            {
                IsLeader = false;
                LeaderWhoAmI = parentSnapper.IsLeader ? parentNpc.whoAmI : parentSnapper.LeaderWhoAmI;

                // Schoolmates are normal by default.
                VariantFlag = 0f;
                NPC.netUpdate = true;
                return;
            }

            // Root spawn becomes leader.
            IsLeader = true;
            LeaderWhoAmI = NPC.whoAmI;
            RoamTarget = NPC.Center;
            RoamRetargetTime = 0;

            // Only the leader rolls infection.
            VariantFlag = Main.rand.NextBool(4) ? 1f : 0f;

            SpawnInitialSchool();
            NPC.netUpdate = true;
        }
        private void SpawnInitialSchool()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int extraFish = Main.rand.Next(3, 7);

            for (int i = 0; i < extraFish; i++)
            {
                Vector2 spawnOffset = FindSchoolmateSpawnOffset(i, extraFish);
                Vector2 spawnWorldPosition = NPC.Center + spawnOffset;

                NPC npc = NPC.NewNPCDirect(
                    new EntitySource_Parent(NPC),
                    (int)spawnWorldPosition.X,
                    (int)spawnWorldPosition.Y,
                    Type,
                    ai0: 0f
                );

                if (npc is null || !npc.active)
                    continue;

                npc.velocity = new Vector2(
                    Main.rand.NextFloatDirection() * 0.8f,
                    Main.rand.NextFloat(-0.35f, 0.35f)
                );

                npc.netUpdate = true;
            }
        }

        private Vector2 FindSchoolmateSpawnOffset(int index, int total)
        {
            // Try several candidate positions near the original fish.
            // We prefer open water and avoid stuffing them inside solid tiles.
           

            // Fallback if all tested spots are bad.
            return new Vector2(Main.rand.NextFloat(-60f, 60f), Main.rand.NextFloat(-30f, 30f));
        }
        #endregion
        private void InitializeAnythingMissing()
        {
            if (NpcBrain is not null)
                return;

            SchoolingNpcContext context = new()
            {
                NeighborRadius = 220f,
                SeparationRadius = 52f,
                DesiredSchoolDistanceFromTarget = 96f
            };

            NpcBrain = new ModularNpcBrain<SchoolingNpcContext>(context);

            NpcBrain.Sensors.Add(new SchoolingSensor
            {
                SameTypeOnly = true,
                RequireLineOfSight = true,
                CanSchoolWith = (self, other) =>
                {
                    bool selfInfected = IsInfected(self);
                    bool otherInfected = IsInfected(other);

                    // Infected fish do not school with each other directly.
                    if (selfInfected && otherInfected)
                        return false;

                    return true;
                }
            });

            NpcBrain.Modules.Add(new AvoidSameTypeModule
            {
                AvoidanceRadius = 34f,
                PreferredSeparation = 42f,
                MaxMoveSpeed = 2.6f,
                BaseScore = 4f,
                CrowdingScoreMultiplier = 12f,
                RequireLineOfSight = false
            });
            NpcBrain.Modules.Add(new SchoolingMovementModule
            {
                SeparationWeight = 2.0f,
                AlignmentWeight = 3.15f,
                CohesionWeight = 1.85f,
                TargetWeight = 1.55f,
                MaxMoveSpeed = 5.0f,
                BaseScore = 12f,
                MinNeighborsToActivate = 1f
            });

            NpcBrain.Modules.Add(new IdleModule
            {
                SlowFactor = 0.96f,
                Score = 0.5f,
                Name = "Drift"
            });
        }

        public override bool PreAI()
        {
            InitializeAnythingMissing();
            NPC.noGravity = true;
            return true;
        }

        public override void AI()
        {
            AttackCooldown = Math.Max(0f, AttackCooldown - 1f);
            StateTimer++;

            Player target = TargetPlayer;

            switch (CurrentState)
            {
                default:
                case DeepSnapperState.Schooling:
                    DoSchoolingBehavior(target);
                    break;

                case DeepSnapperState.Reveal:
                    DoRevealBehavior(target);
                    break;

                case DeepSnapperState.Spit:
                    DoSpitBehavior(target);
                    break;

                case DeepSnapperState.Charge:
                    DoChargeBehavior(target);
                    break;

                case DeepSnapperState.Recover:
                    DoRecoverBehavior(target);
                    break;
            }

            ApplyWaterSteering();
            UpdateVisuals();
        }

        private void DoSchoolingBehavior(Player target)
        {
            if (NpcBrain is null)
                return;

            NPC leader = GetLeaderNPC();
            SchoolingNpcContext context = NpcBrain.Context;

            if (IsLeader || leader is null)
            {
                UpdateLeaderRoamTarget();

                context.HasSchoolTarget = true;
                context.SchoolTargetPosition = RoamTarget;
                context.DesiredSchoolDistanceFromTarget = 24f;
            }
            else
            {
                context.HasSchoolTarget = true;
                context.SchoolTargetPosition = leader.Center;

                // Slight per-fish variation so they do not stack in one exact ring.
                context.DesiredSchoolDistanceFromTarget = 72f + (NPC.whoAmI % 3) * 18f;
            }

            NpcBrain.Update(NPC);

            // Only infected leaders should initiate the ambush.
            if (!IsLeader || !Infected || target is null || AttackCooldown > 0f)
                return;

            float dist = Vector2.Distance(NPC.Center, target.Center);
            bool canSee = Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height);

            if (dist < 240f && canSee)
            {
                CurrentState = DeepSnapperState.Reveal;
                StateTimer = 0f;
                NPC.netUpdate = true;
            }
        }
      

        private void DoRevealBehavior(Player target)
        {
            NPC.velocity *= 0.94f;

            if (StateTimer == 1f)
                SoundEngine.PlaySound(SoundID.NPCHit13 with { Pitch = -0.3f }, NPC.Center);

            if (StateTimer >= 24f)
            {
                CurrentState = Main.rand.NextBool(2) ? DeepSnapperState.Spit : DeepSnapperState.Charge;
                StateTimer = 0f;
                NPC.netUpdate = true;
            }
        }

        private void DoSpitBehavior(Player target)
        {
            if (target is null)
            {
                EnterRecoverState();
                return;
            }

            Vector2 toTarget = target.Center - NPC.Center;
            Vector2 desired = toTarget.SafeNormalize(Vector2.UnitX * NPC.direction) * 1.6f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.08f);

            if (StateTimer %20 ==0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spitVelocity = toTarget.SafeNormalize(Vector2.UnitX) * 8f;
                spitVelocity += target.velocity * 0.12f;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center + spitVelocity.SafeNormalize(Vector2.UnitX) * 36f,
                    spitVelocity,
                    ProjectileID.EyeFire,
                    26,
                    0f,
                    Main.myPlayer
                );
            }

            if (StateTimer >= 40f)
                EnterRecoverState();
        }

        private void DoChargeBehavior(Player target)
        {
            if (target is null)
            {
                EnterRecoverState();
                return;
            }

            if (StateTimer < 14f)
            {
                Vector2 prepDir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX * NPC.direction);
                NPC.velocity = Vector2.Lerp(NPC.velocity, -prepDir * 1.4f, 0.08f);
            }
            else if (StateTimer >= 14f)
            {
                Vector2 lungeDir = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX * NPC.direction);
                NPC.velocity = lungeDir * 20*Utilities.InverseLerpBump(14, 20, 40, 50, StateTimer);
                NPC.netUpdate = true;
            }
            else
            {
                //NPC.velocity *= 0.985f;
            }

            if (StateTimer >= 50f)
                EnterRecoverState();
        }

        private void DoRecoverBehavior(Player target)
        {
            Vector2 homeBias = new Vector2(HomeX - NPC.Center.X, -8f);
            Vector2 desired = homeBias.SafeNormalize(Vector2.Zero) * 1.8f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.05f);

            if (StateTimer >= 48f)
            {
                CurrentState = DeepSnapperState.Schooling;
                StateTimer = 0f;
                AttackCooldown = 90f;
                NPC.netUpdate = true;
            }
        }

        private void EnterRecoverState()
        {
            CurrentState = DeepSnapperState.Recover;
            StateTimer = 0f;
            NPC.netUpdate = true;
        }

        private void ApplyWaterSteering()
        {
         
        }
        private NPC GetLeaderNPC()
        {
            if (IsLeader)
                return NPC;

            if (LeaderWhoAmI < 0 || LeaderWhoAmI >= Main.maxNPCs)
                return null;

            NPC leader = Main.npc[LeaderWhoAmI];
            if (!leader.active || leader.type != Type)
                return null;

            return leader;
        }

        private void UpdateLeaderRoamTarget()
        {
            if (!IsLeader)
                return;

            if (RoamRetargetTime > 0)
                RoamRetargetTime--;

            bool needsNewTarget =
                RoamRetargetTime <= 0 ||
                Vector2.Distance(NPC.Center, RoamTarget) < 36f ||
                IsSolidAtWorld(RoamTarget);

            if (!needsNewTarget)
                return;

            for (int i = 0; i < 20; i++)
            {
                Vector2 candidate = NPC.Center + new Vector2(
                    Main.rand.NextFloat(-220f, 220f),
                    Main.rand.NextFloat(-110f, 110f)
                );

                if (!IsSolidAtWorld(candidate))
                {
                    RoamTarget = candidate;
                    RoamRetargetTime = Main.rand.Next(60, 140);
                    NPC.netUpdate = true;
                    return;
                }
            }

            RoamTarget = NPC.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), Main.rand.NextFloat(-60f, 60f));
            RoamRetargetTime = 90;
            NPC.netUpdate = true;
        }
        private static bool IsSolidAtWorld(Vector2 worldPosition)
        {
            Point p = worldPosition.ToTileCoordinates();

            if (!WorldGen.InWorld(p.X, p.Y, 1))
                return false;

            Tile tile = Framing.GetTileSafely(p.X, p.Y);
            return tile.HasTile && Main.tileSolid[tile.TileType];
        }

        private void UpdateVisuals()
        {
            if (Math.Abs(NPC.velocity.X) > 0.08f)
                NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0f ? -1 : 1;

            NPC.rotation = NPC.velocity.X * 0.035f;

            if (CurrentState == DeepSnapperState.Charge)
                NPC.rotation += NPC.velocity.Y * 0.01f;
        }
        private bool ShouldLookInfected =>
    Infected &&
    (CurrentState == DeepSnapperState.Reveal ||
     CurrentState == DeepSnapperState.Spit ||
     CurrentState == DeepSnapperState.Charge);
        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = ShouldLookInfected ? frameHeight : 0;
        }
        private static bool IsInfected(NPC npc)
        {
            return npc.ModNPC is DeepSnapper snapper && snapper.Infected;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0 && Infected)
            {
                // Concept-art hook:
                // if the fish dies before the parasite is dealt with,
                // spawn your Mucklouse here and optionally let it “puppet” the corpse.
                //
                // Example once you have the NPC:
                // NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Mucklouse>());
            }
        }

        public override bool? CanFallThroughPlatforms() => true;

        
    }   
}

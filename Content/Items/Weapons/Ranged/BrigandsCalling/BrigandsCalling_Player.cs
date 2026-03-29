using CalamityMod;
using System.IO;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.BrigandsCalling
{
    internal class BrigandsCalling_Player : ModPlayer
    {
        public bool Active => Player.HeldItem.type == ModContent.ItemType<BrigandsCalling_Item>();

        public int HitCount;
        public int ForcedTargetIndex = -1;

        public NPC ForcedTarget =>
            ForcedTargetIndex >= 0 &&
            ForcedTargetIndex < Main.maxNPCs &&
            Main.npc[ForcedTargetIndex].active
                ? Main.npc[ForcedTargetIndex]
                : null;

        public List<Projectile> WaterSpouts
        {
            get
            {
                List<Projectile> result = new(MAX_WATER_SPOUTS);

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (!proj.active)
                        continue;

                    if (proj.owner != Player.whoAmI)
                        continue;

                    if (proj.type != SpoutType)
                        continue;

                    result.Add(proj);
                }

                result.Sort((a, b) => a.ai[0].CompareTo(b.ai[0]));
                return result;
            }
        }

        private int FindFreeWaterSpoutSlot()
        {
            bool[] used = new bool[MAX_WATER_SPOUTS];

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != Player.whoAmI || proj.type != SpoutType)
                    continue;

                int slot = (int)proj.ai[0];
                if (slot >= 0 && slot < MAX_WATER_SPOUTS)
                    used[slot] = true;
            }

            for (int i = 0; i < MAX_WATER_SPOUTS; i++)
            {
                if (!used[i])
                    return i;
            }

            return -1;
        }

        public int RPMBoost;
        public int HitCountForWaterSpout => 20;

        // Flip dash state
        public bool IsFlipDashing;
        public int FlipDashTimer;
        public int FlipDashCooldown;
        public int LastFlipTarget = -1;

        private Vector2 _flipDashDirection;

        private const int MAX_WATER_SPOUTS = 6;
        private const int FLIP_DASH_TIME = 40;
        private const int FLIP_DASH_COOLDOWN = 10 * 60;
        private const float FLIP_DASH_SPEED = 28f;
        private const float FLIP_BOUNCE_SPEED = 14f;
        private const float FLIP_DASH_DAMAGE_MULT = 2.35f;
        private const int FLIP_RPM_GAIN = 200;

        public int ForceuseItemTime;

        private HashSet<int> _waterSpouts = new HashSet<int>();
        private List<Projectile> _cachedWaterSpouts;
        private static int SpoutType => ModContent.ProjectileType<BrigandsCalling_WaterSpout>();

        public class _BrigandsCallingProjectile : GlobalProjectile
        {
            public override bool InstancePerEntity => true;
            public bool IsBrigandsCallingProjectile = false;
            public bool SuperHome = false;
            public override void OnSpawn(Projectile projectile, IEntitySource source)
            {
                if (source.Context is not null && source.Context.ToString().Contains("BrigandsCalling"))
                {
                    IsBrigandsCallingProjectile = true;

                    if (source.Context.ToString().Contains("SuperHome"))
                        SuperHome = true;

                    projectile.netUpdate = true;
                }
            }

            public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
            {
                bitWriter.WriteBit(IsBrigandsCallingProjectile);
                bitWriter.WriteBit(SuperHome);
            }

            public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
            {
                IsBrigandsCallingProjectile = bitReader.ReadBit();
                SuperHome = bitReader.ReadBit();
            }
            public override void AI(Projectile projectile)
            {
                if (!IsBrigandsCallingProjectile)
                    return;

                if (SuperHome)
                {
                    NPC thing = Main.player[projectile.owner]
                        .GetModPlayer<BrigandsCalling_Player>()
                        .ForcedTarget;

                    if (thing is not null)
                        projectile.SuperhomeTowardsTarget(thing, 100, 4, 1);
                }
            }
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Active)
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (proj.GetGlobalProjectile<_BrigandsCallingProjectile>().IsBrigandsCallingProjectile)
                HitCount++;
        }
        public override void PostUpdateMiscEffects()
        {
            if (!Active && !IsFlipDashing)
                return;

            if (ForceuseItemTime > 0)
                ForceuseItemTime--;
            else
                ForcedTargetIndex = -1;

            if (HitCount > 0 &&
                HitCount % HitCountForWaterSpout == 0 &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnWaterSpout();
                HitCount++;
            }

            if (ForceuseItemTime < 1 && !Player.HasBuff(ModContent.BuffType<BrigandsCalling_Buff>()))
                RPMBoost = (int)MathHelper.Lerp(RPMBoost, 0f, 0.1f);
        }
        #region FlipDash
        public bool TryStartFlipDashFromMouse()
        {
            return TryStartFlipDash(Player.DirectionTo(Main.MouseWorld));
        }

        public bool TryStartFlipDash(Vector2 desiredDirection)
        {
            if (Player.dead || !Player.active)
                return false;

            if (IsFlipDashing || FlipDashCooldown > 0)
                return false;

            desiredDirection = ClampFlipDashDirection(desiredDirection);

            IsFlipDashing = true;
            FlipDashTimer = FLIP_DASH_TIME;
            LastFlipTarget = -1;
            ForcedTargetIndex = -1;
            _flipDashDirection = desiredDirection;

            Player.velocity = desiredDirection * FLIP_DASH_SPEED;
            Player.ChangeDir(Player.velocity.X >= 0f ? 1 : -1);
            Player.fallStart = (int)(Player.position.Y / 16f);
            

            SendSync();
            return true;
        }
        private Vector2 ClampFlipDashDirection(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                direction = new Vector2(Player.direction, 1f);

            direction = direction.SafeNormalize(new Vector2(Player.direction, 1f));

            // Force the dash into the lower 180-degree arc.
            direction.Y = System.MathF.Max(direction.Y, 0.18f);

            return direction.SafeNormalize(new Vector2(Player.direction, 1f));
        }

        private void CheckFlipDashHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer)
                return;

            Rectangle dashHitbox = Player.Hitbox;
            dashHitbox.Inflate(10, 10);

            Rectangle predictedHitbox = dashHitbox;
            predictedHitbox.Offset((int)Player.velocity.X, (int)Player.velocity.Y);

            dashHitbox = Rectangle.Union(dashHitbox, predictedHitbox);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy())
                    continue;

                if (npc.whoAmI == LastFlipTarget)
                    continue;

                if (!dashHitbox.Intersects(npc.Hitbox))
                    continue;

                PerformFlipBounce(npc);
                break;
            }
        }

        public Entity target;
        private void PerformFlipBounce(NPC target)
        {
            ForceuseItemTime = 70;
            ForcedTargetIndex = target.whoAmI;

            int damage = Player.GetWeaponDamage(Player.HeldItem);
            damage = (int)(damage * FLIP_DASH_DAMAGE_MULT);

            int hitDirection = Player.Center.X < target.Center.X ? 1 : -1;

            // Only server/singleplayer should apply real damage.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player.ApplyDamageToNPC(target, damage, Player.HeldItem.knockBack, hitDirection, false);
                target.immune[Player.whoAmI] = System.Math.Max(target.immune[Player.whoAmI], 10);
            }

            Vector2 reboundDirection = (Player.Center - target.Center).SafeNormalize(new Vector2(-Player.direction, -1f));
            reboundDirection.Y = -System.MathF.Abs(reboundDirection.Y) - 0.35f;
            reboundDirection = reboundDirection.SafeNormalize(new Vector2(-Player.direction, -1f));

            Player.velocity = reboundDirection * FLIP_BOUNCE_SPEED;
            Player.ChangeDir(Player.velocity.X >= 0f ? 1 : -1);

            LastFlipTarget = target.whoAmI;
            RPMBoost += FLIP_RPM_GAIN;

            StopFlipDash(false);
            SendSync();
        }

        private void StopFlipDash(bool startCooldown)
        {
            IsFlipDashing = false;
            FlipDashTimer = 0;
            Player.fullRotation = 0f;

            if (startCooldown)
                FlipDashCooldown = FLIP_DASH_COOLDOWN;
        }

        #endregion
        public override void PreUpdateMovement()
        {
            if (!Active)
                return;
            if (FlipDashCooldown > 0)
                FlipDashCooldown--;
           

            if (!IsFlipDashing)
                return;

            if (Player.dead || !Player.active)
            {
                StopFlipDash(false);
                return;
            }

            if (FlipDashTimer <= 0)
            {
                StopFlipDash(false);
                return;
            }

            float progress = FlipDashTimer / (float)FLIP_DASH_TIME;
            float spinDirection = _flipDashDirection.X == 0f ? Player.direction : System.Math.Sign(_flipDashDirection.X);

            Player.fullRotationOrigin = Player.Size * 0.5f;
            Player.fullRotation = MathHelper.Lerp(0f, -spinDirection, progress);

            Player.velocity = _flipDashDirection * FLIP_DASH_SPEED;
            Player.maxFallSpeed = FLIP_DASH_SPEED;
            Player.fallStart = (int)(Player.position.Y / 16f);

            Player.immune = true;
            Player.immuneNoBlink = true;
            if (Player.immuneTime < 2)
                Player.immuneTime = 2;

            CheckFlipDashHit();

            FlipDashTimer--;
        }

        public void SpawnWaterSpout()
        {
            Vector2 SpawnPos = Player.Center + Main.rand.NextVector2CircularEdge(130, 130);



            Projectile spout = Projectile.NewProjectileDirect(Player.GetSource_FromThis(), SpawnPos, Vector2.zeroVector, SpoutType, 100, 0);

            if (_waterSpouts.Count < MAX_WATER_SPOUTS)
                _waterSpouts.Add(spout.whoAmI);
        }



        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SendSync(toWho, fromWho);
        }
        #region Netsyncing

        public override void CopyClientState(ModPlayer targetCopy)
        {
            BrigandsCalling_Player clone = (BrigandsCalling_Player)targetCopy;

            clone.IsFlipDashing = IsFlipDashing;
            clone.FlipDashTimer = FlipDashTimer;
            clone.FlipDashCooldown = FlipDashCooldown;
            clone.LastFlipTarget = LastFlipTarget;
            clone._flipDashDirection = _flipDashDirection;
            clone.ForceuseItemTime = ForceuseItemTime;
            clone.RPMBoost = RPMBoost;
            clone.ForcedTargetIndex = ForcedTargetIndex;

        }
        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            BrigandsCalling_Player clone = (BrigandsCalling_Player)clientPlayer;

            if (clone.IsFlipDashing != IsFlipDashing ||
                clone.FlipDashTimer != FlipDashTimer ||
                clone.FlipDashCooldown != FlipDashCooldown ||
                clone.LastFlipTarget != LastFlipTarget ||
                clone._flipDashDirection != _flipDashDirection ||
                clone.ForceuseItemTime != ForceuseItemTime ||
                clone.RPMBoost != RPMBoost ||
                clone.ForcedTargetIndex != ForcedTargetIndex)
            {
                SendSync();
            }
        }

        public void SendSync(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AbyssOverhaul.AbyssOverhaulMessageType.SyncBrigandsCallingPlayer);
            packet.Write((byte)Player.whoAmI);

            packet.Write(IsFlipDashing);
            packet.Write(FlipDashTimer);
            packet.Write(FlipDashCooldown);
            packet.Write(LastFlipTarget);

            packet.Write(_flipDashDirection.X);
            packet.Write(_flipDashDirection.Y);

            packet.Write(ForceuseItemTime);
            packet.Write(RPMBoost);
            packet.Write(ForcedTargetIndex);

            packet.Send(toWho, fromWho);
        }

        public void ReceiveSync(BinaryReader reader)
        {
            IsFlipDashing = reader.ReadBoolean();
            FlipDashTimer = reader.ReadInt32();
            FlipDashCooldown = reader.ReadInt32();
            LastFlipTarget = reader.ReadInt32();

            _flipDashDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            ForceuseItemTime = reader.ReadInt32();
            RPMBoost = reader.ReadInt32();
            ForcedTargetIndex = reader.ReadInt32();
        }
        #endregion
    }
}

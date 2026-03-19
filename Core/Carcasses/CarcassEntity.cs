using System.IO;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.Carcasses
{
    public class CarcassEntity : Entity
    {
        public int LeechSpawnTimer;
        public int TotalLeechesSpawned;
        public int LastLeechCheckTimer;

        public int ID;
        public int FleshRemaining;
        public bool PendingDelete;
        public int TimeAlive;
        public float Rotation;
        public CarcassSnapshot Snapshot;

        public float AngularVelocity;
        public float MaxPushSpeed => 3.5f;

        public bool Settled;
        public bool OnGround;
        public bool LeftSupport;
        public bool CenterSupport;
        public bool RightSupport;

        public float GroundFriction;
        public bool Active => active && !PendingDelete && FleshRemaining > 0;

        public bool NetDirty { get; set; }
        public int NetSyncCooldown { get; internal set; }

        public CarcassEntity()
        {
            active = true;
            direction = 1;
        }


        public void Update()
        {
            oldPosition = position;
            oldVelocity = velocity;
            oldDirection = direction;

            float oldRotation = Rotation;
            float oldAngularVelocity = AngularVelocity;
            int oldFlesh = FleshRemaining;

            TimeAlive++;
            HandlePlayerCollisionAndPush();
            ApplyGravity();
            MoveWithTileCollisionAndGrounding();
            ApplyRotationalPhysics();

            if (FleshRemaining <= 0)
            {
                FleshRemaining = 0;
                PendingDelete = true;
                active = false;
            }

            if (position != oldPosition ||
                velocity != oldVelocity ||
                FleshRemaining != oldFlesh ||
                Math.Abs(Rotation - oldRotation) > 0.0001f ||
                Math.Abs(AngularVelocity - oldAngularVelocity) > 0.0001f)
            {
                NetDirty = true;
            }
        }

        private void ApplyGravity()
        {
            velocity.Y += 0.25f;
            if (velocity.Y > 10f)
                velocity.Y = 10f;
        }

        public void MoveWithTileCollisionAndGrounding()
        {
            Vector2 intendedVelocity = velocity;
            Vector2 resolvedVelocity = Collision.TileCollision(position, velocity, width, height, false, false);

            position += resolvedVelocity;

            bool hitX = resolvedVelocity.X != intendedVelocity.X;
            bool hitY = resolvedVelocity.Y != intendedVelocity.Y;

            if (hitX)
            {
                AngularVelocity += intendedVelocity.X * 0.015f;
                velocity.X = 0f;
            }
            else
                velocity.X = resolvedVelocity.X;

            if (hitY)
            {
                if (intendedVelocity.Y > 0f)
                    velocity.Y = 0f;
                else
                    velocity.Y = resolvedVelocity.Y;
            }
            else
                velocity.Y = resolvedVelocity.Y;

            UpdateRotatingGroundSupport();

            // Falling onto support produces tumble.
            if (OnGround && intendedVelocity.Y > 1f)
                AngularVelocity += velocity.X * 0.025f;

            // If only one side is supported, the body should tip.
            if (OnGround)
            {
                if (LeftSupport && !RightSupport)
                    AngularVelocity -= 0.006f;

                if (RightSupport && !LeftSupport)
                    AngularVelocity += 0.006f;
            }
        }

        private void ApplyRotationalPhysics()
        {
            if (OnGround)
            {
                float grip = CenterSupport ? 0.42f : 0.28f;
                float rollCoupling = CenterSupport ? 0.010f : 0.0075f;

                // Spin feeds lateral motion.
                velocity.X += AngularVelocity * grip;

                // Lateral motion feeds spin.
                AngularVelocity += velocity.X * rollCoupling;

                // Uneven support creates settling torque.
                if (LeftSupport && !RightSupport)
                    AngularVelocity -= 0.010f;
                else if (RightSupport && !LeftSupport)
                    AngularVelocity += 0.010f;

                // Stronger damping on ground.
                velocity.X *= 0.88f;
                AngularVelocity *= 0.84f;

                // Settle toward nearest flat rest angle.
                float targetRotation = FindNearestRestAngle(Rotation);
                float angleDelta = WrapAngle(targetRotation - Rotation);

                // More tilted bodies should want to settle harder.
                float settleStrength = 0.018f + Math.Abs((float)Math.Sin(Rotation)) * 0.02f;
                AngularVelocity += angleDelta * settleStrength;

                if (Math.Abs(velocity.X) < 0.03f)
                    velocity.X = 0f;

                if (Math.Abs(AngularVelocity) < 0.0008f)
                    AngularVelocity = 0f;

                AngularVelocity *= 0.9f;
            }
            else
            {
                velocity.X *= 0.985f;
                AngularVelocity *= 0.992f;
            }

            AngularVelocity = MathHelper.Clamp(AngularVelocity, -0.22f, 0.22f);
            AngularVelocity = AngularVelocity.AngleLerp(0, 0.2f);
            Rotation += AngularVelocity;
            Rotation = WrapAngle(Rotation);
        }
        private static float FindNearestRestAngle(float angle)
        {
            float a = WrapAngle(angle);
            float optionA = 0f;
            float optionB = MathHelper.Pi;

            float distA = Math.Abs(WrapAngle(a - optionA));
            float distB = Math.Abs(WrapAngle(a - optionB));

            return angle;
        }

        private static float WrapAngle(float angle)
        {
            while (angle > MathHelper.Pi)
                angle -= MathHelper.TwoPi;
            while (angle < -MathHelper.Pi)
                angle += MathHelper.TwoPi;
            return angle;
        }
        private void HandlePlayerCollisionAndPush()
        {
            Rectangle broadphase = Hitbox;
            broadphase.Inflate(12, 12);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player is null || !player.active || player.dead)
                    continue;

                Rectangle playerBox = player.Hitbox;

                if (!playerBox.Intersects(broadphase))
                    continue;

                if (!RotatingIntersectsRect(playerBox, 1f))
                    continue;

                ResolvePlayerOverlap(player, Hitbox, playerBox);
                ApplyPlayerPushImpulse(player);
            }
        }


        private void UpdateRotatingGroundSupport()
        {
            // Small probe bands below the carcass.
            int probeHeight = 10;
            int y = (int)Bottom.Y - 2;

            Rectangle leftProbe = new Rectangle((int)position.X - 4, y, width / 3 + 8, probeHeight);
            Rectangle centerProbe = new Rectangle((int)position.X + width / 3, y, width / 3, probeHeight);
            Rectangle rightProbe = new Rectangle((int)position.X + (width * 2 / 3) - 4, y, width / 3 + 8, probeHeight);

            LeftSupport = RotatingHitsSolidTilesInArea(leftProbe, 1f);
            CenterSupport = RotatingHitsSolidTilesInArea(centerProbe, 1f);
            RightSupport = RotatingHitsSolidTilesInArea(rightProbe, 1f);

            OnGround = LeftSupport || CenterSupport || RightSupport;
        }

        private void ResolvePlayerOverlap(Player player, Rectangle carcassBox, Rectangle playerBox)
        {
            int overlapLeft = playerBox.Right - carcassBox.Left;
            int overlapRight = carcassBox.Right - playerBox.Left;
            int overlapTop = playerBox.Bottom - carcassBox.Top;
            int overlapBottom = carcassBox.Bottom - playerBox.Top;

            if (overlapLeft <= 0 || overlapRight <= 0 || overlapTop <= 0 || overlapBottom <= 0)
                return;

            int minHorizontal = Math.Min(overlapLeft, overlapRight);
            int minVertical = Math.Min(overlapTop, overlapBottom);

            bool playerMostlyAbove =
             player.oldPosition.Y + player.height <= Top.Y + 8f &&
             player.velocity.Y >= 0f;




        }

        private void ApplyPlayerPushImpulse(Player player)
        {
            float pushStrength = OnGround ? 0.22f : 0.12f;

            velocity.X += player.velocity.X * pushStrength;

            if (Math.Abs(player.velocity.X) < 0.05f)
                velocity.X += player.direction * 0.05f;

            velocity.X = MathHelper.Clamp(velocity.X, -3.5f, 3.5f);

            // Side pushes add torque.
            AngularVelocity += player.velocity.X * 0.01f;

            // Landing on top creates a little tumble.
            if (player.velocity.Y > 1.5f && player.Bottom.Y <= Top.Y + 12f)
                AngularVelocity += player.direction * 0.03f;
        }

        private Vector2 GetRotatingHitboxDirection()
        {
            // This helper wants a line direction.
            // Use actual visual rotation first, and fall back to movement when nearly flat/undefined.
            Vector2 dir = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));

            if (dir.LengthSquared() < 0.0001f)
                dir = velocity.SafeNormalize(Vector2.UnitX * direction);

            if (dir.LengthSquared() < 0.0001f)
                dir = Vector2.UnitX * (direction == 0 ? 1 : direction);

            return Vector2.Normalize(dir);
        }

        private bool RotatingIntersectsRect(Rectangle rect, float scale = 1f)
        {
            return this.RotatingHitboxCollision(
                new Vector2(rect.X, rect.Y),
                new Vector2(rect.Width, rect.Height),
                GetRotatingHitboxDirection(),
                scale);
        }

        private static bool IsSolidTile(int x, int y)
        {
            if (!Terraria.WorldGen.InWorld(x, y, 1))
                return false;

            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.IsActuated)
                return false;

            return Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        private bool RotatingHitsSolidTilesInArea(Rectangle worldArea, float scale = 1f)
        {
            int minTileX = Math.Max(0, worldArea.Left / 16);
            int maxTileX = Math.Min(Main.maxTilesX - 1, worldArea.Right / 16);
            int minTileY = Math.Max(0, worldArea.Top / 16);
            int maxTileY = Math.Min(Main.maxTilesY - 1, worldArea.Bottom / 16);

            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    if (!IsSolidTile(x, y))
                        continue;

                    Rectangle tileRect = new Rectangle(x * 16, y * 16, 16, 16);
                    if (RotatingIntersectsRect(tileRect, scale))
                        return true;
                }
            }

            return false;
        }


        public TagCompound Save()
        {
            return new TagCompound
            {
                ["LeechSpawnTimer"] = LeechSpawnTimer,
                ["TotalLeechesSpawned"] = TotalLeechesSpawned,
                ["LastLeechCheckTimer"] = LastLeechCheckTimer,
                ["ID"] = ID,
                ["PositionX"] = position.X,
                ["PositionY"] = position.Y,
                ["VelocityX"] = velocity.X,
                ["VelocityY"] = velocity.Y,
                ["FleshRemaining"] = FleshRemaining,
                ["PendingDelete"] = PendingDelete,
                ["TimeAlive"] = TimeAlive,
                ["Rotation"] = Rotation,
                ["Snapshot"] = Snapshot?.Save() ?? new TagCompound(),
                ["Hitbox"] = Hitbox
            };
        }

        public static CarcassEntity Load(TagCompound tag)
        {
            return new CarcassEntity
            {
                LeechSpawnTimer = tag.GetInt("LeechSpawnTimer"),
                TotalLeechesSpawned = tag.GetInt("TotalLeechesSpawned"),
                LastLeechCheckTimer = tag.GetInt("LastLeechCheckTimer"),
                ID = tag.GetInt("ID"),
                position = new Vector2(tag.GetFloat("PositionX"), tag.GetFloat("PositionY")),
                velocity = new Vector2(tag.GetFloat("VelocityX"), tag.GetFloat("VelocityY")),
                FleshRemaining = tag.GetInt("FleshRemaining"),
                PendingDelete = tag.GetBool("PendingDelete"),
                TimeAlive = tag.GetInt("TimeAlive"),
                Rotation = tag.GetFloat("Rotation"),
                Snapshot = CarcassSnapshot.Load(tag.GetCompound("Snapshot")),
                Hitbox = tag.Get<Rectangle>("Hitbox")
            };
        }

        public void NetSend(BinaryWriter writer)
        {
            writer.Write(ID);

            writer.Write(active);

            writer.Write(position.X);
            writer.Write(position.Y);

            writer.Write(velocity.X);
            writer.Write(velocity.Y);

            writer.Write(oldPosition.X);
            writer.Write(oldPosition.Y);

            writer.Write(oldVelocity.X);
            writer.Write(oldVelocity.Y);

            writer.Write(direction);
            writer.Write(oldDirection);

            writer.Write(width);
            writer.Write(height);

            writer.Write(FleshRemaining);
            writer.Write(PendingDelete);
            writer.Write(TimeAlive);
            writer.Write(Rotation);
            writer.Write(AngularVelocity);
            Snapshot.NetSend(writer);
        }

        public static CarcassEntity NetReceive(BinaryReader reader)
        {
            CarcassEntity carcass = new CarcassEntity();

            carcass.ID = reader.ReadInt32();

            carcass.active = reader.ReadBoolean();

            carcass.position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            carcass.velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            carcass.oldPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            carcass.oldVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            carcass.direction = reader.ReadInt32();
            carcass.oldDirection = reader.ReadInt32();

            carcass.width = reader.ReadInt32();
            carcass.height = reader.ReadInt32();

            carcass.FleshRemaining = reader.ReadInt32();
            carcass.PendingDelete = reader.ReadBoolean();
            carcass.TimeAlive = reader.ReadInt32();
            carcass.Rotation = reader.ReadSingle();
            carcass.AngularVelocity = reader.ReadSingle();

            carcass.Snapshot = CarcassSnapshot.NetReceive(reader);
            return carcass;
        }
    }
}


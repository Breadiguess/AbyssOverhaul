using AbyssOverhaul.Common.Brain;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace AbyssOverhaul.Content.NPCs.Critters.VoidDreamerNPC
{
    public sealed class ObservationReactionModule : INpcModule<ObservationNpcContext>
    {
        public float WallAvoidanceRange { get; set; } = 72f;
        public float FloorAvoidanceRange { get; set; } = 88f;
        public float CeilingAvoidanceRange { get; set; } = 56f;

        public float WallAvoidanceStrength { get; set; } = 2.4f;
        public float FloorAvoidanceStrength { get; set; } = 3.2f;
        public float CeilingAvoidanceStrength { get; set; } = 1.6f;

        public float MaxDesiredSpeed { get; set; } = 6f;
        public float HoverWaveStrength { get; set; } = 0.35f;

        public NpcDirective Evaluate(ObservationNpcContext context)
        {
            if (!context.HasObservedThing)
                return NpcDirective.None;

            Vector2 selfCenter = context.Self.Center;
            Vector2 offset = context.ObservedPosition - selfCenter;
            float dist = offset.Length();

            if (dist <= 0.001f)
                return NpcDirective.None;

            Vector2 dir = offset / dist;
            Vector2 desiredVelocity = Vector2.Zero;
            float score = 1f;

            float desiredDistance = context.DesiredObservationDistance <= 0f ? 160f : context.DesiredObservationDistance;

            if (context.SawHostileProjectile)
            {
                desiredVelocity = -dir * 12f;
                score = 20f;
            }
            else if (context.SawWeaponSwing)
            {
                desiredVelocity = -dir * 12f;
                score = 12f;
            }
            else
            {
                float tolerance = 24f;

                if (dist < desiredDistance - tolerance)
                    desiredVelocity = -dir * 3f;
                else if (dist > desiredDistance + tolerance)
                    desiredVelocity = dir * 3f;
                else
                    desiredVelocity = Vector2.Zero;

                score = 4f;
            }

            // Gentle hover motion so it does not look dead still.
            desiredVelocity += Vector2.UnitY * (MathF.Cos((float)Main.GameUpdateCount * 0.05f + context.Self.whoAmI*12) * HoverWaveStrength);

            // Push away from nearby solid geometry.
            desiredVelocity += ComputeEnvironmentAvoidance(context);

            if (desiredVelocity.LengthSquared() > MaxDesiredSpeed * MaxDesiredSpeed)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * MaxDesiredSpeed;

            return new NpcDirective
            {
                WantsControl = true,
                Score = score,
                DesiredVelocity = desiredVelocity,
                DebugName = nameof(ObservationReactionModule),
                DebugInfo =
                    $"ObservedDist={dist:0.0}, Swing={context.SawWeaponSwing}, HostileProj={context.SawHostileProjectile}, DesiredVel={desiredVelocity}"
            };
        }

        private Vector2 ComputeEnvironmentAvoidance(ObservationNpcContext context)
        {
            NPC npc = context.Self;
            Vector2 center = npc.Center;
            Vector2 avoidance = Vector2.Zero;

            // Left wall
            float leftHit = GetDistanceToSolid(center, -Vector2.UnitX, WallAvoidanceRange, npc);
            if (leftHit >= 0f)
            {
                float t = 1f - leftHit / WallAvoidanceRange;
                avoidance += Vector2.UnitX * t * WallAvoidanceStrength;
            }

            // Right wall
            float rightHit = GetDistanceToSolid(center, Vector2.UnitX, WallAvoidanceRange, npc);
            if (rightHit >= 0f)
            {
                float t = 1f - rightHit / WallAvoidanceRange;
                avoidance += -Vector2.UnitX * t * WallAvoidanceStrength;
            }

            // Floor
            float downHit = GetDistanceToSolid(center, Vector2.UnitY, FloorAvoidanceRange, npc);
            if (downHit >= 0f)
            {
                float t = 1f - downHit / FloorAvoidanceRange;
                avoidance += -Vector2.UnitY * t * FloorAvoidanceStrength;
            }

            // Ceiling
            float upHit = GetDistanceToSolid(center, -Vector2.UnitY, CeilingAvoidanceRange, npc);
            if (upHit >= 0f)
            {
                float t = 1f - upHit / CeilingAvoidanceRange;
                avoidance += Vector2.UnitY * t * CeilingAvoidanceStrength;
            }

            // Optional diagonal probes help avoid corners feeling sticky.
            avoidance += ComputeDiagonalAvoidance(center, npc);

            return avoidance;
        }

        private Vector2 ComputeDiagonalAvoidance(Vector2 center, NPC npc)
        {
            Vector2 result = Vector2.Zero;

            Vector2[] diagonals =
            {
                Vector2.Normalize(new Vector2(-1f, -1f)),
                Vector2.Normalize(new Vector2( 1f, -1f)),
                Vector2.Normalize(new Vector2(-1f,  1f)),
                Vector2.Normalize(new Vector2( 1f,  1f))
            };

            const float diagonalRange = 52f;
            const float diagonalStrength = 1.15f;

            foreach (Vector2 dir in diagonals)
            {
                float hit = GetDistanceToSolid(center, dir, diagonalRange, npc);
                if (hit < 0f)
                    continue;

                float t = 1f - hit / diagonalRange;
                result -= dir * t * diagonalStrength;
            }

            return result;
        }

        /// <summary>
        /// Returns the distance to the first solid tile hit along a ray, or -1 if none were found.
        /// </summary>
        private float GetDistanceToSolid(Vector2 origin, Vector2 direction, float maxDistance, NPC npc)
        {
            if (direction.LengthSquared() <= 0.0001f)
                return -1f;

            direction.Normalize();

            const float step = 8f;

            for (float d = step; d <= maxDistance; d += step)
            {
                Vector2 sample = origin + direction * d;

                int tileX = (int)(sample.X / 16f);
                int tileY = (int)(sample.Y / 16f);

                if (!WorldGen.InWorld(tileX, tileY, 10))
                    return d;

                Tile tile = Framing.GetTileSafely(tileX, tileY);

                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    return d;
            }

            return -1f;
        }
    }
}
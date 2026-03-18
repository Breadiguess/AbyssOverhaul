using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Core.Utilities;
using Microsoft.Xna.Framework;
using Terraria;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    public sealed class ThreatAwarenessSensor<TContext> : INpcSensor<TContext>
        where TContext : CreatureNpcContext
    {
        public float PlayerThreatRadius = 360f;
        public float ProjectileThreatRadius = 280f;
        public float NpcThreatRadius = 300f;
        public float DisturbanceRadius = 220f;

        public void Update(TContext context)
        {
            NPC self = context.Self;
            if (self is null || !self.active)
                return;

            context.HasThreat = false;
            context.ThreatPosition = Vector2.Zero;
            context.ThreatLevel = 0f;

            context.HasDisturbance = false;
            context.DisturbancePosition = Vector2.Zero;

            float bestThreatScore = 0f;
            Vector2 bestThreatPosition = Vector2.Zero;

            SensePlayers(self, ref bestThreatScore, ref bestThreatPosition);
            SenseProjectiles(self, ref bestThreatScore, ref bestThreatPosition);
            SenseLargeHostileNpcs(self, ref bestThreatScore, ref bestThreatPosition);
            SenseDisturbance(context, self);

            if (bestThreatScore > 0f)
            {
                context.HasThreat = true;
                context.ThreatPosition = bestThreatPosition;
                context.ThreatLevel = MathHelper.Clamp(bestThreatScore, 0f, 1f);
                context.TimeSinceThreatSeen = 0;
            }
            else
                context.TimeSinceThreatSeen++;
        }

        private void SensePlayers(NPC self, ref float bestThreatScore, ref Vector2 bestThreatPosition)
        {
            float maxDistSq = PlayerThreatRadius * PlayerThreatRadius;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player is null || !player.active || player.dead)
                    continue;

                float distSq = Vector2.DistanceSquared(self.Center, player.Center);
                if (distSq > maxDistSq)
                    continue;

                float dist = MathF.Sqrt(distSq);
                float proximityScore = 1f - dist / PlayerThreatRadius;
                float motionScore = MathHelper.Clamp(player.velocity.Length() / 10f, 0f, 1f);
                float actionScore = player.itemAnimation > 0 ? 0.35f : 0f;

                float totalScore = proximityScore * 0.65f + motionScore * 0.2f + actionScore;

                if (Collision.CanHit(self.Center, 1, 1, player.Center, 1, 1))
                    totalScore += 0.1f;

                if (totalScore > bestThreatScore)
                {
                    bestThreatScore = totalScore;
                    bestThreatPosition = player.Center;
                }
            }
        }

        private void SenseProjectiles(NPC self, ref float bestThreatScore, ref Vector2 bestThreatPosition)
        {
            float maxDistSq = ProjectileThreatRadius * ProjectileThreatRadius;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj is null || !proj.active)
                    continue;

                bool dangerous =
                    proj.hostile ||
                    proj.damage > 0 ||
                    proj.velocity.LengthSquared() > 9f;

                if (!dangerous)
                    continue;

                float distSq = Vector2.DistanceSquared(self.Center, proj.Center);
                if (distSq > maxDistSq)
                    continue;

                float dist = MathF.Sqrt(distSq);
                float proximityScore = 1f - dist / ProjectileThreatRadius;
                float speedScore = MathHelper.Clamp(proj.velocity.Length() / 14f, 0f, 1f);

                Vector2 toSelf = self.Center - proj.Center;
                float approachScore = 0f;

                if (proj.velocity != Vector2.Zero && toSelf != Vector2.Zero)
                {
                    Vector2 projDir = Vector2.Normalize(proj.velocity);
                    Vector2 toSelfDir = Vector2.Normalize(toSelf);
                    approachScore = MathHelper.Clamp(Vector2.Dot(projDir, toSelfDir), 0f, 1f);
                }

                float totalScore = proximityScore * 0.5f + speedScore * 0.2f + approachScore * 0.4f;

                if (totalScore > bestThreatScore)
                {
                    bestThreatScore = totalScore;
                    bestThreatPosition = proj.Center;
                }
            }
        }

        private void SenseLargeHostileNpcs(NPC self, ref float bestThreatScore, ref Vector2 bestThreatPosition)
        {
            float maxDistSq = NpcThreatRadius * NpcThreatRadius;



            NPC threat = AbyssUtilities.FindClosestAbyssPredator(self, out float dist);
            if(threat is not null)
            bestThreatPosition = threat.Center;
            
        }

        private void SenseDisturbance(TContext context, NPC self)
        {
            float maxDistSq = DisturbanceRadius * DisturbanceRadius;
            float bestDistSq = maxDistSq;
            bool found = false;
            Vector2 bestPos = Vector2.Zero;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player is null || !player.active || player.dead)
                    continue;

                bool disturbing =
                    player.velocity.LengthSquared() > 16f ||
                    player.itemAnimation > 0;

                if (!disturbing)
                    continue;

                float distSq = Vector2.DistanceSquared(self.Center, player.Center);
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                bestPos = player.Center;
                found = true;
            }

            if (found)
            {
                context.HasDisturbance = true;
                context.DisturbancePosition = bestPos;
            }
        }
    }
}
using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using Microsoft.Xna.Framework;
using Terraria;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    public sealed class SharedCreatureAwarenessSensor : INpcSensor<CreatureNpcContext>
    {
        public float PlayerThreatRadius = 360f;
        public float ProjectileThreatRadius = 280f;
        public float NpcThreatRadius = 300f;
        public float DisturbanceRadius = 220f;

        public void Update(CreatureNpcContext context)
        {
            if (context is not CreatureNpcContext creature || creature.Self is null || !creature.Self.active)
                return;

            NPC self = creature.Self;

            // Reset frame-local awareness outputs.
            creature.HasThreat = false;
            creature.ThreatPosition = Vector2.Zero;
            creature.ThreatLevel = 0f;

            creature.HasDisturbance = false;
            creature.DisturbancePosition = Vector2.Zero;

            float bestThreatScore = 0f;
            Vector2 bestThreatPosition = Vector2.Zero;

            SensePlayers(creature, self, ref bestThreatScore, ref bestThreatPosition);
            SenseProjectiles(creature, self, ref bestThreatScore, ref bestThreatPosition);
            SenseLargeHostileNpcs(creature, self, ref bestThreatScore, ref bestThreatPosition);
            SenseDisturbance(creature, self);

            if (bestThreatScore > 0f)
            {
                creature.HasThreat = true;
                creature.ThreatPosition = bestThreatPosition;
                creature.ThreatLevel = MathHelper.Clamp(bestThreatScore, 0f, 1f);
                creature.TimeSinceThreatSeen = 0;
            }

            UpdateCreatureState(creature, self);
        }

        private void SensePlayers(CreatureNpcContext creature, NPC self, ref float bestThreatScore, ref Vector2 bestThreatPosition)
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

        private void SenseProjectiles(CreatureNpcContext creature, NPC self, ref float bestThreatScore, ref Vector2 bestThreatPosition)
        {
            float maxDistSq = ProjectileThreatRadius * ProjectileThreatRadius;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj is null || !proj.active)
                    continue;

                // Creature-level "dangerous moving object" logic.
                // We do not rely only on hostile because many dangerous projectiles in modded contexts are weird.
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

        private void SenseLargeHostileNpcs(CreatureNpcContext creature, NPC self, ref float bestThreatScore, ref Vector2 bestThreatPosition)
        {
            float maxDistSq = NpcThreatRadius * NpcThreatRadius;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc is null || !npc.active || npc.whoAmI == self.whoAmI)
                    continue;

                if (npc.friendly || npc.townNPC)
                    continue;

                float distSq = Vector2.DistanceSquared(self.Center, npc.Center);
                if (distSq > maxDistSq)
                    continue;

                float sizeAdvantage = 0f;
                if (npc.lifeMax > self.lifeMax)
                    sizeAdvantage = MathHelper.Clamp((npc.lifeMax - self.lifeMax) / (float)Math.Max(1, self.lifeMax), 0f, 1f);

                if (sizeAdvantage <= 0.05f)
                    continue;

                float dist = MathF.Sqrt(distSq);
                float proximityScore = 1f - dist / NpcThreatRadius;
                float totalScore = proximityScore * 0.5f + sizeAdvantage * 0.5f;

                if (Collision.CanHit(self.Center, 1, 1, npc.Center, 1, 1))
                    totalScore += 0.1f;

                if (totalScore > bestThreatScore)
                {
                    bestThreatScore = totalScore;
                    bestThreatPosition = npc.Center;
                }
            }
        }

        private void SenseDisturbance(CreatureNpcContext creature, NPC self)
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
                creature.HasDisturbance = true;
                creature.DisturbancePosition = bestPos;
            }
        }

        private void UpdateCreatureState(CreatureNpcContext creature, NPC self)
        {
            // Baseline drift.
            creature.Curiosity = MathHelper.Clamp(creature.Curiosity + 0.0025f, 0f, 1f);
            creature.Fear = MathHelper.Clamp(creature.Fear - 0.005f, 0f, 1f);

            // Threat reaction.
            if (creature.HasThreat)
            {
                creature.Fear = MathHelper.Clamp(creature.Fear + 0.03f + creature.ThreatLevel * 0.04f, 0f, 1f);
                creature.Curiosity = MathHelper.Clamp(creature.Curiosity - 0.02f, 0f, 1f);
            }
            else if (creature.HasDisturbance)
            {
                creature.Fear = MathHelper.Clamp(creature.Fear + 0.01f, 0f, 1f);
                creature.Curiosity = MathHelper.Clamp(creature.Curiosity + 0.01f, 0f, 1f);
            }

            // Simple energy/fatigue loop.
            float speed = self.velocity.Length();
            creature.Fatigue = MathHelper.Clamp(creature.Fatigue + speed * 0.0015f - 0.002f, 0f, 1f);
            creature.Energy = MathHelper.Clamp(creature.Energy - creature.Fatigue * 0.002f + 0.0015f, 0f, 1f);

            if (speed < 0.5f)
                creature.StuckTime++;
            else
                creature.StuckTime = 0;

            creature.WanderTimer++;
        }
    }
}
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Core.Ecosystem.TerritorySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    public sealed class TerritoryAwarenessSensor : INpcSensor<CreatureNpcContext>
    {
        // If the creature has no owned territory, it can still become aware of
        // whichever territory it is currently standing inside.
        public bool UseCurrentTerritoryIfNoOwnedTerritory = true;

        // Usually leave this on so IdleHomeModule can naturally keep the creature near home.
        public bool SetHomeToOwnedTerritoryCenter = true;

        // Optional: only turn this on if you explicitly want territory to drive pathing/target-point behavior.
        // Leave false if other sensors also use HasTargetPoint.
        public bool SetTargetPointWhenOutsideOwnedTerritory = false;

        public float ReturnPadding = 24f;

        public float PlayerThreatMultiplier = 0.60f;
        public float ProjectileThreatMultiplier = 0.85f;
        public float NpcThreatMultiplier = 0.50f;

        public void Update(CreatureNpcContext context)
        {
            NPC self = context?.Self;
            if (self is null || !self.active)
                return;

            Territory ownedTerritory = TerritoryRegistry.FindOwnedBy(self);
            Territory currentTerritory = TerritoryRegistry.FindContaining(self.Center);
            Territory sensedTerritory = ownedTerritory ?? (UseCurrentTerritoryIfNoOwnedTerritory ? currentTerritory : null);

            if (ownedTerritory is not null && SetHomeToOwnedTerritoryCenter)
                context.HomePosition = ownedTerritory.Center;

            if (ownedTerritory is not null &&
                SetTargetPointWhenOutsideOwnedTerritory &&
                !ownedTerritory.Bounds.Contains(self.Center.ToPoint()))
            {
                context.TargetPoint = TerritoryRegistry.ClampInside(ownedTerritory, self.Center, ReturnPadding);
                context.HasTargetPoint = true;
            }

            if (sensedTerritory is null)
                return;

            float bestThreatScore = context.HasThreat ? context.ThreatLevel : 0f;
            Vector2 bestThreatPosition = context.HasThreat ? context.ThreatPosition : Vector2.Zero;

            float bestDisturbanceDistSq =
                context.HasDisturbance ? Vector2.DistanceSquared(self.Center, context.DisturbancePosition) : float.MaxValue;
            Vector2 bestDisturbancePosition =
                context.HasDisturbance ? context.DisturbancePosition : Vector2.Zero;

            SensePlayers(
                self,
                sensedTerritory,
                ref bestThreatScore,
                ref bestThreatPosition,
                ref bestDisturbanceDistSq,
                ref bestDisturbancePosition);

            SenseProjectiles(
                self,
                sensedTerritory,
                ref bestThreatScore,
                ref bestThreatPosition,
                ref bestDisturbanceDistSq,
                ref bestDisturbancePosition);

            SenseNpcs(
                self,
                sensedTerritory,
                ref bestThreatScore,
                ref bestThreatPosition,
                ref bestDisturbanceDistSq,
                ref bestDisturbancePosition);

            if (bestThreatScore > 0f)
            {
                context.HasThreat = true;
                context.ThreatPosition = bestThreatPosition;
                context.ThreatLevel = MathHelper.Clamp(bestThreatScore, 0f, 1f);
                context.TimeSinceThreatSeen = 0;
            }

            if (bestDisturbanceDistSq < float.MaxValue)
            {
                context.HasDisturbance = true;
                context.DisturbancePosition = bestDisturbancePosition;
            }
        }

        private void SensePlayers(
            NPC self,
            Territory territory,
            ref float bestThreatScore,
            ref Vector2 bestThreatPosition,
            ref float bestDisturbanceDistSq,
            ref Vector2 bestDisturbancePosition)
        {
            float territoryRange = GetTerritoryRange(territory);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player is null || !player.active || player.dead)
                    continue;

                if (ReferenceEquals(territory.Owner, player))
                    continue;

                if (!territory.Bounds.Contains(player.Center.ToPoint()))
                    continue;

                ConsiderDisturbance(self, player.Center, ref bestDisturbanceDistSq, ref bestDisturbancePosition);

                float dist = Vector2.Distance(self.Center, player.Center);
                float proximityScore = 1f - MathHelper.Clamp(dist / territoryRange, 0f, 1f);
                float motionScore = MathHelper.Clamp(player.velocity.Length() / 10f, 0f, 1f);
                float actionScore = player.itemAnimation > 0 ? 0.35f : 0f;

                float totalScore = (proximityScore * 0.45f + motionScore * 0.20f + actionScore * 0.35f) * PlayerThreatMultiplier;

                if (Collision.CanHit(self.Center, 1, 1, player.Center, 1, 1))
                    totalScore += 0.1f;

                if (totalScore > bestThreatScore)
                {
                    bestThreatScore = totalScore;
                    bestThreatPosition = player.Center;
                }
            }
        }

        private void SenseProjectiles(
            NPC self,
            Territory territory,
            ref float bestThreatScore,
            ref Vector2 bestThreatPosition,
            ref float bestDisturbanceDistSq,
            ref Vector2 bestDisturbancePosition)
        {
            float territoryRange = GetTerritoryRange(territory);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj is null || !proj.active)
                    continue;

                if (!territory.Bounds.Contains(proj.Center.ToPoint()))
                    continue;

                bool dangerous =
                    proj.hostile ||
                    proj.damage > 0 ||
                    proj.velocity.LengthSquared() > 9f;

                if (!dangerous)
                    continue;

                ConsiderDisturbance(self, proj.Center, ref bestDisturbanceDistSq, ref bestDisturbancePosition);

                float dist = Vector2.Distance(self.Center, proj.Center);
                float proximityScore = 1f - MathHelper.Clamp(dist / territoryRange, 0f, 1f);
                float speedScore = MathHelper.Clamp(proj.velocity.Length() / 14f, 0f, 1f);

                Vector2 toSelf = self.Center - proj.Center;
                float approachScore = 0f;
                if (proj.velocity != Vector2.Zero && toSelf != Vector2.Zero)
                {
                    Vector2 projDir = Vector2.Normalize(proj.velocity);
                    Vector2 toSelfDir = Vector2.Normalize(toSelf);
                    approachScore = MathHelper.Clamp(Vector2.Dot(projDir, toSelfDir), 0f, 1f);
                }

                float totalScore = (proximityScore * 0.40f + speedScore * 0.20f + approachScore * 0.40f) * ProjectileThreatMultiplier;

                if (totalScore > bestThreatScore)
                {
                    bestThreatScore = totalScore;
                    bestThreatPosition = proj.Center;
                }
            }
        }

        private void SenseNpcs(
            NPC self,
            Territory territory,
            ref float bestThreatScore,
            ref Vector2 bestThreatPosition,
            ref float bestDisturbanceDistSq,
            ref Vector2 bestDisturbancePosition)
        {
            float territoryRange = GetTerritoryRange(territory);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other is null || !other.active || other.whoAmI == self.whoAmI)
                    continue;

                if (ReferenceEquals(territory.Owner, other))
                    continue;

                if (!territory.Bounds.Contains(other.Center.ToPoint()))
                    continue;

                ConsiderDisturbance(self, other.Center, ref bestDisturbanceDistSq, ref bestDisturbancePosition);

                if (other.friendly || other.townNPC)
                    continue;

                float dist = Vector2.Distance(self.Center, other.Center);
                float proximityScore = 1f - MathHelper.Clamp(dist / territoryRange, 0f, 1f);

                float sizeAdvantage = 0f;
                if (other.lifeMax > self.lifeMax)
                {
                    sizeAdvantage = MathHelper.Clamp(
                        (other.lifeMax - self.lifeMax) / (float)System.Math.Max(1, self.lifeMax),
                        0f,
                        1f);
                }

                float totalScore = (proximityScore * 0.55f + sizeAdvantage * 0.45f) * NpcThreatMultiplier;

                if (Collision.CanHit(self.Center, 1, 1, other.Center, 1, 1))
                    totalScore += 0.1f;

                if (totalScore > bestThreatScore)
                {
                    bestThreatScore = totalScore;
                    bestThreatPosition = other.Center;
                }
            }
        }

        private static void ConsiderDisturbance(
            NPC self,
            Vector2 position,
            ref float bestDisturbanceDistSq,
            ref Vector2 bestDisturbancePosition)
        {
            float distSq = Vector2.DistanceSquared(self.Center, position);
            if (distSq < bestDisturbanceDistSq)
            {
                bestDisturbanceDistSq = distSq;
                bestDisturbancePosition = position;
            }
        }

        private static float GetTerritoryRange(Territory territory)
        {
            Vector2 size = territory.Bounds.Size();
            return System.Math.Max(96f, size.Length());
        }
    }
}

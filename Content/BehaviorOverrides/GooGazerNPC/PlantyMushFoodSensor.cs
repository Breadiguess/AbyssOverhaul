using CalamityMod.Tiles.Abyss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.BehaviorOverrides.GooGazerNPC
{
    public sealed class PlantyMushFoodSensor : INpcSensor<GooGazerContext>
    {
        public float SearchRadius = 320f;
        public float FeedOffset = 24f;

        public void Update(GooGazerContext context)
        {
            context.HasFoodTile = false;

            // Do not even look for food unless this fish is low on energy.
            // gluttonous bastard.
            if (!context.WantsFood)
            {
                if (context.HasTargetPoint && context.FoodTile != Point.Zero)
                    context.HasTargetPoint = false;

                return;
            }

            NPC self = context.Self;
            if (self is null || !self.active)
                return;

            Point centerTile = self.Center.ToTileCoordinates();
            int tileRadius = (int)(SearchRadius / 16f);

            Point? bestFoodTile = null;
            Vector2 bestFeedPoint = Vector2.Zero;
            float bestDistSq = float.MaxValue;

            int minX = Math.Max(1, centerTile.X - tileRadius);
            int maxX = Math.Min(Main.maxTilesX - 2, centerTile.X + tileRadius);
            int minY = Math.Max(1, centerTile.Y - tileRadius);
            int maxY = Math.Min(Main.maxTilesY - 2, centerTile.Y + tileRadius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile || tile.TileType != ModContent.TileType<PlantyMush>())
                        continue;

                    if (!TryGetFeedPoint(self, x, y, out Vector2 feedPoint))
                        continue;

                    float distSq = Vector2.DistanceSquared(self.Center, feedPoint);
                    if (distSq >= bestDistSq)
                        continue;

                    bestDistSq = distSq;
                    bestFoodTile = new Point(x, y);
                    bestFeedPoint = feedPoint;
                }
            }

            if (bestFoodTile is null)
            {
                context.HasTargetPoint = false;
                return;
            }

            context.HasFoodTile = true;
            context.FoodTile = bestFoodTile.Value;
            context.TargetPoint = bestFeedPoint;
            context.HasTargetPoint = true;
        }

        private bool TryGetFeedPoint(NPC self, int tileX, int tileY, out Vector2 feedPoint)
        {
            Vector2 tileCenter = new Point(tileX, tileY).ToWorldCoordinates(8f, 8f);

            Vector2[] candidates =
            {
                tileCenter + new Vector2( FeedOffset, 0f),
                tileCenter + new Vector2(-FeedOffset, 0f),
                tileCenter + new Vector2(0f, -FeedOffset),
                tileCenter + new Vector2(0f,  FeedOffset),
            };

            float bestDistSq = float.MaxValue;
            feedPoint = Vector2.Zero;
            bool found = false;

            foreach (Vector2 candidate in candidates)
            {
                Point p = candidate.ToTileCoordinates();
                if (!IsOpenWaterTile(p.X, p.Y))
                    continue;

                float distSq = Vector2.DistanceSquared(self.Center, candidate);
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                feedPoint = candidate;
                found = true;
            }

            return found;
        }

        private bool IsOpenWaterTile(int x, int y)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            return !tile.HasTile && tile.LiquidAmount > 0;
        }
    }

}

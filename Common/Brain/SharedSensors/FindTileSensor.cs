using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    public sealed class FindTileSensor : INpcSensor<NpcContext>
    {
        public float SearchRadius = 320f;
        public Func<Tile, bool> TileCondition;
        public FindTileSensor(Func<Tile, bool> tileCondition)
        {
            TileCondition = tileCondition;
        }

        public void Update(NpcContext context)
        {
            context.HasTargetPoint = false;

            Point? bestTile = FindNearestMatchingTile(context.Self.Center, SearchRadius, TileCondition);
            if (bestTile is null)
                return;

            context.TargetPoint = bestTile.Value.ToWorldCoordinates(8f, 8f);
            context.HasTargetPoint = true;
        }

        private static Point? FindNearestMatchingTile(Vector2 worldCenter, float radius, Func<Tile, bool> condition)
        {
            Point centerTile = worldCenter.ToTileCoordinates();
            int tileRadius = (int)(radius / 16f);

            Point? best = null;
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
                    if (!condition(tile))
                        continue;

                    Vector2 tileWorld = new Vector2(x * 16f + 8f, y * 16f + 8f);
                    float distSq = Vector2.DistanceSquared(worldCenter, tileWorld);

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = new Point(x, y);
                    }
                }
            }

            return best;
        }
    }
}

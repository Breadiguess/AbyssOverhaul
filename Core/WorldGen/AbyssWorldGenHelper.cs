using AbyssOverhaul.Core.Utilities;
using Terraria.ID;

namespace AbyssOverhaul.Core.WorldGen
{
    public static class AbyssWorldGenHelper
{
    public static AbyssRegion CreateRegion(float startT, float endT)
    {
        return new AbyssRegion(
            AbyssGenUtils.MinX,
            AbyssGenUtils.MaxX,
            AbyssGenUtils.YAt(startT),
            AbyssGenUtils.YAt(endT)
        );
    }

    public static void PlaceSolidTile(int x, int y, int type, bool fillWithWater = true, ushort wallType = WallID.None)
    {
        if (!Terraria.WorldGen.InWorld(x, y, 20))
            return;

        Tile tile = Main.tile[x, y];
        tile.HasTile = true;
        tile.TileType = (ushort)type;
        tile.Slope = 0;
        tile.IsHalfBlock = false;

        if (fillWithWater)
        {
            tile.LiquidType = LiquidID.Water;
            tile.LiquidAmount = 255;
        }

        if (wallType != WallID.None)
            tile.WallType = wallType;
    }

    public static void ForceSolidRect(int minX, int maxX, int minY, int maxY, int tileType, bool fillWithWater = true, ushort wallType = WallID.None)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
                PlaceSolidTile(x, y, tileType, fillWithWater, wallType);
        }
    }

    public static void ClearTile(int x, int y, bool fillWithWater = false)
    {
        if (!Terraria.WorldGen.InWorld(x, y, 20))
            return;

        Tile tile = Main.tile[x, y];
        tile.HasTile = false;

        if (fillWithWater)
        {
            tile.LiquidType = LiquidID.Water;
            tile.LiquidAmount = 255;
        }
        else
        {
            tile.LiquidAmount = 0;
        }
    }

    public static void FloodOpenSpace(int minX, int maxX, int startY, int endY)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (!Terraria.WorldGen.InWorld(x, y, 20))
                    continue;

                Tile tile = Main.tile[x, y];
                if (tile.HasTile)
                    continue;

                tile.LiquidType = LiquidID.Water;
                tile.LiquidAmount = 255;
            }
        }
    }

    public static void FloodOpenSpace(AbyssRegion region)
    {
        FloodOpenSpace(region.MinX, region.MaxX, region.StartY, region.EndY);
    }

    public static void FillBlob(int centerX, int centerY, int radiusX, int radiusY, int tileType, float irregularity, bool fillWithWater = true)
    {
        ForEachBlobTile(centerX, centerY, radiusX, radiusY, irregularity, (x, y) =>
        {
            PlaceSolidTile(x, y, tileType, fillWithWater);
        });
    }

    public static void CarveBlob(int centerX, int centerY, int radiusX, int radiusY, float irregularity, bool fillWithWater = false)
    {
        ForEachBlobTile(centerX, centerY, radiusX, radiusY, irregularity, (x, y) =>
        {
            ClearTile(x, y, fillWithWater);
        });
    }

    public static void FillBlobReplace(int centerX, int centerY, int radiusX, int radiusY, int tileType, float irregularity, params int[] replaceable)
    {
        ForEachBlobTile(centerX, centerY, radiusX, radiusY, irregularity, (x, y) =>
        {
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile)
                return;

            for (int i = 0; i < replaceable.Length; i++)
            {
                if (tile.TileType == replaceable[i])
                {
                    tile.TileType = (ushort)tileType;
                    return;
                }
            }
        });
    }

        public static void CarveTunnelBlobLineSmooth(Vector2 start, Vector2 end, int radiusX, int radiusY, float irregularity, bool fillWithWater = true,
         float sampleSpacingFactor = 0.33f, float wanderStrength = 0.22f, float radiusJitterFactor = 0.12f)
        {
            Vector2 delta = end - start;
            float distance = delta.Length();

            if (distance <= 0.001f)
            {
                CarveBlob((int)start.X, (int)start.Y, radiusX, radiusY, irregularity, fillWithWater);
                return;
            }

            Vector2 direction = delta / distance;
            Vector2 perpendicular = new(-direction.Y, direction.X);
            
            float baseRadius = Math.Max(4f, Math.Min(radiusX, radiusY));
            float spacing = Math.Max(1.5f, baseRadius * sampleSpacingFactor);
            int samples = Math.Max(2, (int)(distance / spacing));
            float currentOffset = 0f;
            float targetOffset = 0f;

            float currentRx = radiusX;
            float targetRx = radiusX;

            float currentRy = radiusY;
            float targetRy = radiusY;

            // How often the tunnel "changes its mind".
            int controlStep = Math.Max(3, samples / 8);

            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;

                if (i % controlStep == 0 || i == 0)
                {
                    targetOffset = Terraria.WorldGen.genRand.NextFloat(
                        -radiusY * wanderStrength,
                         radiusY * wanderStrength
                    );

                    targetRx = radiusX + Terraria.WorldGen.genRand.NextFloat(
                        -radiusX * radiusJitterFactor,
                         radiusX * radiusJitterFactor
                    );

                    targetRy = radiusY + Terraria.WorldGen.genRand.NextFloat(
                        -radiusY * radiusJitterFactor,
                         radiusY * radiusJitterFactor
                    );
                }

                currentOffset = MathHelper.Lerp(currentOffset, targetOffset, 0.22f);
                currentRx = MathHelper.Lerp(currentRx, targetRx, 0.20f);
                currentRy = MathHelper.Lerp(currentRy, targetRy, 0.20f);

                Vector2 center = Vector2.Lerp(start, end, t) + perpendicular * currentOffset;

                int rx = Math.Max(3, (int)MathF.Round(currentRx));
                int ry = Math.Max(3, (int)MathF.Round(currentRy));

                // Main carve
                CarveBlob((int)center.X, (int)center.Y, rx, ry, irregularity, fillWithWater);

                if (rx >= 5 && ry >= 5)
                {
                    Vector2 side = perpendicular * MathF.Min(ry * 0.18f, 3f);
                    CarveBlob((int)(center.X + side.X), (int)(center.Y + side.Y), Math.Max(3, rx - 1), Math.Max(3, ry - 1), irregularity * 0.85f, fillWithWater);
                    CarveBlob((int)(center.X - side.X), (int)(center.Y - side.Y), Math.Max(3, rx - 1), Math.Max(3, ry - 1), irregularity * 0.85f, fillWithWater);
                }
            }

            CarveBlob((int)start.X, (int)start.Y, radiusX, radiusY, irregularity, fillWithWater);
            CarveBlob((int)end.X, (int)end.Y, radiusX, radiusY, irregularity, fillWithWater);
        }

        public static void ReframeArea(int minX, int maxX, int startY, int endY)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (!Terraria.WorldGen.InWorld(x, y, 20))
                    continue;

                Terraria.WorldGen.SquareTileFrame(x, y, true);
            }
        }
    }

    public static void RemoveLonelyTiles(int minX, int maxX, int startY, int endY, int maxNeighbors = 2, int chanceDenominator = 3, bool fillWithWater = false)
    {
        for (int x = minX + 1; x < maxX - 1; x++)
        {
            for (int y = startY + 1; y < endY - 1; y++)
            {
                if (!Terraria.WorldGen.InWorld(x, y, 20))
                    continue;

                Tile tile = Main.tile[x, y];
                if (!tile.HasTile)
                    continue;

                int solidNeighbors = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        if (Main.tile[x + dx, y + dy].HasTile)
                            solidNeighbors++;
                    }
                }

                if (solidNeighbors <= maxNeighbors && Terraria.WorldGen.genRand.NextBool(chanceDenominator))
                    ClearTile(x, y, fillWithWater);
            }
        }
    }

    public static float FractalNoise(float x, float y, int octaves, float frequency = 1f, float amplitude = 1f)
    {
        float total = 0f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += ValueNoise(x * frequency, y * frequency) * amplitude;
            norm += amplitude;

            frequency *= 2f;
            amplitude *= 0.5f;
        }

        return norm <= 0f ? 0f : total / norm;
    }

    public static float ValueNoise(float x, float y)
    {
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);

        float fx = x - ix;
        float fy = y - iy;

        float v00 = HashTo01(ix, iy);
        float v10 = HashTo01(ix + 1, iy);
        float v01 = HashTo01(ix, iy + 1);
        float v11 = HashTo01(ix + 1, iy + 1);

        float sx = Smooth(fx);
        float sy = Smooth(fy);

        float i1 = MathHelper.Lerp(v00, v10, sx);
        float i2 = MathHelper.Lerp(v01, v11, sx);
        return MathHelper.Lerp(i1, i2, sy);
    }

    public static float HashTo01(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    public static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static void ForEachBlobTile(int centerX, int centerY, int radiusX, int radiusY, float irregularity, Action<int, int> action)
    {
        for (int x = centerX - radiusX - 2; x <= centerX + radiusX + 2; x++)
        {
            for (int y = centerY - radiusY - 2; y <= centerY + radiusY + 2; y++)
            {
                if (!Terraria.WorldGen.InWorld(x, y, 20))
                    continue;

                float dx = (x - centerX) / (float)Math.Max(1, radiusX);
                float dy = (y - centerY) / (float)Math.Max(1, radiusY);
                float dist = dx * dx + dy * dy;

                float noise = FractalNoise(x * 0.09f, y * 0.09f, 3, 1f, 1f);
                float threshold = 1f + (noise - 0.5f) * 2f * irregularity;

                if (dist <= threshold)
                    action(x, y);
            }
        }
    }
}

}
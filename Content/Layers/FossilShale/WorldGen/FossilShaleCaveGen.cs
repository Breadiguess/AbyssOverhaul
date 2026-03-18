using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.Utilities;

namespace AbyssOverhaul.Content.Layers.FossilShale.WorldGen
{
    public static class FossilShaleCaveGen
    {
        private struct Tunnel
        {
            public List<Point> Points;
            public int[] Radius;

            public Tunnel(List<Point> points, int radius)
            {
                Points = points;
                Radius = new int[points.Count];
                for (int i = 0; i < points.Count; i++)
                    Radius[i] = radius;
            }

            public Tunnel(List<Point> points, int[] radius)
            {
                Points = points;
                Radius = new int[points.Count];
                for (int i = 0; i < points.Count; i++)
                    Radius[i] = radius[i];
            }
        }

        private struct Chamber2
        {
            public Point Center;

            /// <summary>
            /// 0..1 within this sublayer.
            /// </summary>
            public float LayerPosition;
            public int Rx;
            public int Ry;
            public int RadiusApprox => (Rx + Ry) / 2;

            public Chamber2(Point c, int rx, int ry)
            {
                Center = c;
                LayerPosition = 0f;
                Rx = rx;
                Ry = ry;
            }
        }

        /// <summary>
        /// Generates a chamber-first cave network with a guaranteed top-to-bottom continuous path.
        /// solidTileType = the tile that fills the solid parts of the mask.
        /// openWallType = the wall placed in carved/open areas.
        /// </summary>
        public static void GenerateFossilShaleCaves(
            GenerationProgress progress,
            GameConfiguration config,
            int minX,
            int maxX,
            int topY,
            int bottomY,
            ushort solidTileType,
            ushort openWallType = 0,
            int mainChamberCount = 10,
            int extraChamberCount = 5)
        {
            progress.Message = "Fossil Shale: generating cave network";

            UnifiedRandom rand = Terraria.WorldGen.genRand;

            int width = maxX - minX;
            int height = bottomY - topY;
            if (width <= 0 || height <= 0)
                return;

            List<Chamber2> chambers = GenerateChambers(minX, maxX, topY, bottomY, mainChamberCount, extraChamberCount, rand);
            List<int> spine = BuildGuaranteedSpine(chambers, mainChamberCount);
            List<Tunnel> tunnels = BuildTunnels(chambers, spine, topY, bottomY, rand);

            bool[,] solidMask = BuildSolidMask(chambers, tunnels, minX, maxX, topY, bottomY);

            for (int i = 0; i < 2; i++)
                RunBoundaryCleanup(solidMask);
            
            ApplyMaskToWorld(solidMask, minX, maxX, topY, bottomY, solidTileType, openWallType, (ushort)ModContent.TileType<CyanobacteriaSludge_Tile>(), 0.7f, 2f, 4);
        }

        private static List<Chamber2> GenerateChambers(
            int minX,
            int maxX,
            int topY,
            int bottomY,
            int mainChamberCount,
            int extraChamberCount,
            UnifiedRandom rand)
        {
            List<Chamber2> chambers = new();

            int width = maxX - minX;
            int height = bottomY - topY;
            int centerX = (minX + maxX) / 2;

            // Main chambers: vertically ordered, guaranteed full-span coverage.
            for (int i = 0; i < mainChamberCount; i++)
            {
                float t = mainChamberCount <= 1 ? 0.5f : i / (float)(mainChamberCount - 1);
                float layerPos = MathHelper.Lerp(0.04f, 0.96f, t);

                int y = topY + (int)(layerPos * height);

                // Keep the main line relatively centered, but with drift.
                int drift = (int)(width * 0.22f);
                int x = centerX + rand.Next(-drift, drift + 1);
                x = Utils.Clamp(x, minX + 30, maxX - 30);

                if (i == 0)
                    x = centerX;

                int rx = rand.Next(18, 34);
                int ry = rand.Next(14, 28);

                Chamber2 c = new(new Point(x, y), rx, ry)
                {
                    LayerPosition = layerPos
                };
                chambers.Add(c);
            }

            // Extra chambers: side blobs / branches.
            for (int i = 0; i < extraChamberCount; i++)
            {
                float layerPos = rand.NextFloat(0.07f, 0.93f);
                int y = topY + (int)(layerPos * height);

                int x = rand.Next(minX + 28, maxX - 28);
                int rx = rand.Next(12, 26);
                int ry = rand.Next(10, 22);

                Chamber2 c = new(new Point(x, y), rx, ry)
                {
                    LayerPosition = layerPos
                };
                chambers.Add(c);
            }

            return chambers;
        }

        private static List<int> BuildGuaranteedSpine(List<Chamber2> chambers, int mainChamberCount)
        {
            // The first mainChamberCount chambers are intentionally the top->bottom guaranteed chain.
            List<int> spine = new();
            for (int i = 0; i < mainChamberCount && i < chambers.Count; i++)
                spine.Add(i);

            spine.Sort((a, b) => chambers[a].LayerPosition.CompareTo(chambers[b].LayerPosition));
            return spine;
        }

        private static List<Tunnel> BuildTunnels(
            List<Chamber2> chambers,
            List<int> spine,
            int topY,
            int bottomY,
            UnifiedRandom rand)
        {
            List<Tunnel> tunnels = new();
            HashSet<int> spineSet = new(spine);

            // Guarantee literal top entrance -> spine -> bottom exit continuity.
            Chamber2 first = chambers[spine[0]];
            Chamber2 last = chambers[spine[spine.Count - 1]];

            tunnels.Add(new Tunnel(
                new List<Point>
                {
                new(first.Center.X, topY + 3),
                first.Center
                },
                Math.Max(6, first.RadiusApprox / 2)));

            // Connect the main spine in order.
            for (int i = 0; i < spine.Count - 1; i++)
            {
                Tunnel t = CreateOrganicTunnel(chambers[spine[i]], chambers[spine[i + 1]], rand);
                tunnels.Add(t);
            }

            tunnels.Add(new Tunnel(
                new List<Point>
                {
                last.Center,
                new(last.Center.X, bottomY - 3)
                },
                Math.Max(6, last.RadiusApprox / 2)));

            // Connect extras to nearby chambers to create branches/loops.
            for (int i = 0; i < chambers.Count; i++)
            {
                if (spineSet.Contains(i))
                    continue;

                int best = -1;
                float bestScore = float.MaxValue;

                for (int j = 0; j < chambers.Count; j++)
                {
                    if (i == j)
                        continue;

                    Vector2 delta = chambers[j].Center.ToVector2() - chambers[i].Center.ToVector2();

                    // Prefer mostly-nearby and not absurdly horizontal-only.
                    float score = MathF.Abs(delta.X) * 1.0f + MathF.Abs(delta.Y) * 0.8f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = j;
                    }
                }

                if (best != -1)
                {
                    // Strong chance to connect, to keep the layout cohesive.
                    if (rand.NextBool(2))
                        tunnels.Add(CreateOrganicTunnel(chambers[i], chambers[best], rand));
                }
            }

            // Small number of bonus loops between close chambers for that chunky maze feel.
            for (int i = 0; i < chambers.Count; i++)
            {
                for (int j = i + 1; j < chambers.Count; j++)
                {
                    Vector2 d = chambers[j].Center.ToVector2() - chambers[i].Center.ToVector2();
                    float dist = d.Length();
                    if (dist > 140f)
                        continue;

                    if (MathF.Abs(d.Y) > 90f)
                        continue;

                    if (rand.NextBool(8))
                        tunnels.Add(CreateOrganicTunnel(chambers[i], chambers[j], rand));
                }
            }

            return tunnels;
        }

        private static Tunnel CreateOrganicTunnel(Chamber2 a, Chamber2 b, UnifiedRandom rand)
        {
            Vector2 start = a.Center.ToVector2();
            Vector2 end = b.Center.ToVector2();
            Vector2 delta = end - start;

            float length = delta.Length();
            Vector2 dir = length <= 0.001f ? Vector2.UnitY : delta / length;
            Vector2 normal = new(-dir.Y, dir.X);

            int controlPoints = rand.Next(2, 5);

            List<Point> points = new();
            points.Add(start.ToPoint());

            float tunnelSeed = rand.NextFloat(0f, 10000f);

            for (int i = 1; i <= controlPoints; i++)
            {
                float t = i / (float)(controlPoints + 1);

                Vector2 pos = Vector2.Lerp(start, end, t);

                float bigWobble = MathF.Sin(t * MathHelper.TwoPi * rand.NextFloat(0.8f, 1.6f) + tunnelSeed) * rand.NextFloat(10f, 26f);
                float smallWobble = MathF.Sin(t * MathHelper.TwoPi * rand.NextFloat(2.5f, 4.5f) + tunnelSeed * 0.37f) * rand.NextFloat(3f, 9f);
                float lateralJitter = bigWobble + smallWobble + rand.NextFloat(-10f, 10f);

                float alongJitter = rand.NextFloat(-8f, 8f);

                pos += normal * lateralJitter;
                pos += dir * alongJitter;

                points.Add(pos.ToPoint());
            }

            points.Add(end.ToPoint());

            int[] radii = new int[points.Count];
            float radiusSeed = rand.NextFloat(0f, 10000f);

            for (int i = 0; i < points.Count; i++)
            {
                float t = points.Count <= 1 ? 0.5f : i / (float)(points.Count - 1);
                float baseRadius = MathHelper.Lerp(a.RadiusApprox * 0.42f, b.RadiusApprox * 0.42f, t);

                float bulge1 = MathF.Sin(t * MathHelper.TwoPi * 1.2f + radiusSeed) * 2.5f;
                float bulge2 = MathF.Sin(t * MathHelper.TwoPi * 3.1f + radiusSeed * 0.61f) * 1.5f;

                baseRadius += bulge1 + bulge2 + rand.NextFloat(-2f, 2f);
                radii[i] = Math.Max(5, (int)baseRadius);
            }

            return new Tunnel(points, radii);
        }

        private static bool[,] BuildSolidMask(
            List<Chamber2> chambers,
            List<Tunnel> tunnels,
            int minX,
            int maxX,
            int topY,
            int bottomY)
        {
            int width = maxX - minX;
            int height = bottomY - topY;

            bool[,] solid = new bool[width, height];

            // Start as all solid.
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    solid[x, y] = true;

            // Carve chambers first.
            foreach (Chamber2 chamber in chambers)
            {
                int lx = chamber.Center.X - minX;
                int ly = chamber.Center.Y - topY;
                CarveEllipse(solid, lx, ly, chamber.Rx, chamber.Ry);
            }

            // Then carve tunnel network.
            foreach (Tunnel tunnel in tunnels)
                CarveTunnel(solid, tunnel, minX, topY);

            return solid;
        }

        private static void CarveEllipse(bool[,] solid, int centerX, int centerY, int rx, int ry)
        {
            int width = solid.GetLength(0);
            int height = solid.GetLength(1);

            float seedA = centerX * 0.173f + centerY * 0.117f;
            float seedB = centerX * 0.091f - centerY * 0.213f;

            int pad = 8;
            for (int dx = -rx - pad; dx <= rx + pad; dx++)
            {
                for (int dy = -ry - pad; dy <= ry + pad; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x < 0 || y < 0 || x >= width || y >= height)
                        continue;

                    float nx = dx / (float)rx;
                    float ny = dy / (float)ry;

                    float angle = MathF.Atan2(ny, nx);
                    float dist = MathF.Sqrt(nx * nx + ny * ny);

                    float angularNoise =
                        MathF.Sin(angle * 3f + seedA) * 0.10f +
                        MathF.Sin(angle * 5f - seedB) * 0.07f +
                        MathF.Sin(angle * 9f + seedA * 0.7f) * 0.04f;

                    float positionalNoise =
                        (ValueNoise2D((x + seedA * 20f) * 0.08f, (y + seedB * 20f) * 0.08f) - 0.5f) * 0.22f +
                        (ValueNoise2D((x - seedB * 11f) * 0.16f, (y + seedA * 9f) * 0.16f) - 0.5f) * 0.10f;

                    float threshold = 1f + angularNoise + positionalNoise;

                    if (dist <= threshold)
                        solid[x, y] = false;
                }
            }
        }
        private static void CarveTunnel(bool[,] solid, Tunnel tunnel, int originX, int originY)
        {
            float seed = tunnel.Points[0].X * 0.137f + tunnel.Points[0].Y * 0.193f;

            for (int i = 0; i < tunnel.Points.Count - 1; i++)
            {
                Vector2 a = tunnel.Points[i].ToVector2();
                Vector2 b = tunnel.Points[i + 1].ToVector2();

                int ra = tunnel.Radius[i];
                int rb = tunnel.Radius[i + 1];

                float dist = Vector2.Distance(a, b);
                int steps = Math.Max(3, (int)(dist / 2.25f));

                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    Vector2 p = Vector2.Lerp(a, b, t);

                    float rBase = MathHelper.Lerp(ra, rb, t);

                    float lengthT = (i + t) / Math.Max(1f, tunnel.Points.Count - 1f);
                    float widthNoise =
                        MathF.Sin(lengthT * MathHelper.TwoPi * 2.1f + seed) * 1.5f +
                        MathF.Sin(lengthT * MathHelper.TwoPi * 5.3f + seed * 0.43f) * 0.9f;

                    int r = Math.Max(4, (int)(rBase + widthNoise));

                    int lx = (int)p.X - originX;
                    int ly = (int)p.Y - originY;

                    CarveIrregularCircle(solid, lx, ly, r, seed + i * 13.17f + s * 0.11f);
                }
            }
        }
        private static void CarveIrregularCircle(bool[,] solid, int centerX, int centerY, int radius, float seed)
        {
            int width = solid.GetLength(0);
            int height = solid.GetLength(1);

            int pad = 5;
            for (int dx = -radius - pad; dx <= radius + pad; dx++)
            {
                for (int dy = -radius - pad; dy <= radius + pad; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x < 0 || y < 0 || x >= width || y >= height)
                        continue;

                    float nx = dx / (float)Math.Max(1, radius);
                    float ny = dy / (float)Math.Max(1, radius);

                    float angle = MathF.Atan2(ny, nx);
                    float dist = MathF.Sqrt(nx * nx + ny * ny);

                    float angularNoise =
                        MathF.Sin(angle * 3f + seed) * 0.09f +
                        MathF.Sin(angle * 6f - seed * 0.7f) * 0.05f;

                    float positionalNoise =
                        (ValueNoise2D((x + seed * 17f) * 0.14f, (y - seed * 11f) * 0.14f) - 0.5f) * 0.16f;

                    float threshold = 1f + angularNoise + positionalNoise;

                    if (dist <= threshold)
                        solid[x, y] = false;
                }
            }
        }
        private static void CarveCircle(bool[,] solid, int centerX, int centerY, int radius)
        {
            int width = solid.GetLength(0);
            int height = solid.GetLength(1);
            int r2 = radius * radius;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy > r2)
                        continue;

                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x < 0 || y < 0 || x >= width || y >= height)
                        continue;

                    solid[x, y] = false;
                }
            }
        }

        private static void RunBoundaryCleanup(bool[,] solid)
        {
            int width = solid.GetLength(0);
            int height = solid.GetLength(1);
            bool[,] copy = (bool[,])solid.Clone();

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    int solidNeighbors = 0;

                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            if (ox == 0 && oy == 0)
                                continue;

                            if (copy[x + ox, y + oy])
                                solidNeighbors++;
                        }
                    }

                    // Simple chunky cleanup:
                    // - fill tiny isolated holes
                    // - shave tiny isolated solid nubs
                    if (!copy[x, y] && solidNeighbors >= 7)
                        solid[x, y] = true;
                    else if (copy[x, y] && solidNeighbors <= 2)
                        solid[x, y] = false;
                }
            }
        }

        private static void ApplyMaskToWorld(
     bool[,] solidMask,
     int minX,
     int maxX,
     int topY,
     int bottomY,
     ushort solidTileType,
     ushort openWallType,
     ushort sludgeTileType,
     float sludgeCoverage = 0.22f,
     float sludgeNoiseScale = 0.045f,
     int sludgeSmoothPasses = 3)
        {
            int width = solidMask.GetLength(0);
            int height = solidMask.GetLength(1);

            // First: apply the cave mask normally.
            for (int lx = 0; lx < width; lx++)
            {
                int worldX = minX + lx;
                if (worldX <= 5 || worldX >= Main.maxTilesX - 5)
                    continue;

                for (int ly = 0; ly < height; ly++)
                {
                    int worldY = topY + ly;
                    if (worldY <= 2 || worldY >= Main.maxTilesY - 5)
                        continue;

                    Tile tile = Main.tile[worldX, worldY];
                    bool isSolid = solidMask[lx, ly];

                    tile.WallType = openWallType;
                    if (isSolid)
                    {
                        tile.HasTile = true;
                        tile.TileType = solidTileType;
                        tile.Slope = 0;
                        tile.IsHalfBlock = false;
                    }
                    else
                    {
                        tile.HasTile = false;
                        tile.LiquidType = LiquidID.Water;
                        tile.LiquidAmount = 255;
                        if (openWallType != 0)
                            tile.WallType = openWallType;
                    }
                }
            }
            
            bool[,] sludgeMask = new bool[width, height];

            // Random offsets so every worldgen is different.
            float noiseOffsetX = Terraria.WorldGen.genRand.NextFloat(0f, 10000f);
            float noiseOffsetY = Terraria.WorldGen.genRand.NextFloat(0f, 10000f);

            // Initial thresholded noise.
            for (int lx = 0; lx < width; lx++)
            {
                for (int ly = 0; ly < height; ly++)
                {
                    if (!solidMask[lx, ly])
                        continue;

                    int worldX = minX + lx;
                    int worldY = topY + ly;

                    // Low-frequency blended noise for broad regions.
                    float n1 = FractalNoise(
                        (worldX + noiseOffsetX) * sludgeNoiseScale,
                        (worldY + noiseOffsetY) * sludgeNoiseScale,
                        3,
                        0.5f,
                        2f);

                    float n2 = FractalNoise(
                        (worldX + noiseOffsetX + 137.2f) * sludgeNoiseScale * 0.55f,
                        (worldY + noiseOffsetY + 491.7f) * sludgeNoiseScale * 0.55f,
                        2,
                        0.5f,
                        2f);

                    float n = n1 * 0.7f + n2 * 0.3f;

                    // Optional vertical bias so sludge slightly favors deeper parts.
                    float depthT = ly / (float)Math.Max(1, height - 1);
                    n += MathHelper.Lerp(-0.05f, 0.08f, depthT);

                    sludgeMask[lx, ly] = n > (1f - sludgeCoverage);
                }
            }

            // Smooth into larger, rounder patches.
            for (int pass = 0; pass < sludgeSmoothPasses; pass++)
                SmoothSludgeMask(sludgeMask, solidMask);

            // Remove tiny specks and preserve only tiles embedded in solid regions.
            for (int lx = 1; lx < width - 1; lx++)
            {
                for (int ly = 1; ly < height - 1; ly++)
                {
                    if (!sludgeMask[lx, ly])
                        continue;

                    if (!solidMask[lx, ly])
                    {
                        sludgeMask[lx, ly] = false;
                        continue;
                    }

                    int sludgeNeighbors = 0;
                    int solidNeighbors = 0;

                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            if (ox == 0 && oy == 0)
                                continue;

                            if (solidMask[lx + ox, ly + oy])
                                solidNeighbors++;

                            if (sludgeMask[lx + ox, ly + oy])
                                sludgeNeighbors++;
                        }
                    }

                    // Kill isolated dots and thin whiskers.
                    if (sludgeNeighbors <= 1 || solidNeighbors <= 3)
                        sludgeMask[lx, ly] = false;
                }
            }

            // Apply sludge to world.
            for (int lx = 0; lx < width; lx++)
            {
                int worldX = minX + lx;
                if (worldX <= 5 || worldX >= Main.maxTilesX - 5)
                    continue;

                for (int ly = 0; ly < height; ly++)
                {
                    if (!sludgeMask[lx, ly])
                        continue;

                    int worldY = topY + ly;
                    if (worldY <= 5 || worldY >= Main.maxTilesY - 5)
                        continue;

                    Tile tile = Main.tile[worldX, worldY];
                    if (!tile.HasTile)
                        continue;

                    // Only replace the base terrain tile.
                    if (tile.TileType == solidTileType)
                        tile.TileType = sludgeTileType;
                }
            }









            ApplyTopShalesandSprinkle(
            minX,
            topY-1,
            width,
            height,
            solidTileType,
            (ushort)ModContent.TileType<ShaleSand_Tile>(),
            fadeEndT: 1.0f,
            noiseScale: 0.9f,
            maxChance: 0.6f,
            preferExposedStone: false);






            // Final framing.
            for (int x = minX - 2; x <= maxX + 2; x++)
            {
                if (x <= 5 || x >= Main.maxTilesX - 5)
                    continue;

                for (int y = topY - 2; y <= bottomY + 2; y++)
                {
                    if (y <= 5 || y >= Main.maxTilesY - 5)
                        continue;

                    WorldUtils.TileFrame(x, y, true);
                }
            }
        }

        private static void SmoothSludgeMask(bool[,] sludgeMask, bool[,] solidMask)
        {
            int width = sludgeMask.GetLength(0);
            int height = sludgeMask.GetLength(1);
            bool[,] copy = (bool[,])sludgeMask.Clone();

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (!solidMask[x, y])
                    {
                        sludgeMask[x, y] = false;
                        continue;
                    }

                    int neighbors = 0;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            if (ox == 0 && oy == 0)
                                continue;

                            if (copy[x + ox, y + oy])
                                neighbors++;
                        }
                    }

                    // Cellular automata style smoothing.
                    if (copy[x, y])
                        sludgeMask[x, y] = neighbors >= 3;
                    else
                        sludgeMask[x, y] = neighbors >= 5;
                }
            }
        }
        private static void ApplyTopShalesandSprinkle(
    int minX,
    int topY,
    int width,
    int height,
    ushort baseSolidTileType,
    ushort shalesandTileType,
    float fadeEndT = 0.42f,
    float noiseScale = 0.055f,
    float maxChance = 0.42f,
    bool preferExposedStone = true)
        {
            float noiseOffsetX = Terraria.WorldGen.genRand.NextFloat(0f, 10000f);
            float noiseOffsetY = Terraria.WorldGen.genRand.NextFloat(0f, 10000f);

            for (int lx = 1; lx < width - 1; lx++)
            {
                int worldX = minX + lx;
                if (worldX <= 5 || worldX >= Main.maxTilesX - 5)
                    continue;

                for (int ly = 1; ly < height - 1; ly++)
                {
                    int worldY = topY + ly;
                    if (worldY <= 5 || worldY >= Main.maxTilesY - 5)
                        continue;

                    Tile tile = Main.tile[worldX, worldY];
                    if (!tile.HasTile || tile.TileType != baseSolidTileType)
                        continue;

                    float depthT = ly / (float)Math.Max(1, height - 1);

                    // 1 at the top, 0 by fadeEndT and below.
                    float fade = 1f - MathHelper.Clamp(depthT / fadeEndT, 0f, 1f);

                    // Sharpen the falloff so the upper band gets most of it.
                    fade *= fade;

                    if (fade <= 0f)
                        continue;

                    bool exposedAbove = !Framing.GetTileSafely(worldX, worldY - 1).HasTile;
                    bool exposedLeft = !Framing.GetTileSafely(worldX - 1, worldY).HasTile;
                    bool exposedRight = !Framing.GetTileSafely(worldX + 1, worldY).HasTile;

                    if (preferExposedStone && !exposedAbove && !exposedLeft && !exposedRight)
                        continue;

                    float n1 = FractalNoise(
                        (worldX + noiseOffsetX) * noiseScale,
                        (worldY + noiseOffsetY) * noiseScale,
                        3,
                        0.5f,
                        2f);

                    float n2 = FractalNoise(
                        (worldX + noiseOffsetX + 241.7f) * noiseScale * 1.8f,
                        (worldY + noiseOffsetY + 91.3f) * noiseScale * 1.8f,
                        2,
                        0.5f,
                        2f);

                    float noise = n1 * 0.75f + n2 * 0.25f;

                    float chance = maxChance * fade;

                    // Slight bonus for upper-facing ledges / ceilings.
                    if (exposedAbove)
                        chance += 0.10f * fade;

                    if (noise < chance)
                        tile.TileType = shalesandTileType;
                }
            }
        }
        private static float FractalNoise(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float amplitudeSum = 0f;

            for (int i = 0; i < octaves; i++)
            {
                total += ValueNoise2D(x * frequency, y * frequency) * amplitude;
                amplitudeSum += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (amplitudeSum <= 0f)
                return 0f;

            return total / amplitudeSum;
        }

        private static float ValueNoise2D(float x, float y)
        {
            int x0 = (int)MathF.Floor(x);
            int y0 = (int)MathF.Floor(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float tx = x - x0;
            float ty = y - y0;

            float sx = SmoothStep(tx);
            float sy = SmoothStep(ty);

            float n00 = HashToUnitFloat(x0, y0);
            float n10 = HashToUnitFloat(x1, y0);
            float n01 = HashToUnitFloat(x0, y1);
            float n11 = HashToUnitFloat(x1, y1);

            float ix0 = MathHelper.Lerp(n00, n10, sx);
            float ix1 = MathHelper.Lerp(n01, n11, sx);

            return MathHelper.Lerp(ix0, ix1, sy);
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float HashToUnitFloat(int x, int y)
        {
            unchecked
            {
                int h = x;
                h = h * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;

                uint uh = (uint)h;
                return uh / (float)uint.MaxValue;
            }
        }
    }
}

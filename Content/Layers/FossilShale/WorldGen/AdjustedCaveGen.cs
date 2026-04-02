using AbyssOverhaul.Content.Layers.FossilShale.Tiles.Rubble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Utilities;

namespace AbyssOverhaul.Content.Layers.FossilShale.WorldGen
{
    public static partial class FossilShaleGen
    {
        public static void GenerateFossilShaleCaves_Overhauled(
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

            ApplyMaskToWorld(
                solidMask,
                minX,
                maxX,
                topY,
                bottomY,
                solidTileType,
                openWallType,
                (ushort)Sludge,
                0.7f,
                2f,
                4);

            // New post-pass. Does not touch cave carving, only material layout.
            OverhaulFossilShaleMaterials(minX, maxX, topY, bottomY, solidTileType);

           // for (int i = 0; i < tunnels.Count; i++)
            //    DecorateTunnel(tunnels[i], rand, (ushort)ModContent.TileType<XL_Orbbies>());

            int[] tileTypes = new int[]
            {
                ModContent.TileType<MediumOrbbies>(),
                ModContent.TileType<Small_Rock>(),
                ModContent.TileType<XL_Orbbies>()
            };

            int[] tileVariations = new int[]
            {
                12, 4, 16
            };

            for (int k = 0; k < width / 3; k++)
            {
                bool success = false;
                int attempts = 0;

                while (!success)
                {
                    attempts++;
                    if (attempts > 1000)
                        break;

                    int x = Terraria.WorldGen.genRand.Next(minX, maxX);
                    int y = Terraria.WorldGen.genRand.Next(topY, bottomY);
                    int arand = Terraria.WorldGen.genRand.Next(0, 3);

                    int tileType = tileTypes[arand];
                    int placeStyle = Main.rand.Next(tileVariations[arand]);

                    if (Main.tile[x, y].TileType == tileType)
                        continue;

                    Terraria.WorldGen.PlaceTile(x, y, tileType, mute: true, style: placeStyle);
                    success = Main.tile[x, y].TileType == tileType;
                }
            }
            PlaceBacteriaChainsInTunnels(tunnels, rand);

            FrameFossilRegion(minX, maxX, topY, bottomY);
        }

        private static void OverhaulFossilShaleMaterials(
            int minX,
            int maxX,
            int topY,
            int bottomY,
            ushort originalSolidTileType)
        {
            ResetFossilMaterialsToCarbonShale(minX, maxX, topY, bottomY, originalSolidTileType);
            ApplyMicrobialSlatePlates(minX, maxX, topY, bottomY);
            ApplyPocketSludge(minX, maxX, topY, bottomY);
            DepositSettledShaleSand(minX, maxX, topY, bottomY, (maxX - minX) * 9);
            FrameFossilRegion(minX, maxX, topY, bottomY);
        }

        private static void ResetFossilMaterialsToCarbonShale(
            int minX,
            int maxX,
            int topY,
            int bottomY,
            ushort originalSolidTileType)
        {
            for (int x = minX; x < maxX; x++)
            {
                if (x <= 5 || x >= Main.maxTilesX - 5)
                    continue;

                for (int y = topY; y < bottomY; y++)
                {
                    if (y <= 5 || y >= Main.maxTilesY - 5)
                        continue;

                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile)
                        continue;

                    if (IsFossilMaterial(tile.TileType, originalSolidTileType))
                        tile.TileType = (ushort)CarbonShale;
                }
            }
        }

        private static void ApplyMicrobialSlatePlates(
            int minX,
            int maxX,
            int topY,
            int bottomY)
        {
            UnifiedRandom rand = Terraria.WorldGen.genRand;

            int width = maxX - minX;
            int height = bottomY - topY;

            int plateCount = Math.Max(12, (width * height) / 10000);

            for (int i = 0; i < plateCount; i++)
            {
                int cx = rand.Next(minX + 24, maxX - 24);
                int cy = rand.Next(topY + 18, bottomY - 18);

               // if (!IsDenseRockArea(cx, cy, 2))
                //    continue;

                float angle = rand.NextFloat(-0.75f, 0.75f);
                int rx = rand.Next(26, 62);
                int ry = rand.Next(4, 11);
                int segments = rand.Next(2, 5);

                Vector2 dir = angle.ToRotationVector2();
                Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);

                for (int s = 0; s < segments; s++)
                {
                    float along = MathHelper.Lerp(-rx * 0.45f, rx * 0.45f, segments <= 1 ? 0.5f : s / (float)(segments - 1));
                    Vector2 center = new Vector2(cx, cy)
                        + dir * along
                        + normal * rand.NextFloat(-5f, 5f);

                    StampMicrobialSlateBlob(
                        center,
                        Math.Max(14, (int)(rx * rand.NextFloat(0.50f, 0.95f))),
                        Math.Max(3, (int)(ry * rand.NextFloat(0.85f, 1.35f))),
                        angle,
                        rand.NextFloat(0f, 10000f));
                }
            }
        }

        private static void StampMicrobialSlateBlob(
            Vector2 center,
            int rx,
            int ry,
            float angle,
            float seed)
        {
            int minX = (int)center.X - rx - 6;
            int maxX = (int)center.X + rx + 6;
            int minY = (int)center.Y - ry - 6;
            int maxY = (int)center.Y + ry + 6;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 10))
                        continue;

                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile || tile.TileType != (ushort)CarbonShale)
                        continue;

                    // Keep the plates embedded in dense rock so they don't eat into cave silhouettes.
                   

                    Vector2 local = new Vector2(x, y) - center;
                    local = local.RotatedBy(-angle);

                    float nx = local.X / Math.Max(1f, rx);
                    float ny = local.Y / Math.Max(1f, ry);
                    float dist = MathF.Sqrt(nx * nx + ny * ny);

                    float noise =
                        (FractalNoise((x + seed) * 0.08f, (y - seed) * 0.08f, 2, 0.5f, 2f) - 0.5f) * 0.18f +
                        (FractalNoise((x - seed * 0.37f) * 0.16f, (y + seed * 0.61f) * 0.16f, 2, 0.5f, 2f) - 0.5f) * 0.08f;

                    if (dist <= 1f + noise)
                        tile.TileType = (ushort)MicrobialSlate;
                }
            }
        }

        private static void ApplyPocketSludge(
            int minX,
            int maxX,
            int topY,
            int bottomY)
        {
            int width = maxX - minX;
            int height = bottomY - topY;

            float offsetX = Terraria.WorldGen.genRand.NextFloat(0f, 10000f);
            float offsetY = Terraria.WorldGen.genRand.NextFloat(0f, 10000f);

            bool[,] sludgeMask = new bool[width, height];

            for (int lx = 1; lx < width - 1; lx++)
            {
                int worldX = minX + lx;

                for (int ly = 1; ly < height - 1; ly++)
                {
                    int worldY = topY + ly;

                    Tile tile = Framing.GetTileSafely(worldX, worldY);
                    if (!tile.HasTile || tile.TileType != (ushort)CarbonShale)
                        continue;

                    int openNeighbors = CountOpenNeighbors8(worldX, worldY);
                    if (openNeighbors == 0)
                        continue;

                    float depthT = ly / (float)Math.Max(1, height - 1);
                    if (depthT < 0.28f)
                        continue;

                    bool openAbove = !Framing.GetTileSafely(worldX, worldY - 1).HasTile;
                    bool solidBelow = IsRealSupport(Framing.GetTileSafely(worldX, worldY + 1));
                    bool wallExposure = !Framing.GetTileSafely(worldX - 1, worldY).HasTile || !Framing.GetTileSafely(worldX + 1, worldY).HasTile;

                    float n =
                        FractalNoise((worldX + offsetX) * 0.045f, (worldY + offsetY) * 0.045f, 3, 0.5f, 2f) * 0.65f +
                        FractalNoise((worldX + offsetX * 0.41f) * 0.10f, (worldY - offsetY * 0.29f) * 0.10f, 2, 0.5f, 2f) * 0.35f;

                    float score = 0f;
                    score += depthT * 0.40f;
                    score += n * 0.35f;
                    score += MathHelper.Clamp(openNeighbors / 8f, 0f, 1f) * 0.15f;

                    if (openAbove)
                        score += 0.06f;
                    if (solidBelow)
                        score += 0.08f;
                    if (wallExposure)
                        score += 0.06f;

                    if (score >= 0.46f)
                        sludgeMask[lx, ly] = true;
                }
            }

            for (int pass = 0; pass < 2; pass++)
            {
                bool[,] copy = (bool[,])sludgeMask.Clone();

                for (int lx = 1; lx < width - 1; lx++)
                {
                    int worldX = minX + lx;

                    for (int ly = 1; ly < height - 1; ly++)
                    {
                        int worldY = topY + ly;

                        Tile tile = Framing.GetTileSafely(worldX, worldY);
                        if (!tile.HasTile || tile.TileType != (ushort)CarbonShale)
                            continue;

                        if (CountOpenNeighbors8(worldX, worldY) == 0)
                            continue;

                        int sludgeNeighbors = 0;

                        for (int ox = -1; ox <= 1; ox++)
                        {
                            for (int oy = -1; oy <= 1; oy++)
                            {
                                if (ox == 0 && oy == 0)
                                    continue;

                                if (copy[lx + ox, ly + oy])
                                    sludgeNeighbors++;
                            }
                        }

                        if (copy[lx, ly])
                            sludgeMask[lx, ly] = sludgeNeighbors >= 2;
                        else if (sludgeNeighbors >= 5)
                            sludgeMask[lx, ly] = true;
                    }
                }
            }

            for (int lx = 0; lx < width; lx++)
            {
                int worldX = minX + lx;

                for (int ly = 0; ly < height; ly++)
                {
                    if (!sludgeMask[lx, ly])
                        continue;

                    int worldY = topY + ly;
                    Tile tile = Framing.GetTileSafely(worldX, worldY);

                    if (tile.HasTile && tile.TileType == (ushort)CarbonShale)
                        tile.TileType = (ushort)Sludge;
                }
            }
        }

        private static void DepositSettledShaleSand(
            int minX,
            int maxX,
            int topY,
            int bottomY,
            int grainCount)
        {
            UnifiedRandom rand = Terraria.WorldGen.genRand;
            int height = bottomY - topY;

            for (int i = 0; i < grainCount; i++)
            {
                int x = rand.Next(minX + 6, maxX - 6);
                int y = FindFirstAirInColumn(x, topY + 2, bottomY - 8);

                if (y < 0)
                    continue;

                int maxSteps = height * 2;
                int steps = 0;

                while (steps++ < maxSteps)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 1))
                        break;

                    if (Framing.GetTileSafely(x, y).HasTile)
                        break;

                    bool downOpen = !Framing.GetTileSafely(x, y + 1).HasTile;
                    bool downLeftOpen = !Framing.GetTileSafely(x - 1, y + 1).HasTile;
                    bool downRightOpen = !Framing.GetTileSafely(x + 1, y + 1).HasTile;
                    bool leftOpen = !Framing.GetTileSafely(x - 1, y).HasTile;
                    bool rightOpen = !Framing.GetTileSafely(x + 1, y).HasTile;

                    if (downOpen)
                    {
                        y++;
                        continue;
                    }

                    if (downLeftOpen && downRightOpen)
                    {
                        x += rand.NextBool() ? -1 : 1;
                        y++;
                        continue;
                    }

                    if (downLeftOpen && leftOpen)
                    {
                        x--;
                        y++;
                        continue;
                    }

                    if (downRightOpen && rightOpen)
                    {
                        x++;
                        y++;
                        continue;
                    }

                    if (CanSettleShaleSandAt(x, y, topY, bottomY))
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        tile.LiquidAmount = 0;
                        Terraria.WorldGen.PlaceTile(x, y, ShaleSand, mute: true, forced: true);
                    }

                    break;
                }
            }
        }

        private static int FindFirstAirInColumn(int x, int startY, int endY)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (!Terraria.WorldGen.InWorld(x, y, 10))
                    continue;

                if (!Framing.GetTileSafely(x, y).HasTile)
                    return y;
            }

            return -1;
        }

        private static bool CanSettleShaleSandAt(int x, int y, int topY, int bottomY)
        {
            if (!Terraria.WorldGen.InWorld(x, y, 10))
                return false;

            Tile here = Framing.GetTileSafely(x, y);
            Tile below = Framing.GetTileSafely(x, y + 1);

            if (here.HasTile)
                return false;

            if (!IsRealSupport(below))
                return false;

            float depthT = (y - topY) / (float)Math.Max(1, bottomY - topY - 1);

            // Keep most of the sand in upper/mid cave space rather than blanketing the bottom.
            if (depthT > 0.8f)
                return false;

            // Prefer actual open cavities so we make shelves / piles instead of plugging tiny cracks.
            if (CountOpenNeighbors8(x, y) < 2)
                return false;

            // Needs some headroom.
            if (Framing.GetTileSafely(x, y - 1).HasTile && Framing.GetTileSafely(x, y - 2).HasTile)
                return false;

            return true;
        }

        private static bool IsFossilMaterial(ushort tileType, ushort originalSolidTileType)
        {
            return tileType == originalSolidTileType
                || tileType == (ushort)CarbonShale
                || tileType == (ushort)MicrobialSlate
                || tileType == (ushort)Sludge
                || tileType == (ushort)ShaleSand;
        }

        private static bool IsDenseRockArea(int x, int y, int radius)
        {
            int solidCount = 0;
            int total = 0;

            for (int ox = -radius; ox <= radius; ox++)
            {
                for (int oy = -radius; oy <= radius; oy++)
                {
                    int tx = x + ox;
                    int ty = y + oy;

                    if (!Terraria.WorldGen.InWorld(tx, ty, 10))
                        continue;

                    total++;

                    if (Framing.GetTileSafely(tx, ty).HasTile)
                        solidCount++;
                }
            }

            return total > 0 && solidCount >= total - 2;
        }

        private static int CountOpenNeighbors8(int x, int y)
        {
            int count = 0;

            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0)
                        continue;

                    int tx = x + ox;
                    int ty = y + oy;

                    if (!Terraria.WorldGen.InWorld(tx, ty, 10))
                        continue;

                    if (!Framing.GetTileSafely(tx, ty).HasTile)
                        count++;
                }
            }

            return count;
        }

        private static bool IsRealSupport(Tile tile)
        {
            return tile.HasTile && !Main.tileSolidTop[tile.TileType];
        }

        private static void FrameFossilRegion(int minX, int maxX, int topY, int bottomY)
        {
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
    }
}

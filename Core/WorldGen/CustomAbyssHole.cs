using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Walls;
using CalamityMod.Walls.UnsafeWalls;
using Terraria.ID;

namespace CalamityMod.World
{
    public class CustomAbyssHole
    {
        public static int AbyssWidth;
        public static bool AtLeftSideOfWorld = false;
        public static int AbyssChasmBottom = 0;

        public static bool UnlockChests { get; set; }

        public sealed class TrenchSettings
        {
            // Horizontal placement / size.
            public int SideInset = 170;
            public int HalfWidth = 160;
            public int SideWallThickness = 28;

            // Vertical placement.
            public int TopOffsetFromRockLayer = 20;
            public int BottomOffsetFromUnderworldTop = -18;

            // Main carve shape.
            public int EntryRadius = 18;
            public int MidRadius = 28;
            public int BottomRadius = 42;
            public int Meander = 16;
            public int SegmentCount = 24;

            // Initial fill tile for the whole trench region before carving.
            // Layers are expected to overwrite this later.
            public ushort FillTileType = (ushort)ModContent.TileType<Voidstone>();
            public ushort FillWallType = (ushort)ModContent.WallType<UnsafeVoidstoneWall>();

            // Extra irregular side pockets during carving.
            public int SidePocketChance = 2;

            // Cleanup.
            public bool SmoothTiles = true;
            public bool RemoveSmallIslands = true;
            public int SmallIslandCutoff = 75;
        }

        public static void PlaceAbyss()
        {
            PlaceAbyss(new TrenchSettings()
            {
                HalfWidth = 225,
                SideInset = 225,
                SideWallThickness = 100,
                SegmentCount = 3,
                MidRadius = 30,
                BottomRadius = 1,
                TopOffsetFromRockLayer = 0,
                BottomOffsetFromUnderworldTop = -20,
                SmoothTiles = true



            });

        }

        public static void PlaceAbyss(TrenchSettings settings)
        {
            int worldWidth = Main.maxTilesX;
            int worldHeight = Main.maxTilesY;
            int genLimit = worldWidth / 2;
            int rockLayer = (int)Main.rockLayer;
            int underworldTop = worldHeight - 200;

            AtLeftSideOfWorld = Main.dungeonX < genLimit;

            int abyssChasmX = AtLeftSideOfWorld
                ? settings.SideInset
                : worldWidth - settings.SideInset;

            int abyssMinX = AtLeftSideOfWorld
                ? 0
                : Math.Max(0, abyssChasmX - settings.HalfWidth);

            int abyssMaxX = AtLeftSideOfWorld
                ? Math.Min(worldWidth - 1, abyssChasmX + settings.HalfWidth)
                : worldWidth - 1;

            int abyssTopY = rockLayer + settings.TopOffsetFromRockLayer;
            int abyssBottomY = underworldTop + settings.BottomOffsetFromUnderworldTop;

            if (abyssBottomY <= abyssTopY + 60)
                abyssBottomY = abyssTopY + 60;

            AbyssWidth = abyssMaxX - abyssMinX;
            AbyssChasmBottom = abyssBottomY;

            AbyssGenUtils.SetBounds(
                abyssMinX,
                abyssMaxX,
                abyssTopY,
                abyssBottomY,
                abyssChasmX,
                AtLeftSideOfWorld,
                ModContent.GetInstance<AbyssOverhaul.AbyssOverhaul>()
            );
            FillContainer(abyssMinX, abyssMaxX, abyssTopY, abyssBottomY, settings);
            CarveMainTrench(abyssMinX, abyssMaxX, abyssTopY, abyssBottomY, abyssChasmX, settings);

           

            FloodOpenSpace(abyssMinX, abyssMaxX, abyssTopY, abyssBottomY);
            RemoveSmallRoof(abyssTopY, abyssMinX, abyssMaxX);
            Polish(abyssMinX, abyssMaxX, abyssTopY, abyssBottomY, settings);
            ApplyAbyssMaterials(abyssMinX, abyssMaxX, abyssTopY, abyssBottomY);
        }

        private static void RemoveSmallRoof(int abyssTopY, int abyssMinX, int abyssMaxX)
        {
            Point Center = new Vector2(AbyssGenUtils.ChasmX, abyssTopY).ToPoint();
            AbyssWorldGenHelper.CarveBlob(Center.X, Center.Y, 30,20, 0.5f, true);
        }

        private static void FillContainer(int minX, int maxX, int topY, int bottomY, TrenchSettings settings)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = topY; y <= bottomY; y++)
                {
                    if (!WorldGen.InWorld(x, y, 20))
                        continue;

                    Tile tile = Main.tile[x, y];
                    tile.HasTile = true;
                    tile.TileType = settings.FillTileType;
                    tile.LiquidType = LiquidID.Water;
                    tile.LiquidAmount = 255;

                    if (settings.FillWallType != WallID.None)
                        tile.WallType = settings.FillWallType;
                }
            }
        }

        private static void CarveMainTrench(int minX, int maxX, int topY, int bottomY, int chasmX, TrenchSettings settings)
        {
            int leftLimit = minX + settings.SideWallThickness;
            int rightLimit = maxX - settings.SideWallThickness;

            int alignedStartX = chasmX;

            if (AbyssOverhaul.Core.WorldGen.SulphurousSeaRevamp.UpperAbyssTransitionX > 0)
                alignedStartX = Utils.Clamp(AbyssOverhaul.Core.WorldGen.SulphurousSeaRevamp.UpperAbyssTransitionX, leftLimit + 10, rightLimit - 10);

            // Main aligned descent.
            CalamityStyleChasmGenerator(
                alignedStartX,
                topY,
                bottomY,
                leftLimit,
                rightLimit,
                ocean: true,
                settings
            );

            // A tighter inner continuation so the upper abyss feels attached to the sea funnel.
            CalamityStyleChasmGenerator(
                alignedStartX,
                topY,
                bottomY - 40,
                Math.Max(leftLimit, alignedStartX - Math.Max(16, settings.MidRadius * 2)),
                Math.Min(rightLimit, alignedStartX + Math.Max(16, settings.MidRadius * 2)),
                ocean: false,
                settings
            );
        }
        private static void CalamityStyleChasmGenerator(
            int startX,
            int startY,
            int bottomY,
            int leftLimit,
            int rightLimit,
            bool ocean,
            TrenchSettings settings)
        {
            float remainingDepth = Math.Max(1, bottomY - startY);
            int five = 5;

            Vector2 position = new(startX, startY);

            // Matches the feel of Calamity's drift vector:
            // tiny X perturbation, steady downward Y.
            Vector2 velocity = new(
                Terraria.WorldGen.genRand.Next(-1, 2) * 0.1f,
                Terraria.WorldGen.genRand.Next(3, 8) * 0.2f + 0.5f
            );

            double width = Terraria.WorldGen.genRand.Next(5, 7) + settings.EntryRadius;

            while (width > 0.0)
            {
                if (remainingDepth > 0f)
                {
                    width += Terraria.WorldGen.genRand.Next(10);
                    width -= Terraria.WorldGen.genRand.Next(10);

                    // Near the top, keep the shaft tighter. Deeper down, allow it to open up.
                    float progress = Utils.GetLerpValue(startY, bottomY, position.Y, true);
                    bool inLowerExpansionZone = progress > 0.45f;

                    double minWidth;
                    double maxWidth;

                    if (!inLowerExpansionZone)
                    {
                        minWidth = Math.Max(7.0, settings.EntryRadius * 0.45);
                        maxWidth = Math.Max(minWidth + 4.0, settings.MidRadius * 1.25);
                    }
                    else
                    {
                        minWidth = Math.Max(14.0, settings.MidRadius * 0.9);
                        maxWidth = Math.Max(minWidth + 6.0, settings.BottomRadius * 1.45);
                    }

                    width = Math.Clamp(width, minWidth, maxWidth);
                }
                else
                {
                    if (position.Y > bottomY)
                        width -= Terraria.WorldGen.genRand.Next(5) + 8;
                }

                if (position.Y > bottomY && remainingDepth > 0f)
                    remainingDepth = 0f;

                remainingDepth -= 1f;

                int coreMinX, coreMaxX, coreMinY, coreMaxY;

                if (remainingDepth > five)
                {
                    coreMinX = (int)(position.X - width * 0.5);
                    coreMaxX = (int)(position.X + width * 0.5);
                    coreMinY = (int)(position.Y - width * 0.5);
                    coreMaxY = (int)(position.Y + width * 0.5);

                    coreMinX = Utils.Clamp(coreMinX, leftLimit, rightLimit);
                    coreMaxX = Utils.Clamp(coreMaxX, leftLimit, rightLimit);
                    coreMinY = Utils.Clamp(coreMinY, startY, bottomY);
                    coreMaxY = Utils.Clamp(coreMaxY, startY, bottomY);

                    for (int x = coreMinX; x < coreMaxX; x++)
                    {
                        for (int y = coreMinY; y < coreMaxY; y++)
                        {
                            if (Math.Abs(x - position.X) + Math.Abs(y - position.Y) <
                                width * 0.5 * (1.0 + Terraria.WorldGen.genRand.Next(-5, 6) * 0.015))
                            {
                                ClearAndFill(x, y, ocean);
                            }
                        }
                    }
                }

                position += velocity;

                velocity.X += Terraria.WorldGen.genRand.Next(-1, 2) * 0.01f;
                if (velocity.X > 0.02f)
                    velocity.X = 0.02f;
                if (velocity.X < -0.02f)
                    velocity.X = -0.02f;

                // Clamp head horizontally so the chasm never punches through the side seal.
                if (position.X < leftLimit + 6)
                {
                    position.X = leftLimit + 6;
                    velocity.X = Math.Abs(velocity.X);
                }
                else if (position.X > rightLimit - 6)
                {
                    position.X = rightLimit - 6;
                    velocity.X = -Math.Abs(velocity.X);
                }

                int shellMinX = (int)(position.X - width * 1.1);
                int shellMaxX = (int)(position.X + width * 1.1);
                int shellMinY = (int)(position.Y - width * 1.1);
                int shellMaxY = (int)(position.Y + width * 1.1);

                shellMinX = Utils.Clamp(shellMinX, leftLimit, rightLimit);
                shellMaxX = Utils.Clamp(shellMaxX, leftLimit, rightLimit);
                shellMinY = Utils.Clamp(shellMinY, startY, bottomY);
                shellMaxY = Utils.Clamp(shellMaxY, startY, bottomY);

                for (int x = shellMinX; x < shellMaxX; x++)
                {
                    for (int y = shellMinY; y < shellMaxY; y++)
                    {
                        if (Math.Abs(x - position.X) + Math.Abs(y - position.Y) <
                            width * 1.1 * (1.0 + Terraria.WorldGen.genRand.Next(-5, 6) * 0.015))
                        {
                            if (y > startY + Terraria.WorldGen.genRand.Next(7, 16) || remainingDepth <= five)
                                Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;

                            if (ocean)
                            {
                                Main.tile[x, y].LiquidAmount = 255;
                                Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Water;
                            }
                            else
                            {
                                Main.tile[x, y].LiquidAmount = 255;
                                Main.tile[x, y].Get<LiquidData>().LiquidType = LiquidID.Lava;
                            }
                        }
                    }
                }
            }
        }

        private static void CarveDiamondPocket(int centerX, int centerY, int radius, int leftLimit, int rightLimit, int topY, int bottomY, bool ocean)
        {
            int minX = Utils.Clamp(centerX - radius, leftLimit, rightLimit);
            int maxX = Utils.Clamp(centerX + radius, leftLimit, rightLimit);
            int minY = Utils.Clamp(centerY - radius, topY, bottomY);
            int maxY = Utils.Clamp(centerY + radius, topY, bottomY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (Math.Abs(x - centerX) + Math.Abs(y - centerY) <
                        radius * (1.0 + Terraria.WorldGen.genRand.Next(-5, 6) * 0.015))
                    {
                        ClearAndFill(x, y, ocean);
                    }
                }
            }
        }

        private static void ClearAndFill(int x, int y, bool ocean)
        {
            if (!Terraria.WorldGen.InWorld(x, y, 20))
                return;

            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
            Main.tile[x, y].LiquidAmount = 255;
            Main.tile[x, y].Get<LiquidData>().LiquidType = ocean ? LiquidID.Water : LiquidID.Lava;
        }
        private static void FloodAir(GenerationProgress progress)
        {
            progress.Message = "Abyss: flooding trench";

            for (int x = AbyssGenUtils.MinX; x <= AbyssGenUtils.MaxX; x++)
            {
                for (int y = AbyssGenUtils.TopY; y <= AbyssGenUtils.BottomY; y++)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 20))
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                    {
                        tile.LiquidAmount = byte.MaxValue;
                        tile.LiquidType = LiquidID.Water;
                    }
                }
            }
        }
        private static void Polish(int minX, int maxX, int topY, int bottomY, TrenchSettings settings)
        {
            if (settings.RemoveSmallIslands)
                RemoveSmallFloatingIslands(minX, maxX, topY, bottomY, settings.SmallIslandCutoff);

            for (int x = minX + 1; x < maxX - 1; x++)
            {
                for (int y = topY + 1; y < bottomY - 1; y++)
                {
                    if (!WorldGen.InWorld(x, y, 20))
                        continue;

                    Tile tile = Main.tile[x, y];

                    if (!tile.HasTile)
                    {
                        tile.LiquidType = LiquidID.Water;
                        tile.LiquidAmount = 255;
                        continue;
                    }

                    if (settings.SmoothTiles)
                        Tile.SmoothSlope(x, y, true);
                }
            }

            // Reinforce the outer walls one final time so side leaks don't happen.
            ReinforceOuterWalls(minX, maxX, topY, bottomY, settings.SideWallThickness, settings);
        }

        private static void ReinforceOuterWalls(int minX, int maxX, int topY, int bottomY, int wallThickness, TrenchSettings settings)
        {
            for (int x = minX; x < minX + wallThickness; x++)
            {
                for (int y = topY; y <= bottomY; y++)
                    ForceSolid(x, y, settings);
            }

            for (int x = maxX - wallThickness; x <= maxX; x++)
            {
                for (int y = topY; y <= bottomY; y++)
                    ForceSolid(x, y, settings);
            }
        }

        private static void ForceSolid(int x, int y, TrenchSettings settings)
        {
            if (!WorldGen.InWorld(x, y, 20))
                return;

            Tile tile = Main.tile[x, y];
            tile.HasTile = true;
            tile.TileType = settings.FillTileType;
            tile.LiquidType = LiquidID.Water;
            tile.LiquidAmount = 255;

            if (settings.FillWallType != WallID.None)
                tile.WallType = settings.FillWallType;
        }

        private static void RemoveSmallFloatingIslands(int minX, int maxX, int topY, int bottomY, int cutoff)
        {
            var visited = new HashSet<Point>();
            var component = new List<Point>();
            var queue = new Queue<Point>();

            for (int x = minX + 1; x < maxX - 1; x++)
            {
                for (int y = topY + 1; y < bottomY - 1; y++)
                {
                    Point start = new Point(x, y);
                    if (visited.Contains(start))
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                    {
                        visited.Add(start);
                        continue;
                    }

                    component.Clear();
                    queue.Clear();

                    queue.Enqueue(start);
                    visited.Add(start);

                    while (queue.Count > 0 && component.Count <= cutoff)
                    {
                        Point p = queue.Dequeue();
                        component.Add(p);

                        TryQueue(p.X + 1, p.Y);
                        TryQueue(p.X - 1, p.Y);
                        TryQueue(p.X, p.Y + 1);
                        TryQueue(p.X, p.Y - 1);
                    }

                    if (component.Count > 0 && component.Count < cutoff)
                    {
                        foreach (Point p in component)
                        {
                            Tile t = Main.tile[p.X, p.Y];
                            t.HasTile = false;
                            t.LiquidType = LiquidID.Water;
                            t.LiquidAmount = 255;
                        }
                    }
                }
            }

            void TryQueue(int x, int y)
            {
                Point p = new Point(x, y);
                if (visited.Contains(p))
                    return;
                if (!WorldGen.InWorld(x, y, 20))
                    return;
                if (x < minX || x > maxX || y < topY || y > bottomY)
                    return;

                visited.Add(p);

                if (!Main.tile[x, y].HasTile)
                    return;

                queue.Enqueue(p);
            }
        }
        private static void FloodOpenSpace(int minX, int maxX, int topY, int bottomY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = topY; y <= bottomY; y++)
                {
                    if (!WorldGen.InWorld(x, y, 20))
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                    {
                        tile.LiquidType = LiquidID.Water;
                        tile.LiquidAmount = 255;
                    }
                }
            }
        }

        public static void FillTileWithWater(int i, int j)
        {
            var tile = Main.tile[i, j];
            tile.LiquidAmount = 255;
            tile.LiquidType = LiquidID.Water;
        }

        private static float ValueNoise(float x, float y)
        {
            int ix = (int)MathF.Floor(x);
            int iy = (int)MathF.Floor(y);

            float fx = x - ix;
            float fy = y - iy;

            float v00 = Hash01(ix, iy);
            float v10 = Hash01(ix + 1, iy);
            float v01 = Hash01(ix, iy + 1);
            float v11 = Hash01(ix + 1, iy + 1);

            float sx = SmoothStep(fx);
            float sy = SmoothStep(fy);

            float a = MathHelper.Lerp(v00, v10, sx);
            float b = MathHelper.Lerp(v01, v11, sx);
            return MathHelper.Lerp(a, b, sy);
        }

        private static float Hash01(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);

        private static void ApplyAbyssMaterials(int minX, int maxX, int topY, int bottomY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = topY; y <= bottomY; y++)
                {
                    if (!WorldGen.InWorld(x, y, 10))
                        continue;

                    Tile tile = Main.tile[x, y];
                    bool canConvert = CanConvertAbyssTile(tile);

                    // Convert existing solids/walls that belong to the abyss mass.
                    if (canConvert)
                    {
                        ApplyMaterialForDepth(tile, y, allowDither: true);
                    }
                    // Optional: seed some new solids into empty spots so you still get
                    // those scattered clusters the original code made.
                    else if (!tile.HasTile && ShouldBackfillEmptySpot(x, y))
                    {
                        tile.HasTile = true;
                        tile.Slope = 0;
                        tile.IsHalfBlock = false;

                        ApplyMaterialForDepth(tile, y, allowDither: true);
                    }
                }
            }
        }

        private static bool CanConvertAbyssTile(Tile tile)
        {

            // Only repaint actual solids / already-existing abyss mass.
            if (tile.HasTile)
                return true;

            // Or repaint walls in open spaces if you want the background to transition too.
            return tile.WallType != WallID.None;
        }

        private static bool ShouldBackfillEmptySpot(int x, int y)
        {
            // This is the equivalent of the "place smaller clusters everywhere else" idea,
            // but made much more conservative so it doesn't clog your carved routes.

            float t = AbyssGenUtils.TAt(y);

            // Prefer backfilling near edges / deeper areas, less in the central swim space.
            int neighbors = 0;
            if (Framing.GetTileSafely(x + 1, y).HasTile) neighbors++;
            if (Framing.GetTileSafely(x - 1, y).HasTile) neighbors++;
            if (Framing.GetTileSafely(x, y + 1).HasTile) neighbors++;
            if (Framing.GetTileSafely(x, y - 1).HasTile) neighbors++;

            if (neighbors <= 0)
                return false;

            int chance = t switch
            {
                < 0.15f => 18,
                < 0.45f => 14,
                < 0.60f => 11,
                < 0.80f => 10,
                _ => 8
            };

            return WorldGen.genRand.NextBool(chance);
        }

        private static void ApplyMaterialForDepth(Tile tile, int y, bool allowDither)
        {
            // Normalize depth across YOUR abyss, not the whole vanilla world.
            float t = AbyssGenUtils.TAt(y);

            // These thresholds mirror your current layer setup much better:
            // 0.00 - 0.45  : upper abyss
            // 0.45 - 0.60  : thermal vents / pyre zone
            // 0.60 - 0.80  : veil-ish zone
            // 0.80 - 1.00  : bottom voidstone zone

            ushort tileType;
            ushort wallType;

            // Layer 4
            if (t >= 0.80f)
            {
                tileType = (ushort)ModContent.TileType<Voidstone>();
                wallType = (ushort)ModContent.WallType<UnsafeVoidstoneWall>();
            }
            // Layer 3 -> 4 dithering
            else if (t >= 0.78f && allowDither && WorldGen.genRand.NextBool(2))
            {
                tileType = (ushort)ModContent.TileType<Voidstone>();
                wallType = (ushort)ModContent.WallType<UnsafeVoidstoneWall>();
            }
            // Layer 3
            else if (t >= 0.45f)
            {
                tileType = (ushort)ModContent.TileType<PyreMantle>();
                wallType = (ushort)ModContent.WallType<PyreMantleWall>();
            }
            // Layer 2 -> 3 dithering
            else if (t >= 0.43f && allowDither && WorldGen.genRand.NextBool(2))
            {
                tileType = (ushort)ModContent.TileType<PyreMantle>();
                wallType = (ushort)ModContent.WallType<PyreMantleWall>();
            }
            // Layer 1 -> 2 dithering
            else if (t >= 0.12f && t <= 0.16f)
            {
                if (WorldGen.genRand.NextBool(2))
                {
                    tileType = (ushort)ModContent.TileType<AbyssGravel>();
                    wallType = (ushort)ModContent.WallType<UnsafeAbyssGravelWall>();
                }
                else
                {
                    tileType = (ushort)ModContent.TileType<SulphurousShale>();
                    wallType = (ushort)ModContent.WallType<UnsafeSulphurousShaleWall>();
                }
            }
            // Layer 1
            else if (t < 0.12f)
            {
                tileType = (ushort)ModContent.TileType<SulphurousShale>();
                wallType = (ushort)ModContent.WallType<UnsafeSulphurousShaleWall>();
            }
            // Layer 2
            else
            {
                tileType = (ushort)ModContent.TileType<AbyssGravel>();
                wallType = (ushort)ModContent.WallType<UnsafeAbyssGravelWall>();
            }

            if (tile.HasTile)
                tile.TileType = tileType;

            tile.WallType = wallType;
        }
    }
}
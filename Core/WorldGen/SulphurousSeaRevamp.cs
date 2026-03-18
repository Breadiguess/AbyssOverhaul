using System;
using System.Collections.Generic;
using System.Linq;
using AbyssOverhaul.Core.Utilities;
using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Items.Tools.SpawnBlocker;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.Schematics;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Abyss.AbyssAmbient;
using CalamityMod.Tiles.Abyss.Stalactite;
using CalamityMod.Walls;
using CalamityMod.Walls.UnsafeWalls;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace AbyssOverhaul.Core.WorldGen
{
    public class SulphurousSeaRevamp
    {
        #region Fields and Properties
        public static int SeaSilhouetteSeed { get; private set; }

        public static float SandBlockEdgeDescentSmoothness = 0.24f;

        // What percentage it takes for dither effects to start appearing at the edges.
        // As an example, if this value is 0.7 that would mean that tiles that are below 70% of the way down from the top of the ocean would
        // start randomly dithering.
        public const float DitherStartFactor = 0.9f;

        public static int DepthForWater = 12;

        public static float TopWaterDepthPercentage = 0.125f;

        public const float TopWaterDescentSmoothnessMin = 0.26f;

        public const float TopWaterDescentSmoothnessMax = 0.39f;

        public const int TotalSandTilesBeforeWaterMin = 32;

        public const int TotalSandTilesBeforeWaterMax = 45;

        public static float OpenSeaWidthPercentage = 0.795f;

        public const float IslandWidthPercentage = 0.36f;

        public const float IslandCurvatureSharpness = 0.74f;

        // 0-1 value of how jagged the small caves should be. The higher this value is, the more variance you can expect for each step when carving out caves.
        public const float SmallCavesJaggedness = 0.51f;

        // How much of a tendency the small caves have to be cramped instead of large and open, with values between 0-1 emphasizing larger caves while values greater than 1
        // emphasizing more cramped caves.
        public const float SmallCavesBiasTowardsTightness = 2.21f;

        // How much of a magnification is performed when calculating perlin noise for spaghetti caves. The closer to 0 this value is, the more same-y the caves will seem in
        // terms of direction, size, etc.
        public const float SpaghettiCaveMagnification = 0.00193f;

        // 0-1 value that determines the threshold for spaghetti caves being carved out. At 0, no tiles are carved out, at 1, all tiles are carved out.
        // This is used in the formula 'abs(noise(x, y)) < r' to determine whether the cave should remove tiles.
        public static readonly float[] SpaghettiCaveCarveOutThresholds = new float[]
        {
            0.033f,
            0.089f
        };

        public const float CheeseCaveMagnification = 0.00237f;

        public static readonly float[] CheeseCaveCarveOutThresholds = new float[]
        {
            0.32f
        };

        // Percentage of how far down a tile has to be for open caverns to appear.
        public const float OpenCavernStartDepthPercentage = 0.42f;

        // The percentage of tiles on average that should be transformed into water.
        // A value of 1 indicates that every tile should have water.
        // This value should be close to 1, but not exactly, so that when water settles the top of caverns will be open.
        public const float WaterSpreadPercentage = 0.91f;

        public const float HardenedSandstoneLineMagnification = 0.004f;

        public static int MaxIslandHeight = 16;

        public static int MaxIslandDepth = 9;

        public const float IslandLineMagnification = 0.0079f;

        public const int TreeGrowChance = 5;

        public const int MinColumnHeight = 5;

        public const int MaxColumnHeight = 50;

        public const int BeachMaxDepth = 50;

        public const int ScrapPileAnticlumpDistance = 80;

        public const float SandstoneEdgeNoiseMagnification = 0.00115f;

        public const int StalactitePairMinDistance = 6;

        public const int StalactitePairMaxDistance = 44;

        // Loop variables that are accessed via getter methods should be stored externally in local variables for performance reasons.
        public static int BiomeWidth
        {
            get
            {
                return Main.maxTilesX switch
                {
                    // Small worlds.
                    4200 => 370,

                    // Medium worlds.
                    6400 => 515,

                    // Large worlds. This also accounts for worlds of an unknown size, such as extra large worlds.
                    _ => (int)(Main.maxTilesX / 16.8f),
                };
            }
        }
        private enum SeaZone
        {
            OpenBasin,
            SideWall,
            FunnelCore,
            LowerFunnel
        }

        private static bool TryGetSeaInteriorBoundsAtY(int y, int seed, out int left, out int right)
        {
            left = 0;
            right = 0;

            if (y < YStart - 24 || y > YStart + BlockDepth + 8)
                return false;

            GetSeaInteriorBoundsAtY(y, seed, out left, out right);
            return right > left;
        }

        private static SeaZone ClassifySeaZone(int localX, int y, int silhouetteSeed)
        {
            GetSeaInteriorBoundsAtY(y, silhouetteSeed, out int left, out int right);

            int width = Math.Max(1, right - left);
            float t = Utils.GetLerpValue(YStart, YStart + BlockDepth, y, true);
            float xT = Utils.GetLerpValue(left, right, localX, true);

            bool inUpperProtectedBasin = t < 0.34f;
            bool inDeepLowerRegion = t >= 0.48f;

            // Near edges of the carved cavity = side walls.
            if (xT <= 0.16f || xT >= 0.84f)
                return SeaZone.SideWall;

            // Upper reservoir center should remain broadly open.
            if (inUpperProtectedBasin)
                return SeaZone.OpenBasin;

            // Mid and lower center becomes the funnel.
            if (inDeepLowerRegion)
                return SeaZone.LowerFunnel;

            return SeaZone.FunnelCore;
        }

        private static bool CanCarveSmallCaveAt(int localX, int y, int silhouetteSeed)
        {
            SeaZone zone = ClassifySeaZone(localX, y, silhouetteSeed);

            return zone == SeaZone.SideWall || zone == SeaZone.LowerFunnel;
        }

        private static bool CanCarveNoiseCaveAt(int localX, int y, int silhouetteSeed, bool cheese)
        {
            SeaZone zone = ClassifySeaZone(localX, y, silhouetteSeed);

            if (zone == SeaZone.OpenBasin)
                return false;

            if (zone == SeaZone.SideWall)
                return true;

            if (zone == SeaZone.LowerFunnel)
                return true;

            // Funnel core is allowed a bit, but much less aggressively than side walls.
            return cheese ? y >= YStart + (int)(BlockDepth * 0.52f) : y >= YStart + (int)(BlockDepth * 0.60f);
        }

        public static int BlockDepth
        {
            get
            {
                if (Main.remixWorld)
                    return (int)(Main.UnderworldLayer * 0.2f);

                float depthFactor = Main.maxTilesX switch
                {
                    // Small worlds.
                    4200 => 0.8f,

                    // Medium worlds.
                    6400 => 0.85f,

                    // Large worlds.
                    _ => 0.925f
                };
                return (int)((Main.rockLayer + 112 - YStart) * depthFactor);
            }
        }

        public static int TotalCavesInShallowWater => (int)Math.Ceiling(Main.maxTilesX / 1000f);

        public static int MaxTopWaterDepth => (int)(BlockDepth * TopWaterDepthPercentage);

        public static int MinCaveWidth => Main.maxTilesX / 2500;

        public static int MaxCaveWidth => (int)Math.Ceiling(Main.maxTilesX / 566f);

        public static int MinCaveMovementSteps => (int)Math.Ceiling(Main.maxTilesX / 70f);

        public static int MaxCaveMovementSteps => (int)Math.Ceiling(Main.maxTilesX / 40f);

        public static int ColumnCount => Main.maxTilesX / 96;

        public static int GeyserCount => Main.maxTilesX / 137;

        public static int YStart
        {
            get;
            set;
        }

        public static readonly List<int> SulphSeaTiles = new()
        {
            ModContent.TileType<SulphurousSand>(),
            ModContent.TileType<SulphurousSandstone>(),
            ModContent.TileType<HardenedSulphurousSandstone>()
        };

        // Vines cannot grow any higher than this. This is done to prevent vines from growing very close to the sea surface.
        public static int VineGrowTopLimit => (Main.remixWorld ? (int)Main.rockLayer : YStart + 100);
        #endregion

        #region Placement Methods
        public static void PlaceSulphurSea()
        {
            // Settle the foundation for the sea. This involves creating the base sulphurous sea block, old tile cleanup, and creating water at the surface.
            DetermineYStart();
            GenerateSandBlock();

            if (!Main.remixWorld)
                RemoveStupidTilesAboveSea();

            GenerateShallowTopWater();
            GenerateIsland();

            // Cave generation. Some of these borrow concepts and tricks used by Minecraft's new generation.
            GenerateSmallWaterCaverns();
            GenerateSpaghettiWaterCaves();
            GenerateCheeseWaterCaves();

            // Lay down decorations and post-processing effects after the caves are generated.
            DecideHardSandstoneLine();
            MakeSurfaceLessRigid();


            if (!Main.remixWorld)
                LayTreesOnSurface();

            SulphurSeaGenerationAfterAbyss();
        }

        public static void SulphurSeaGenerationAfterAbyss()
        {
            CreateBeach();
            ClearOutStrayTiles();
            ClearAloneTiles();
            //PlaceSulphurReef();
            var scrapPilePositions = PlaceScrapPiles();
            GenerateColumnsInCaverns();
            GenerateHardenedSandstone();
            PlaceAmbience();
            GenerateChests(scrapPilePositions);
        }


        public static int UpperAbyssTransitionX { get; private set; }
        public static int UpperAbyssTransitionY { get; private set; }
        public static int UpperAbyssTransitionHalfWidth { get; private set; }

        private static float BellCurve(float t, float center, float width)
        {
            width = Math.Max(0.001f, width);
            float d = (t - center) / width;
            return MathF.Exp(-d * d);
        }

        private static void GetSeaInteriorBoundsAtY(int y, int seed, out int left, out int right)
        {
            int width = BiomeWidth;
            float t = Utils.GetLerpValue(YStart - 200, YStart + BlockDepth, y, true);

            // Broad upper basin -> mid pinch -> slightly re-opened lower shaft.
            float halfWidth =
                MathHelper.Lerp(width * 0.34f, width * 0.10f, MathF.Pow(t, 1.08f));

            // Make the upper reservoir broad and open.
            halfWidth += BellCurve(t, 0.41f, 0.14f) * width * 0.002f;

            // Create the funnel/throat in the mid-lower section.
            halfWidth -= BellCurve(t, 0.63f, 0.13f) * width * 0.4f;

            // Re-open a little near the bottom so it reads like a real descent path rather than a needle.
            halfWidth += Utils.GetLerpValue(0.1f, 1f, t, true) * width * 0.01f;

            // Keep it centered overall, but allow very subtle drift so it doesn't look CAD-perfect.
            float lateralNoise = FractalBrownianMotion(t * 6.1f, 0.17f, seed, 4) * width * 0.12f;
            float lateralOffset =
                BellCurve(t, 0.30f, 0.20f) * width * 0.018f -
                BellCurve(t, 0.18f, 0.16f) * width * 0.012f +
                lateralNoise;

            int center = (int)MathF.Round(width * 0.5f + lateralOffset);
            int hw = Math.Max(18, (int)MathF.Round(halfWidth));

            left = Utils.Clamp(center - hw, 8, width - 24);
            right = Utils.Clamp(center + hw, 24, width - 8);

            if (right - left < 24)
            {
                int mid = (left + right) / 2;
                left = Math.Max(8, mid - 12);
                right = Math.Min(width - 8, mid + 12);
            }
        }

        private static void PlaceSolidSeaTile(int x, int y, ushort tileType, ushort wallType)
        {
            if (!Terraria.WorldGen.InWorld(x, y, 20))
                return;

            Tile tile = Main.tile[x, y];
            tile.HasTile = true;
            tile.TileType = tileType;
            tile.Slope = SlopeType.Solid;
            tile.IsHalfBlock = false;
            tile.LiquidType = LiquidID.Water;
            tile.LiquidAmount = 255;

            if (wallType != WallID.None)
                tile.WallType = wallType;
        }

        private static void ClearSeaTileToWater(int x, int y, ushort wallType)
        {
            if (!Terraria.WorldGen.InWorld(x, y, 1))
                return;

            Tile tile = Main.tile[x, y];
            tile.HasTile = false;
            tile.Slope = SlopeType.Solid;
            tile.IsHalfBlock = false;
            tile.LiquidType = LiquidID.Water;
            tile.LiquidAmount = 255;

            if (wallType != WallID.None)
                tile.WallType = wallType;
        }

        private static void ApplyWallInBounds(int minX, int maxX, int minY, int maxY, ushort wallType)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 20))
                        continue;

                    if (Main.tile[x, y].HasTile)
                        Main.tile[x, y].WallType = wallType;
                }
            }
        }
        #endregion

        #region Generation Functions
        public static void DetermineYStart()
        {
            int xCheckPosition = GetActualX(BiomeWidth + 1);
            var searchCondition = Searches.Chain(new Searches.Down(3000), new Conditions.IsSolid());
            Point determinedPoint;

            do
            {
                WorldUtils.Find(new Point(xCheckPosition, (int)GenVars.worldSurfaceLow - 20), searchCondition, out determinedPoint);
                xCheckPosition += Abyss.AtLeftSideOfWorld.ToDirectionInt();
            }
            while (CalamityUtils.ParanoidTileRetrieval(determinedPoint.X, determinedPoint.Y).TileType == TileID.Ebonstone);
            YStart = Main.remixWorld ? (int)(Main.UnderworldLayer * 0.8f) : determinedPoint.Y;
        }

        public static void GenerateSandBlock()
        {
            int width = BiomeWidth + 4;
            int maxDepth = BlockDepth;
            ushort blockTileType = (ushort)ModContent.TileType<SulphurousSand>();
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();

            // Instead of a sloped wedge, create a full solid mass that we will carve the basin out of.
            // This gives us much better control over the final silhouette.
            for (int i = 0; i < width; i++)
            {
                int x = GetActualX(i);

                for (int y = YStart; y < YStart + maxDepth + 36; y++)
                    PlaceSolidSeaTile(x, y, blockTileType, y >= YStart + 10 ? wallID : WallID.None);

                if (!Main.remixWorld)
                {
                    for (int y = YStart - 75; y < YStart + 50; y++)
                    {
                        if (Terraria.WorldGen.InWorld(x, y, 20) && Main.tile[x, y].TileType == TileID.PalmTree)
                            Terraria.WorldGen.KillTile(x, y);
                    }
                }
            }
        }

        public static void RemoveStupidTilesAboveSea()
        {
            for (int i = 0; i < BiomeWidth; i++)
            {
                int x = GetActualX(i);
                for (int y = YStart - 140; y < YStart + 80; y++)
                {
                    int type = CalamityUtils.ParanoidTileRetrieval(x, y).TileType;
                    if (YStartWhitelist.Contains(type) ||
                        OtherTilesForSulphSeaToDestroy.Contains(type))
                        CalamityUtils.ParanoidTileRetrieval(x, y).Get<TileWallWireStateData>().HasTile = false;
                    if (WallsForSulphSeaToDestroy.Contains(CalamityUtils.ParanoidTileRetrieval(x, y).WallType))
                        CalamityUtils.ParanoidTileRetrieval(x, y).WallType = WallID.None;
                }
            }
        }

        public static void GenerateShallowTopWater()
        {
            int width = BiomeWidth;
            int maxDepth = BlockDepth;
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();
            SeaSilhouetteSeed = Terraria.WorldGen.genRand.Next();
            int silhouetteSeed = SeaSilhouetteSeed;

            // First, clear any liquid above the sea surface so the surface stays visually flat.
            for (int i = 0; i < width; i++)
            {
                int x = GetActualX(i);
                for (int y = YStart - 20; y < YStart + 20; y++)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 20))
                        continue;

                    Main.tile[x, y].LiquidAmount = 0;
                }
            }

            // Carve the main cavity from top to bottom.
            for (int y = YStart-5; y < YStart + maxDepth; y++)
            {
                GetSeaInteriorBoundsAtY(y, silhouetteSeed, out int left, out int right);

                // Slight side roughness, stronger deeper down, weaker near the surface.
                float t = Utils.GetLerpValue(YStart, YStart + maxDepth, y, true);
                int edgeNoise = (int)MathF.Round(
                    FractalBrownianMotion(y * 0.031f, 0.42f, silhouetteSeed, 3) *
                    MathHelper.Lerp(1.5f, 7f, t)
                );

                left += edgeNoise;
                right -= edgeNoise;

                if (right - left < 20)
                    continue;

                for (int i = left; i <= right; i++)
                {
                    int x = GetActualX(i);
                    ClearSeaTileToWater(x, y, y >= YStart + DepthForWater ? wallID : WallID.None);
                }
            }

            // Cache the lower funnel opening so the upper abyss can align to it later.
            int transitionY = YStart + (int)(maxDepth * 0.86f);
            GetSeaInteriorBoundsAtY(transitionY, silhouetteSeed, out int transitionLeft, out int transitionRight);

            UpperAbyssTransitionY = transitionY;
            UpperAbyssTransitionX = GetActualX((transitionLeft + transitionRight) / 2);
            UpperAbyssTransitionHalfWidth = Math.Max(8, (transitionRight - transitionLeft) / 2);
        }

        public static void GenerateIsland()
        {
            ushort blockTileType = (ushort)ModContent.TileType<SulphurousSand>();
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();

            int centerLocalX = BiomeWidth / 2;
            int centerWorldX = GetActualX(centerLocalX);

            // Sit it in the upper basin, centered, with enough clearance for the player to pass around it.
            int centerY = YStart + MaxTopWaterDepth -38;

            int rx = Math.Max(70, BiomeWidth / 7);
            int ry = Math.Max(24, BlockDepth / 11);

            // Main body.
            AbyssWorldGenHelper.FillBlob(centerWorldX, centerY, rx, ry, blockTileType, 0.27f, true);

            // Flatten the top a bit and bulk the upper mass.
            AbyssWorldGenHelper.FillBlob(centerWorldX, centerY - 8, (int)(rx * 0.92f), (int)(ry * 0.55f), blockTileType, 0.26f, true);

            // Give it a heavier hanging underside.
            AbyssWorldGenHelper.FillBlob(centerWorldX, centerY + 10, (int)(rx * 0.62f), (int)(ry * 0.85f), blockTileType, 0.28f, true);

            // Carve a slight underside notch so it doesn't read like a perfect oval.
            AbyssWorldGenHelper.CarveBlob(centerWorldX, centerY + ry, (int)(rx * 0.34f), (int)(ry * 0.33f), 0.92f, true);

            ApplyWallInBounds(centerWorldX - rx - 4, centerWorldX + rx + 4, centerY - ry - 16, centerY + ry + 20, wallID);
        }
        public static void GenerateSmallWaterCaverns()
        {
            int shallowWaterCaveCount = TotalCavesInShallowWater;
            int minCaveWidth = MinCaveWidth;
            int maxCaveWidth = Math.Min(MaxCaveWidth, 17);
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();

            for (int i = 0; i < shallowWaterCaveCount; i++)
            {
                int startY = Terraria.WorldGen.genRand.Next(
                    YStart + (int)(BlockDepth * 0.24f),
                    YStart + (int)(BlockDepth * 0.78f));

                if (!TryGetSeaInteriorBoundsAtY(startY, SeaSilhouetteSeed, out int left, out int right))
                    continue;

                bool preferLeftWall = Terraria.WorldGen.genRand.NextBool();
                int localStartX = preferLeftWall
                    ? Terraria.WorldGen.genRand.Next(left + 4, left + Math.Max(10, (right - left) / 5))
                    : Terraria.WorldGen.genRand.Next(right - Math.Max(10, (right - left) / 5), right - 4);

                if (!CanCarveSmallCaveAt(localStartX, startY, SeaSilhouetteSeed))
                    continue;

                int worldStartX = GetActualX(localStartX);
                int caveWidth = Terraria.WorldGen.genRand.Next(minCaveWidth + 1, maxCaveWidth + 1);
                int caveSteps = Terraria.WorldGen.genRand.Next(MinCaveMovementSteps, MaxCaveMovementSteps);
                int caveSeed = Terraria.WorldGen.genRand.Next();

                Vector2 cavePosition = new(worldStartX, startY);

                Vector2 baseDirection;
                SeaZone zone = ClassifySeaZone(localStartX, startY, SeaSilhouetteSeed);

                if (zone == SeaZone.SideWall)
                {
                    float inwardX = preferLeftWall ? 1f : -1f;
                    baseDirection = new Vector2(inwardX, 0.65f).SafeNormalize(Vector2.UnitY);
                }
                else
                    baseDirection = Vector2.UnitY;

                for (int j = 0; j < caveSteps; j++)
                {
                    int localX = AbyssGenUtils.OnLeft ? (int)cavePosition.X : (Main.maxTilesX - 1) - (int)cavePosition.X;
                    if (!TryGetSeaInteriorBoundsAtY((int)cavePosition.Y, SeaSilhouetteSeed, out int innerLeft, out int innerRight))
                        break;

                    if (localX <= 4 || localX >= BiomeWidth - 4)
                        break;

                    if (!CanCarveSmallCaveAt(localX, (int)cavePosition.Y, SeaSilhouetteSeed))
                        break;

                    float caveOffsetAngleAtStep = CalamityUtils.PerlinNoise2D(i / 50f, j / 50f, 4, caveSeed) * MathHelper.Pi * 1.1f;
                    Vector2 caveDirection = baseDirection.RotatedBy(caveOffsetAngleAtStep * 0.4f);

                    Terraria.WorldGen.digTunnel(cavePosition.X, cavePosition.Y, caveDirection.X, caveDirection.Y, 1, (int)(caveWidth * 1.15f), true);
                    WorldUtils.Gen(cavePosition.ToPoint(), new Shapes.Circle(caveWidth), Actions.Chain(
                        new Actions.ClearTile(true),
                        new Actions.PlaceWall(wallID, true),
                        new Actions.SetLiquid(LiquidID.Water, (byte)(Terraria.WorldGen.genRand.NextFloat() > WaterSpreadPercentage ? 0 : byte.MaxValue)),
                        new Actions.Smooth(true)
                    ));

                    cavePosition += caveDirection * Math.Max(4, caveWidth - 1);

                    // Pull the cave back inward if it tries to leave the allowed side-wall/lower-funnel band.
                    localX = AbyssGenUtils.OnLeft ? (int)cavePosition.X : (Main.maxTilesX - 1) - (int)cavePosition.X;
                    if (localX < innerLeft + 2 || localX > innerRight - 2)
                        break;

                    float caveWidthFactorInterpolant = (float)Math.Pow(Terraria.WorldGen.genRand.NextFloat(), SmallCavesBiasTowardsTightness);
                    caveWidth = (int)Math.Round(caveWidth * MathHelper.Lerp(1f - SmallCavesJaggedness, 1f + SmallCavesJaggedness, caveWidthFactorInterpolant));

                    if (Terraria.WorldGen.genRand.NextBool(12))
                        caveWidth = (int)(caveWidth * 1.3f);

                    caveWidth = Utils.Clamp(caveWidth, minCaveWidth, maxCaveWidth);
                }
            }
        }

        public static void GenerateSpaghettiWaterCaves()
        {
            int width = BiomeWidth;
            int depth = (int)(BlockDepth * 0.96f);
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();

            for (int c = 0; c < SpaghettiCaveCarveOutThresholds.Length; c++)
            {
                int caveSeed = Terraria.WorldGen.genRand.Next();
                for (int i = 2; i < width; i++)
                {
                    // Initialize variables for the cave.
                    int x = GetActualX(i);
                    for (int y = YStart; y < YStart + depth; y++)
                    {
                        if (!CanCarveNoiseCaveAt(i, y, SeaSilhouetteSeed, cheese: false))
                            continue;
                        float noise = FractalBrownianMotion(i * SpaghettiCaveMagnification, y * SpaghettiCaveMagnification, caveSeed, 5);

                        // Bias noise away from 0, effectively making caves less likely to appear, based on how close it is to the bottom/horizontal edge.
                        // This causes caves to naturally stop as the reach ends instead of abruptly stopping like in the old Sulphurous Sea worldgen.
                        float distanceFromEdge = new Vector2(i / (float)width, (y - YStart) / (float)depth).Length();
                        float biasAwayFrom0Interpolant = Utils.GetLerpValue(0.82f, 0.96f, distanceFromEdge * 0.8f, true);
                        biasAwayFrom0Interpolant += Utils.GetLerpValue(YStart + 12f, YStart, y, true) * 0.2f;
                        biasAwayFrom0Interpolant += Utils.GetLerpValue(width - 19f, width - 4f, i, true) * 0.6f;

                        // If the noise is less than 0, bias to -1, if it's greater than 0, bias away to 1.
                        // This is done instead of biasing to -1 or 1 without exception to ensure that in doing so the noise does not cross into the
                        // cutout threshold near 0 as it interpolates.
                        noise = MathHelper.Lerp(noise, Math.Sign(noise), biasAwayFrom0Interpolant);

                        if (Math.Abs(noise) < SpaghettiCaveCarveOutThresholds[c])
                        {
                            WorldUtils.Gen(new(x, y), new Shapes.Rectangle(1, 1), Actions.Chain(new GenAction[]
                            {
                                new Actions.ClearTile(true),
                                new Actions.PlaceWall(wallID, true),
                                new Actions.SetLiquid(LiquidID.Water, (byte)(Terraria.WorldGen.genRand.NextFloat() > WaterSpreadPercentage ? 0 : byte.MaxValue)),
                                new Actions.Smooth(true)
                            }));
                        }
                    }
                }
            }
        }

        public static void GenerateCheeseWaterCaves()
        {
            int width = BiomeWidth;
            int depth = (int)(BlockDepth * 0.96f);
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();

            for (int c = 0; c < CheeseCaveCarveOutThresholds.Length; c++)
            {
                int caveSeed = Terraria.WorldGen.genRand.Next();
                for (int i = 2; i < width; i++)
                {
                    // Initialize variables for the cave.
                    int x = GetActualX(i);
                    for (int y = YStart; y < YStart + depth; y++)
                    {
                        if (!CanCarveNoiseCaveAt(i, y, SeaSilhouetteSeed, cheese: true))
                            continue;
                        float noise = FractalBrownianMotion(i * CheeseCaveMagnification, y * CheeseCaveMagnification, caveSeed, 6);
                        float distanceFromEdge = new Vector2(i / (float)width, (y - YStart) / (float)depth).Length();

                        float biasToNegativeOneInterpolant = Utils.GetLerpValue(0.82f, 0.96f, distanceFromEdge * 0.8f, true);
                        biasToNegativeOneInterpolant += Utils.GetLerpValue(YStart + OpenCavernStartDepthPercentage * depth, YStart + OpenCavernStartDepthPercentage * depth - 25f, y, true);
                        biasToNegativeOneInterpolant += Utils.GetLerpValue(width - 19f, width - 4f, i, true);
                        if (noise - biasToNegativeOneInterpolant > CheeseCaveCarveOutThresholds[c])
                        {
                            WorldUtils.Gen(new(x, y), new Shapes.Rectangle(1, 1), Actions.Chain(new GenAction[]
                            {
                                new Actions.ClearTile(true),
                                new Actions.PlaceWall(wallID, true),
                                new Actions.SetLiquid(LiquidID.Water, (byte)(Terraria.WorldGen.genRand.NextFloat() > WaterSpreadPercentage ? 0 : byte.MaxValue)),
                                new Actions.Smooth(true)
                            }));
                        }
                    }
                }
            }
        }

        public static void ClearOutStrayTiles()
        {
            int width = BiomeWidth;
            int depth = BlockDepth;
            List<ushort> blockTileTypes = new()
            {
                (ushort)ModContent.TileType<SulphurousSand>(),
                (ushort)ModContent.TileType<SulphurousSandstone>(),
                (ushort)ModContent.TileType<HardenedSulphurousSandstone>(),
            };
            ushort wallID = (ushort)ModContent.WallType<SulphurousSandWall>();

            void getAttachedPoints(int x, int y, List<Point> points)
            {
                Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
                Point p = new(x, y);
                if (!blockTileTypes.Contains(t.TileType) || !t.HasTile || points.Count > 432 || points.Contains(p) || CalculateDitherChance(width, YStart, YStart + depth, x, y) > 0f)
                    return;

                points.Add(p);

                getAttachedPoints(x + 1, y, points);
                getAttachedPoints(x - 1, y, points);
                getAttachedPoints(x, y + 1, points);
                getAttachedPoints(x, y - 1, points);
            }

            // Clear out stray chunks created by caverns.
            for (int i = 1; i < width; i++)
            {
                int x = GetActualX(i);
                for (int y = YStart; y < YStart + depth; y++)
                {
                    List<Point> chunkPoints = new();
                    getAttachedPoints(x, y, chunkPoints);

                    int cutoffLimit = y >= YStart + depth * OpenCavernStartDepthPercentage ? 432 : 50;
                    if (chunkPoints.Count >= 2 && chunkPoints.Count < cutoffLimit)
                    {
                        foreach (Point p in chunkPoints)
                        {
                            WorldUtils.Gen(p, new Shapes.Rectangle(1, 1), Actions.Chain(new GenAction[]
                            {
                                new Actions.ClearTile(true),
                                new Actions.PlaceWall(wallID, true),
                                new Actions.SetLiquid()
                            }));
                        }
                    }
                }
            }
        }

        public static void DecideHardSandstoneLine()
        {
            int width = BiomeWidth;
            int depth = BlockDepth;

            int sandstoneSeed = Terraria.WorldGen.genRand.Next();
            ushort blockTypeToReplace = (ushort)ModContent.TileType<SulphurousSand>();
            ushort blockTypeToPlace = (ushort)ModContent.TileType<HardenedSulphurousSandstone>();
            ushort wallID = (ushort)ModContent.WallType<HardenedSulphurousSandstoneWall>();

            for (int i = 0; i < width; i++)
            {
                for (int y = YStart; y < YStart + depth; y++)
                {
                    int sandstoneLineOffset = (int)(FractalBrownianMotion(i * HardenedSandstoneLineMagnification, y * HardenedSandstoneLineMagnification, sandstoneSeed, 7) * 30) + (int)(depth * OpenCavernStartDepthPercentage);

                    // Make the sandstone line descent a little bit the closer it is to the world edge, to make it look like it "warps" towards the abyss.
                    sandstoneLineOffset -= (int)(Math.Pow(Utils.GetLerpValue(width * 0.1f, width * 0.8f, i, true), 1.72f) * 67f);

                    Point p = new(GetActualX(i), y);
                    Tile t = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y);
                    if (y >= YStart + sandstoneLineOffset && t.HasTile && t.TileType == blockTypeToReplace)
                    {
                        WorldUtils.Gen(p, new Shapes.Rectangle(1, 1), Actions.Chain(new GenAction[]
                        {
                            new Actions.SetTile(blockTypeToPlace, true),
                            new Actions.PlaceWall(wallID, true),
                            new Actions.SetLiquid()
                        }));
                    }
                }
            }
        }

        public static void MakeSurfaceLessRigid()
        {
            int y = YStart;
            int width = BiomeWidth;
            int heightSeed = Terraria.WorldGen.genRand.Next();
            ushort blockTileType = (ushort)ModContent.TileType<SulphurousSand>();
            ushort wallID = (ushort)ModContent.WallType<HardenedSulphurousSandstoneWall>();

            for (int i = 2; i < width; i++)
            {
                int x = GetActualX(i);
                Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);

                // If the tile below is solid, then determine how high it should rise upward.
                // This is done to make the surface less unnaturally flat.
                if (t.HasTile)
                {
                    float noise = FractalBrownianMotion(i * IslandLineMagnification, y * IslandLineMagnification, heightSeed, 5) * 0.5f + 0.5f;
                    noise = MathHelper.Lerp(noise, 0.5f, Utils.GetLerpValue(width - 13f, width - 1f, i, true));

                    int heightOffset = -(int)Math.Round(MathHelper.Lerp(-MaxIslandDepth, MaxIslandHeight, noise));
                    for (int dy = 0; dy != heightOffset; dy += Math.Sign(heightOffset))
                    {
                        WorldUtils.Gen(new(x, y + dy), new Shapes.Rectangle(1, 1), Actions.Chain(new GenAction[]
                        {
                            heightOffset > 0 ? new Actions.ClearTile() : new Actions.SetTile(blockTileType, true),
                            new Actions.PlaceWall(MathHelper.Distance(dy, heightOffset) >= 3f && heightOffset < 0f ? wallID : WallID.None, true),
                            new Actions.SetLiquid(),
                            new Actions.Smooth(true)
                        }));
                    }
                }
            }
        }

        public static void LayTreesOnSurface()
        {
            int width = BiomeWidth;

            for (int i = 0; i < width - 8; i++)
            {
                // Only sometimes generate trees.
                if (!Terraria.WorldGen.genRand.NextBool(TreeGrowChance))
                    continue;

                int x = GetActualX(i);
                int y = YStart - 30;

                // Search downward in hopes of finding a position to generate and grow an acorn.
                // If no such downward tile exists, skip this tile.
                if (!WorldUtils.Find(new(x, y), Searches.Chain(new Searches.Down(MaxIslandDepth + 31), new Conditions.IsSolid()), out Point growPoint))
                    continue;

                x = growPoint.X;
                y = growPoint.Y - 1;

                // Ignore tiles if there's water above.
                if (CalamityUtils.ParanoidTileRetrieval(x, y).LiquidAmount > 0)
                    continue;

                Main.tile[x, y].TileType = TileID.Saplings;
                Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                if (!Terraria.WorldGen.GrowPalmTree(x, y))
                    Terraria.WorldGen.KillTile(x, y);
            }
        }

        /*
        public static void PlaceSulphurReef()
        {
            int beachWidth = Terraria.WorldGen.genRand.Next(150, 190 + 1);
            int x = GetActualX(BiomeWidth - 10);
            float xRatio = Utils.GetLerpValue(BiomeWidth - 10, BiomeWidth + beachWidth, x, true);
            int depth = (int)(Math.Sin((1f - xRatio) * MathHelper.PiOver2) * BeachMaxDepth + 1f);

            int cavePerlinSeed = Terraria.WorldGen.genRand.Next();
            int cavePerlinSeedWalls = Terraria.WorldGen.genRand.Next();

            Point origin = new Point(x + (x < Main.maxTilesX / 2 ? -35 : 35), YStart + depth + 80);
            Vector2 center = origin.ToVector2() * 16f + new Vector2(8f);

            float angle = MathHelper.Pi * 0.15f;
            float otherAngle = MathHelper.PiOver2 - angle;

            int size = 80 + (Main.maxTilesX / 180);
            float actualSize = size * 16f;
            float constant = actualSize * 2f / (float)Math.Sin(angle);

            float fociSpacing = actualSize * (float)Math.Sin(otherAngle) / (float)Math.Sin(angle);
            int verticalRadius = (int)(constant / 16f);

            Vector2 fociOffset = Vector2.UnitY * fociSpacing;
            Vector2 topFoci = center - fociOffset;
            Vector2 bottomFoci = center + fociOffset;

            //first, place a basalt barrier around where the biome will be
            for (int X = origin.X - size - 3; X <= origin.X + size + 3; X++)
            {
                for (int Y = (int)(origin.Y - verticalRadius * 0.4f) - 3; Y <= origin.Y + verticalRadius + 3; Y++)
                {
                    if (CheckReefsCircle(new Point(X, Y), topFoci, bottomFoci, constant, center, out float dist))
                    {
                        float percent = dist / constant;
                        float blurPercent = 0.98f;

                        if (percent > blurPercent)
                        {

                        }
                        else
                        {
                            //clear absolutely everything before generating the caverns
                            Main.tile[X, Y].ClearEverything();

                            //generate perlin noise caves
                            float horizontalOffsetNoise = CalamityUtils.PerlinNoise2D(X / 20f, Y / 20f, 5, unchecked(cavePerlinSeed + 1)) * 0.01f;
                            float cavePerlinValue = CalamityUtils.PerlinNoise2D(X / 1000f, Y / 350f, 5, cavePerlinSeed) + 0.5f + horizontalOffsetNoise;
                            float cavePerlinValue2 = CalamityUtils.PerlinNoise2D(X / 1000f, Y / 350f, 5, unchecked(cavePerlinSeed - 1)) + 0.5f;
                            float caveNoiseMap = (cavePerlinValue + cavePerlinValue2) * 0.5f;
                            float caveCreationThreshold = horizontalOffsetNoise * 3.5f + 0.235f;

                            //kill or place tiles depending on the noise map
                            if (caveNoiseMap * caveNoiseMap > caveCreationThreshold)
                            {
                                Terraria.WorldGen.KillTile(X, Y);
                            }
                            else
                            {
                                Terraria.WorldGen.PlaceTile(X, Y, (ushort)ModContent.TileType<HardenedSulphurousSandstone>());
                            }

                            Main.tile[X, Y].WallType = (ushort)ModContent.WallType<HardenedSulphurousSandstoneWall>();
                            Terraria.WorldGen.PlaceWall(X, Y, ModContent.WallType<HardenedSulphurousSandstoneWall>());

                            Main.tile[X, Y].Get<LiquidData>().LiquidType = LiquidID.Water;
                            Main.tile[X, Y].LiquidAmount = byte.MaxValue;
                        }
                    }
                }
            }

            //place sand
            for (int X = origin.X - size - 3; X <= origin.X + size + 3; X++)
            {
                for (int Y = (int)(origin.Y - verticalRadius * 0.4f) - 3; Y <= origin.Y + verticalRadius + 3; Y++)
                {
                    if (CheckReefsCircle(new Point(X, Y), topFoci, bottomFoci, constant, center, out float dist))
                    {   
                        bool canPlaceSand = false;

                        //place sand clumps on top of exposed navystone
                        if (Main.tile[X, Y].TileType == ModContent.TileType<HardenedSulphurousSandstone>() && !Main.tile[X, Y - 1].HasTile)
                        {
                            canPlaceSand = true;
                        }

                        if (canPlaceSand)
                        {
                            SunkenSea.PlaceSand(X, Y, 3, ModContent.TileType<CalamityMod.Tiles.SunkenSea.VolcanicSand>());
                        }
                    }
                }
            }

            //cleanup
            for (int X = origin.X - size - 3; X <= origin.X + size + 3; X++)
            {
                for (int Y = (int)(origin.Y - verticalRadius * 0.4f) - 3; Y <= origin.Y + verticalRadius + 3; Y++)
                {
                    if (CheckReefsCircle(new Point(X, Y), topFoci, bottomFoci, constant, center, out float dist))
                    {
                        //clean tiles that are sticking out (aka tiles only attached to one tile on one side)
                        bool OnlyRight = !Main.tile[X, Y - 1].HasTile && !Main.tile[X, Y + 1].HasTile && !Main.tile[X - 1, Y].HasTile;
                        bool OnlyLeft = !Main.tile[X, Y - 1].HasTile && !Main.tile[X, Y + 1].HasTile && !Main.tile[X + 1, Y].HasTile;
                        bool OnlyDown = !Main.tile[X, Y - 1].HasTile && !Main.tile[X - 1, Y].HasTile && !Main.tile[X + 1, Y].HasTile;
                        bool OnlyUp = !Main.tile[X, Y + 1].HasTile && !Main.tile[X - 1, Y].HasTile && !Main.tile[X + 1, Y].HasTile;

                        if (OnlyRight || OnlyLeft || OnlyDown || OnlyUp)
                        {
                            Terraria.WorldGen.KillTile(X, Y);
                        }

                        //kill random single floating tiles
                        if (!Main.tile[X, Y - 1].HasTile && !Main.tile[X, Y + 1].HasTile && !Main.tile[X - 1, Y].HasTile && !Main.tile[X + 1, Y].HasTile)
                        {
                            Terraria.WorldGen.KillTile(X, Y);
                        }

                        Tile.SmoothSlope(X, Y);
                    }
                }
            }
        }

        public static bool CheckReefsCircle(Point tile, Vector2 focus1, Vector2 focus2, float distanceConstant, Vector2 center, out float distance)
        {
            Vector2 point = tile.ToWorldCoordinates();

            float distY = center.Y - point.Y;
            point.Y -= distY * 3f;

            float distance1 = Vector2.Distance(point, focus1);
            float distance2 = Vector2.Distance(point, focus2);
            distance = distance1 + distance2;

            return distance <= distanceConstant;
        }
        */

        public static void CreateBeach()
        {
            int beachWidth = Terraria.WorldGen.genRand.Next(150, 190 + 1);
            var searchCondition = Searches.Chain(new Searches.Down(3000), new Conditions.IsSolid());
            ushort sandID = (ushort)ModContent.TileType<SulphurousSand>();
            ushort wallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();

            // Stop immediately if for some strange reason a valid tile could not be located for the beach starting point.
            if (!WorldUtils.Find(new Point(BiomeWidth + 4, Main.remixWorld ? YStart : (int)GenVars.worldSurfaceLow - 20), searchCondition, out Point determinedPoint))
                return;

            Tile tileAtEdge = CalamityUtils.ParanoidTileRetrieval(determinedPoint.X, determinedPoint.Y);

            // Extend outward to encompass some of the desert, if there is one.
            if (tileAtEdge.TileType is TileID.Sand or TileID.Ebonsand or TileID.Crimsand)
                beachWidth += 85;

            // Transform the landscape.
            for (int i = BiomeWidth - 10; i <= BiomeWidth + beachWidth; i++)
            {
                int x = GetActualX(i);
                float xRatio = Utils.GetLerpValue(BiomeWidth - 10, BiomeWidth + beachWidth, i, true);
                float ditherChance = Utils.GetLerpValue(0.92f, 0.99f, xRatio, true);
                int depth = (int)(Math.Sin((1f - xRatio) * MathHelper.PiOver2) * BeachMaxDepth + 1f);
                for (int y = YStart - 50; y < YStart + depth; y++)
                {
                    Tile tileAtPosition = CalamityUtils.ParanoidTileRetrieval(x, y);
                    if (tileAtPosition.HasTile && ValidBeachDestroyTiles.Contains(tileAtPosition.TileType))
                    {
                        // Kill trees manually so that no leftover tiles are present.
                        if (Main.tile[x, y].TileType == TileID.Trees)
                            Terraria.WorldGen.KillTile(x, y);
                        else
                            Main.tile[x, y].Get<TileWallWireStateData>().HasTile = false;
                    }

                    else if (tileAtPosition.HasTile && ValidBeachCovertTiles.Contains(tileAtPosition.TileType) && Terraria.WorldGen.genRand.NextFloat() >= ditherChance)
                        Main.tile[x, y].TileType = sandID;

                    //do not replace dungeon walls ever
                    int[] DungeonWalls = { 7, 94, 95, 8, 98, 99, 9, 96, 97 };
                    if (tileAtPosition.WallType > WallID.None && !DungeonWalls.Contains(tileAtPosition.WallType))
                        Main.tile[x, y].WallType = wallID;
                }
            }

            // Plant new trees.
            if (!Main.remixWorld)
            {
                for (int x = BiomeWidth - 10; x <= BiomeWidth + beachWidth; x++)
                {
                    int trueX = AbyssGenUtils.OnLeft? x : Main.maxTilesX - x;
                    if (!Terraria.WorldGen.genRand.NextBool(10))
                        continue;

                    int y = YStart - 30;
                    if (!WorldUtils.Find(new Point(trueX, y), Searches.Chain(new Searches.Down(100), new Conditions.IsTile(sandID)), out Point treePlantPosition))
                        continue;

                    treePlantPosition.Y--;

                    // Place the saplings and try to grow them.
                    Terraria.WorldGen.PlaceTile(treePlantPosition.X, treePlantPosition.Y, ModContent.TileType<AcidWoodTreeSapling>());
                    Main.tile[treePlantPosition].TileType = TileID.Saplings;
                    Main.tile[treePlantPosition].Get<TileWallWireStateData>().HasTile = true;
                    if (!Terraria.WorldGen.GrowPalmTree(treePlantPosition.X, treePlantPosition.Y))
                        Terraria.WorldGen.KillTile(treePlantPosition.X, treePlantPosition.Y);
                }
            }
        }

        public static void ClearAloneTiles()
        {
            int width = BiomeWidth;
            int depth = BlockDepth;
            List<ushort> blockTileTypes = new()
            {
                (ushort)ModContent.TileType<SulphurousSand>(),
                (ushort)ModContent.TileType<SulphurousSandstone>(),
                (ushort)ModContent.TileType<HardenedSulphurousSandstone>(),
            };

            for (int i = 0; i < width; i++)
            {
                int x = GetActualX(i);
                for (int y = YStart; y < YStart + depth; y++)
                {
                    Tile t = CalamityUtils.ParanoidTileRetrieval(x, y);
                    if (!t.HasTile || !blockTileTypes.Contains(t.TileType))
                        continue;

                    // Check to see if the tile has any cardinal neighbors. If it doesn't, destroy it.
                    if (!CalamityUtils.ParanoidTileRetrieval(x - 1, y).HasTile &&
                    !CalamityUtils.ParanoidTileRetrieval(x + 1, y).HasTile &&
                    !CalamityUtils.ParanoidTileRetrieval(x, y - 1).HasTile &&
                    !CalamityUtils.ParanoidTileRetrieval(x, y + 1).HasTile)
                    {
                        WorldUtils.Gen(new(x, y), new Shapes.Rectangle(1, 1), Actions.Chain(new GenAction[]
                        {
                            new Actions.ClearTile(true),
                            new Actions.ClearWall(true),
                            new Actions.SetLiquid()
                        }));
                    }
                }
            }
        }

        public static List<Vector2> PlaceScrapPiles()
        {
            int tries = 0;
            List<Vector2> pastPlacementPositions = new List<Vector2>();
            for (int i = 0; i < 3; i++)
            {
                tries++;
                if (tries > 20000)
                    continue;

                int x = GetActualX(Terraria.WorldGen.genRand.Next(75, BiomeWidth - 85));
                int y = Terraria.WorldGen.genRand.Next(YStart + (int)(BlockDepth * 0.3f), YStart + (int)(BlockDepth * 0.8f));

                Point pilePlacementPosition = new Point(x, y);

                // If the selected position is sitting inside of a tile, try again.
                if (Terraria.WorldGen.SolidTile(pilePlacementPosition.X, pilePlacementPosition.Y))
                {
                    i--;
                    continue;
                }

                // If the selected position is close to other piles, try again.
                if (pastPlacementPositions.Any(p => Vector2.Distance(p, pilePlacementPosition.ToVector2()) < ScrapPileAnticlumpDistance))
                {
                    i--;
                    continue;
                }

                // Otherwise, decide which pile should be created.
                int pileVariant = Terraria.WorldGen.genRand.Next(7);
                string schematicName = $"Sulphurous Scrap {pileVariant + 1}";
                Vector2? wrappedSchematicArea = SchematicManager.GetSchematicArea(schematicName);

                // Create a log message if for some reason the schematic in question doesn't exist.
                if (!wrappedSchematicArea.HasValue)
                {
                   // CalamityMod.CalamityMod.Log.Warn($"Tried to place a schematic with name \"{schematicName}\". No matching schematic file found.");
                    continue;
                }

                Vector2 schematicArea = wrappedSchematicArea.Value;

                // Decide the placement position by searching downward and looking for the lowest point.
                // If the position is quite steep, try again.
                Vector2 left = pilePlacementPosition.ToVector2() - Vector2.UnitX * schematicArea.X * 0.5f;
                Vector2 right = pilePlacementPosition.ToVector2() + Vector2.UnitX * schematicArea.X * 0.5f;
                while (!Terraria.WorldGen.SolidTile(CalamityUtils.ParanoidTileRetrieval((int)left.X, (int)left.Y)))
                    left.Y++;
                while (!Terraria.WorldGen.SolidTile(CalamityUtils.ParanoidTileRetrieval((int)right.X, (int)right.Y)))
                    right.Y++;

                if (Math.Abs(left.Y - right.Y) >= 20f)
                {
                    i--;
                    continue;
                }

                // If the placement position ended up in the abyss, try again.
                if (left.Y >= YStart + BlockDepth - 50 || right.Y >= YStart + BlockDepth - 50)
                {
                    i--;
                    continue;
                }

                // Pick the lowest point vertically.
                Point bottomCenter = new Point(pilePlacementPosition.X, (int)Math.Max(left.Y, right.Y) + 6);
                bool _ = false;
                SchematicManager.PlaceSchematic<Action<Chest>>(schematicName, bottomCenter, SchematicAnchor.BottomCenter, ref _);

                pastPlacementPositions.Add(bottomCenter.ToVector2());
                tries = 0;
            }
            return pastPlacementPositions;
        }

        //GenerateColumnsInCaverns(int width, int depth, int maxHeight, int minHeight, int columnCount)
        public static void GenerateColumnsInCaverns()
        {
            int columnCount = ColumnCount;
            int width = BiomeWidth;
            int depth = BlockDepth;
            var searchCondition = Searches.Chain(new Searches.Up(MaxColumnHeight), new Conditions.IsSolid());

            for (int c = 0; c < columnCount; c++)
            {
                int x = GetActualX(Terraria.WorldGen.genRand.Next(20, width - 32));
                int y = Terraria.WorldGen.genRand.Next(YStart, YStart + depth - 55);

                bool tryAgain = false;

                // Try again if inside a tile.
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, y);
                Tile right = CalamityUtils.ParanoidTileRetrieval(x + 1, y);
                Tile bottom = CalamityUtils.ParanoidTileRetrieval(x, y + 1);
                Tile bottomRight = CalamityUtils.ParanoidTileRetrieval(x + 1, y + 1);
                if (tile.HasTile || right.HasTile)
                    tryAgain = true;

                // Try again if there is no bottom tile.
                if (!Terraria.WorldGen.SolidTile(bottom) || !Terraria.WorldGen.SolidTile(bottomRight))
                    tryAgain = true;

                // Try again if there is no tile above or the ceiling is not level.
                if (!WorldUtils.Find(new(x, y), searchCondition, out Point top) ||
                !WorldUtils.Find(new(x + 1, y), searchCondition, out Point topRight) || top.Y != topRight.Y)
                {
                    tryAgain = true;
                }

                // Try again if the distance between the top and bottom is too short.
                if (MathHelper.Distance(y, top.Y) < MinColumnHeight)
                    tryAgain = true;

                if (tryAgain)
                {
                    c--;
                    continue;
                }

                if (Terraria.WorldGen.genRand.NextBool(2))
                {
                    GenerateColumn(x, top.Y, y);
                }
            }
        }

        public static void GenerateHardenedSandstone()
        {
            int sandstoneSeed = Terraria.WorldGen.genRand.Next();
            ushort sandstoneID = (ushort)ModContent.TileType<SulphurousSandstone>();
            ushort sandstoneWallID = (ushort)ModContent.WallType<UnsafeSulphurousSandstoneWall>();

            // Edge score evaluation function that determines the propensity a tile has to become sandstone.
            // This is based on how much nearby empty areas there are, allowing for "edges" to appear.
            static int getEdgeScore(int x, int y)
            {
                int edgeScore = 0;
                for (int dx = x - 6; dx <= x + 6; dx++)
                {
                    if (dx == x)
                        continue;

                    if (!CalamityUtils.ParanoidTileRetrieval(dx, y).HasTile)
                        edgeScore++;
                }
                for (int dy = y - 6; dy <= y + 6; dy++)
                {
                    if (dy == y)
                        continue;

                    if (!CalamityUtils.ParanoidTileRetrieval(x, dy).HasTile)
                        edgeScore++;
                }
                return edgeScore;
            }

            for (int i = 1; i < BiomeWidth; i++)
            {
                for (int y = YStart; y <= YStart + BlockDepth; y++)
                {
                    int x = GetActualX(i);
                    float sandstoneConvertChance = FractalBrownianMotion(i * SandstoneEdgeNoiseMagnification, y * SandstoneEdgeNoiseMagnification, sandstoneSeed, 7) * 0.5f + 0.5f;

                    // Make the sandstone appearance chance dependant on the edge score.
                    sandstoneConvertChance *= Utils.GetLerpValue(4f, 11f, getEdgeScore(x, y), true);

                    // Make sandstone less likely to appear on the surface.
                    sandstoneConvertChance *= Utils.GetLerpValue(YStart + 30f, YStart + 54f, y, true);

                    if (Terraria.WorldGen.genRand.NextFloat() > sandstoneConvertChance || sandstoneConvertChance < 0.5f)
                        continue;

                    // Convert to sandstone as necessary.
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            if (Terraria.WorldGen.InWorld(x + dx, y + dy))
                            {
                                if (CalamityUtils.ParanoidTileRetrieval(x + dx, y + dy).TileType != sandstoneID &&
                                SulphSeaTiles.Contains(CalamityUtils.ParanoidTileRetrieval(x + dx, y + dy).TileType))
                                {
                                    Main.tile[x + dx, y + dy].WallType = sandstoneWallID;
                                    Main.tile[x + dx, y + dy].TileType = sandstoneID;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void PlaceAmbience()
        {
            for (int i = 0; i < BiomeWidth; i++)
            {
                int x = GetActualX(i);
                for (int y = YStart - 140; y < (Main.remixWorld ? Main.UnderworldLayer : Main.rockLayer); y++)
                {
                    Tile tile = Main.tile[x, y];
                    Tile tileUp = Main.tile[x, y - 1];
                    Tile tileDown = Main.tile[x, y + 1];

                    if (tile.TileType == ModContent.TileType<SulphurousSand>() || tile.TileType == ModContent.TileType<SulphurousSandstone>() ||
                    tile.TileType == ModContent.TileType<HardenedSulphurousSandstone>() || tile.TileType == ModContent.TileType<SulphurousShale>())
                    {
                        //stalagmites, fossiles, and ribs
                        if (tileUp.LiquidType == LiquidID.Water && tileUp.LiquidAmount > 0 && !tileUp.HasTile)
                        {
                            if (Terraria.WorldGen.genRand.NextBool(25))
                            {
                                ushort[] Crates = new ushort[] { (ushort)ModContent.TileType<PirateCrate4>(),
                                (ushort)ModContent.TileType<PirateCrate5>(), (ushort)ModContent.TileType<PirateCrate6>() };

                                Terraria.WorldGen.PlaceObject(x, y - 1, Terraria.WorldGen.genRand.Next(Crates));
                            }

                            if (Terraria.WorldGen.genRand.NextBool(18))
                            {
                                ushort[] Vents = new ushort[] { (ushort)ModContent.TileType<SteamGeyser1>(),
                                (ushort)ModContent.TileType<SteamGeyser2>(), (ushort)ModContent.TileType<SteamGeyser3>() };

                                Terraria.WorldGen.PlaceObject(x, y - 1, Terraria.WorldGen.genRand.Next(Vents));
                            }

                            if (Terraria.WorldGen.genRand.NextBool(12))
                            {
                                ushort[] Stalagmites = new ushort[] { (ushort)ModContent.TileType<SulphurousStalacmite1>(),
                                (ushort)ModContent.TileType<SulphurousStalacmite2>(), (ushort)ModContent.TileType<SulphurousStalacmite3>(),
                                (ushort)ModContent.TileType<SulphurousStalacmite4>(), (ushort)ModContent.TileType<SulphurousStalacmite5>(),
                                (ushort)ModContent.TileType<SulphurousStalacmite6>() };

                                Terraria.WorldGen.PlaceObject(x, y - 1, Terraria.WorldGen.genRand.Next(Stalagmites));
                            }

                            if (Terraria.WorldGen.genRand.NextBool(15))
                            {
                                ushort[] SulphuricFossils = new ushort[] { (ushort)ModContent.TileType<SulphuricFossil1>(),
                                (ushort)ModContent.TileType<SulphuricFossil2>(), (ushort)ModContent.TileType<SulphuricFossil3>() };

                                Terraria.WorldGen.PlaceObject(x, y - 1, Terraria.WorldGen.genRand.Next(SulphuricFossils));
                            }

                            if (Terraria.WorldGen.genRand.NextBool(18))
                            {
                                ushort[] Ribs = new ushort[] { (ushort)ModContent.TileType<SulphurousRib1>(),
                                (ushort)ModContent.TileType<SulphurousRib2>(), (ushort)ModContent.TileType<SulphurousRib3>(),
                                (ushort)ModContent.TileType<SulphurousRib4>(), (ushort)ModContent.TileType<SulphurousRib5>() };

                                Terraria.WorldGen.PlaceObject(x, y - 1, Terraria.WorldGen.genRand.Next(Ribs));
                            }
                        }

                        //stalactites
                        if (tileDown.LiquidType == LiquidID.Water && tileDown.LiquidAmount > 0 && !tileDown.HasTile)
                        {
                            if (Terraria.WorldGen.genRand.NextBool(12))
                            {
                                ushort[] Stalactites = new ushort[] { (ushort)ModContent.TileType<SulphurousStalactite1>(),
                                (ushort)ModContent.TileType<SulphurousStalactite2>(), (ushort)ModContent.TileType<SulphurousStalactite3>(),
                                (ushort)ModContent.TileType<SulphurousStalactite4>(), (ushort)ModContent.TileType<SulphurousStalactite5>(),
                                (ushort)ModContent.TileType<SulphurousStalactite6>() };

                                Terraria.WorldGen.PlaceObject(x, y + 1, Terraria.WorldGen.genRand.Next(Stalactites));
                            }
                        }
                    }
                }
            }
        }

        public static void GenerateChests(List<Vector2> scrapPilePositions)
        {
            GenerateTreasureChest();
            CalamityUtils.SettleWater(false);
            GenerateOpenAirChestChest();
            GenerateScrapPileChest(scrapPilePositions);
            GenerateDeepWaterChest();
        }

        public static void GenerateTreasureChest()
        {
            // Generate on chest below the island to the edge as buried treasure.
            static bool tryToGenerateTreasureChest(Point chestPoint)
            {
                WorldUtils.Find(chestPoint, Searches.Chain(new Searches.Down(300), new Conditions.IsSolid()), out Point p);
                chestPoint = p;

                // Determine how far down the island chest should generate.
                int minDepth = 32;
                int digDepth = 0;
                Point startingIslandChestPoint = chestPoint;
                while (true)
                {
                    Tile down = CalamityUtils.ParanoidTileRetrieval(chestPoint.X, chestPoint.Y + digDepth);
                    Tile downRight = CalamityUtils.ParanoidTileRetrieval(chestPoint.X + 1, chestPoint.Y + digDepth);

                    // Continue digging straight down as long as you're going through standard solid tiles.
                    // As soon as either tile you find is not a standard solid tile, stop immediately.
                    bool downSolidAndValid = down.HasTile && down.IsTileSolid();
                    bool downRightSolidAndValid = downRight.HasTile && downRight.IsTileSolid();
                    if (digDepth >= minDepth && (!downSolidAndValid || !downRightSolidAndValid))
                        break;

                    digDepth++;
                    if (digDepth >= 80)
                        return false;
                }
                chestPoint.Y += digDepth - 12;

                // Check the nearby area and ensure that it's not exposed to air. The treasure should be buried.
                bool nearbyAreaIsClosed = false;
                while (!nearbyAreaIsClosed)
                {
                    nearbyAreaIsClosed = true;
                    for (int dx = -2; dx < 4; dx++)
                    {
                        for (int dy = -1; dy < 3; dy++)
                        {
                            if (!Main.tile[chestPoint.X + dx, chestPoint.Y - dy].HasTile)
                                nearbyAreaIsClosed = false;
                        }
                    }

                    if (!nearbyAreaIsClosed)
                        chestPoint.Y++;
                }

                // Dig up a bit and place the chest.
                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = 0; dy < 2; dy++)
                    {
                        Main.tile[chestPoint.X + dx, chestPoint.Y - dy].LiquidAmount = 0;
                        Main.tile[chestPoint.X + dx, chestPoint.Y - dy].WallType = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();
                        Main.tile[chestPoint.X + dx, chestPoint.Y - dy].Get<TileWallWireStateData>().HasTile = false;
                    }
                }

                // If a buried chest was placed, force its first item to be the effigy of decay.
                Chest chest = MiscWorldgenRoutines.AddChestWithLoot(chestPoint.X + 1, chestPoint.Y + 1, (ushort)ModContent.TileType<RustyChestTile>());
                if (chest != null)
                {
                    chest.item[0].SetDefaults(ModContent.ItemType<EffigyOfDecay>());
                    chest.item[0].Prefix(-1);
                }
                else
                    return false;

                // Go back to the surface and leave a little bit of sulphurous sandstone instead of sand as a small marker of the treasure.
                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 3; dy++)
                    {
                        // Ensure that palm trees and pots are not transformed.
                        int oldTileType = Main.tile[startingIslandChestPoint.X + dx, startingIslandChestPoint.Y + dy].TileType;
                        if (oldTileType == TileID.PalmTree || !Main.tileSolid[oldTileType])
                        {
                            Terraria.WorldGen.KillTile(startingIslandChestPoint.X + dx, startingIslandChestPoint.Y + dy);
                            continue;
                        }

                        Main.tile[startingIslandChestPoint.X + dx, startingIslandChestPoint.Y + dy].LiquidAmount = 0;
                        Main.tile[startingIslandChestPoint.X + dx, startingIslandChestPoint.Y + dy].TileType = (ushort)ModContent.TileType<SulphurousSandstone>();
                    }
                }
                return true;
            }

            int centerLocalX = BiomeWidth / 2;
            Point islandChestPoint = new(GetActualX(centerLocalX + Terraria.WorldGen.genRand.Next(-12, 13)), YStart - 100);
            while (!tryToGenerateTreasureChest(islandChestPoint))
                islandChestPoint.X += Abyss.AtLeftSideOfWorld.ToDirectionInt();
        }

        public static void GenerateOpenAirChestChest()
        {
            int width = BiomeWidth;
            Dictionary<int, int> depthMap = new();

            for (int i = 60; i < width - 50; i++)
            {
                int x = GetActualX(i);
                int y = YStart + MaxIslandDepth + 2;
                int dy = 0;

                while (CalamityUtils.ParanoidTileRetrieval(x, y + dy).HasTile || CalamityUtils.ParanoidTileRetrieval(x, y + dy).LiquidAmount <= 0)
                    dy++;

                depthMap[x] = CalamityUtils.ParanoidTileRetrieval(x, y).HasTile ? y + dy : 0;
            }

            // Pick a smooth place on the depth map to place the chest. This should happen close to an open air point in the caves.
            for (int i = 0; i < 400; i++)
            {
                int x = depthMap.Keys.ElementAt(Terraria.WorldGen.genRand.Next(10, depthMap.Count - 10));
                int leftY = depthMap[x - 1];
                int currentY = depthMap[x];
                int rightY = depthMap[x + 1];
                int averageY = (leftY + currentY + rightY) / 3;

                if (Math.Abs(averageY - currentY) < 3f && currentY > 0)
                {
                    currentY += 3;

                    // Ignore the current position if the chest cannot be placed due to tiles in the way.
                    if (CalamityUtils.AnySolidTileInSelection(x, currentY - 1, 4, -4))
                        continue;

                    // Place the chest and ground.
                    for (int dx = -1; dx < 3; dx++)
                    {
                        Main.tile[x + dx, currentY + 1].LiquidAmount = 0;
                        Main.tile[x + dx, currentY + 1].TileType = (ushort)ModContent.TileType<SulphurousSand>();
                        Main.tile[x + dx, currentY + 1].Get<TileWallWireStateData>().HasTile = true;
                    }

                    // If a buried chest was placed, force its first item to be the broken water filter.
                    Chest chest = MiscWorldgenRoutines.AddChestWithLoot(x, currentY - 2, (ushort)ModContent.TileType<RustyChestTile>());
                    if (chest != null)
                    {
                        chest.item[0].SetDefaults(ModContent.ItemType<BrokenWaterFilter>());
                        chest.item[0].Prefix(-1);
                        break;
                    }
                    else
                        continue;
                }
            }
        }

        public static void GenerateScrapPileChest(List<Vector2> scrapPilePositions)
        {
            // Pick a random scrap pile to generate near.
            for (int i = 0; i < 800; i++)
            {
                Point placeToGenerateNear = Terraria.WorldGen.genRand.Next(scrapPilePositions).ToPoint();
                int x = placeToGenerateNear.X + Terraria.WorldGen.genRand.Next(-25 - i / 12, 25 + i / 12);
                int y = placeToGenerateNear.Y + Terraria.WorldGen.genRand.Next(-16 - i / 25, 4 + i / 25);
                if (Terraria.WorldGen.SolidTile(x, y))
                    continue;

                // If a buried chest was successfully placed, force its first item to be the rusty beacon prototype.
                Chest chest = MiscWorldgenRoutines.AddChestWithLoot(x, y, (ushort)ModContent.TileType<RustyChestTile>());
                if (chest != null)
                {
                    chest.item[0].SetDefaults(ModContent.ItemType<RustyBeaconPrototype>());
                    chest.item[0].Prefix(-1);
                    break;
                }
            }
        }

        public static void GenerateDeepWaterChest()
        {
            // Pick a random scrap pile to generate near.
            for (int i = 0; i < 400; i++)
            {
                int x = GetActualX(Terraria.WorldGen.genRand.Next(60, BiomeWidth - 60));
                int y = YStart + Terraria.WorldGen.genRand.Next(BlockDepth - 150, BlockDepth - 60);
                if (Terraria.WorldGen.SolidTile(x, y))
                    continue;

                // Try again if too far down.
                while (y < Main.maxTilesY - 210)
                {
                    if (!Terraria.WorldGen.SolidTile(x, y))
                        y++;
                    else
                    {
                        y -= 3;
                        break;
                    }
                }
                if (y >= YStart + BlockDepth - 60)
                    continue;

                // If a buried chest was successfully placed, force its first item to be the rusty medallion.
                Chest chest = MiscWorldgenRoutines.AddChestWithLoot(x, y, (ushort)ModContent.TileType<RustyChestTile>());
                if (chest != null)
                {
                    chest.item[0].SetDefaults(ModContent.ItemType<ScionsCurio>());
                    chest.item[0].Prefix(-1);
                    break;
                }
            }
        }
        #endregion Generation Functions

        #region Misc Functions
        public static readonly List<int> YStartWhitelist = new()
        {
            TileID.Stone,
            TileID.Dirt,
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Grass,
            TileID.CorruptGrass,
            TileID.CrimsonGrass,
            TileID.ClayBlock,
            TileID.Mud,
            TileID.Copper,
            TileID.Tin,
            TileID.Iron,
            TileID.Lead,
            TileID.Silver,
            TileID.Tungsten,
            TileID.Crimstone,
            TileID.Ebonstone,
            TileID.HardenedSand,
            TileID.CorruptHardenedSand,
            TileID.CrimsonHardenedSand,
            TileID.Coral,
            TileID.BeachPiles,
            TileID.Plants,
            TileID.Plants2,
            TileID.SmallPiles,
            TileID.LargePiles,
            TileID.LargePiles2,
            TileID.Trees,
            TileID.Vines,
            TileID.CorruptThorns,
            TileID.CrimsonThorns,
            TileID.CrimsonVines,
            TileID.Containers,
            TileID.DyePlants,
            TileID.JungleGrass, // Yes, this can happen on rare occasion.
            TileID.SeaOats
        };

        public static readonly List<int> OtherTilesForSulphSeaToDestroy = new()
        {
            TileID.PalmTree,
            TileID.Sunflower,
            TileID.CorruptThorns,
            TileID.CrimsonThorns,
            TileID.CorruptGrass,
            TileID.CorruptPlants,
            TileID.Stalactite,
            TileID.ImmatureHerbs,
            TileID.MatureHerbs,
            TileID.Pots,
            TileID.Pumpkins, // Happens during Halloween
            TileID.FallenLog,
            TileID.LilyPad,
            TileID.VanityTreeSakura,
            TileID.VanityTreeYellowWillow,
            TileID.ShellPile
        };

        public static readonly List<int> WallsForSulphSeaToDestroy = new()
        {
            WallID.Dirt,
            WallID.DirtUnsafe,
            WallID.DirtUnsafe1,
            WallID.DirtUnsafe2,
            WallID.DirtUnsafe3,
            WallID.DirtUnsafe4,
            WallID.Cave6Unsafe, // Rocky dirt wall
            WallID.Grass,
            WallID.GrassUnsafe,
            WallID.Flower,
            WallID.FlowerUnsafe,
            WallID.CorruptGrassUnsafe,
            WallID.EbonstoneUnsafe,
            WallID.CrimstoneUnsafe,
        };

        public static readonly List<int> ValidBeachCovertTiles = new()
        {
            TileID.Dirt,
            TileID.Stone,
            TileID.Crimstone,
            TileID.Ebonstone,
            TileID.Sand,
            TileID.Ebonsand,
            TileID.Crimsand,
            TileID.Grass,
            TileID.CorruptGrass,
            TileID.CrimsonGrass,
            TileID.ClayBlock,
            TileID.Mud,
        };

        public static readonly List<int> ValidBeachDestroyTiles = new()
        {
            TileID.Coral,
            TileID.BeachPiles,
            TileID.Plants,
            TileID.Plants2,
            TileID.SmallPiles,
            TileID.LargePiles,
            TileID.LargePiles2,
            TileID.CorruptThorns,
            TileID.CrimsonThorns,
            TileID.DyePlants,
            TileID.Trees,
            TileID.Sunflower,
            TileID.LilyPad,
            TileID.SeaOats,
            TileID.ImmatureHerbs,
            TileID.MatureHerbs,
            TileID.BloomingHerbs,
            TileID.VanityTreeSakura,
            TileID.VanityTreeYellowWillow,
        };

        // This method is an involutory function, meaning that applying it to the same number twice always yields the original number.
        public static int GetActualX(int x)
        {
            if (!AbyssGenUtils.OnLeft)
                return x;
            else

                return (Main.maxTilesX - 1) - x;
        }

        public static float CalculateDitherChance(int width, int top, int bottom, int x, int y)
        {
            float verticalCompletion = Utils.GetLerpValue(top, bottom, y, true);
            float horizontalDitherChance = Utils.GetLerpValue(DitherStartFactor, 1f, x / (float)width, true);
            float verticalDitherChance = Utils.GetLerpValue(DitherStartFactor, 1f, verticalCompletion, true);
            float ditherChance = horizontalDitherChance + verticalDitherChance;
            if (ditherChance > 1f)
                ditherChance = 1f;

            // Make the dither chance fizzle out at low vertical completion values.
            // This is done so that there isn't dithering on the surface of the sea.
            ditherChance -= Utils.GetLerpValue(0.56f, 0.5f, verticalCompletion, true);
            if (ditherChance < 0f)
                ditherChance = 0f;
            return ditherChance;
        }

        public static float FractalBrownianMotion(float x, float y, int seed, int octaves, float gain = 0.5f, float lacunarity = 2f)
        {
            float result = 0f;
            float frequency = 1f;
            float amplitude = 0.5f;
            x += seed * 0.00489937f % 10f;

            for (int i = 0; i < octaves; i++)
            {
                float noise = NoiseHelper.GetStaticNoise(new Vector2(x, y) * frequency) * 2f - 1f;
                result += noise * amplitude;
                amplitude *= gain;
                frequency *= lacunarity;
            }

            return result;
        }

        public static void GenerateColumn(int left, int top, int bottom)
        {
            int depth = BlockDepth;
            ushort columnID = (ushort)ModContent.TileType<SulphurousColumn>();
            ushort hardenedSandstoneWallID = (ushort)ModContent.WallType<HardenedSulphurousSandstoneWall>();
            ushort sandWallID = (ushort)ModContent.WallType<UnsafeSulphurousSandWall>();
            short variantFrameOffset = (short)(Terraria.WorldGen.genRand.Next(3) * 36);

            for (int x = left; x < left + 2; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    short frameX = (short)((x - left) * 18 + variantFrameOffset);

                    // Use the top frame if at the top, bottom frame if at the bottom, and the middle frame otherwise.
                    short frameY = 18;
                    if (y == top)
                        frameY = 0;
                    else if (y == bottom)
                        frameY = 36;

                    Main.tile[x, y].TileType = columnID;
                    Main.tile[x, y].TileFrameX = frameX;
                    Main.tile[x, y].TileFrameY = frameY;
                    Main.tile[x, y].WallType = y >= YStart + depth * OpenCavernStartDepthPercentage ? hardenedSandstoneWallID : sandWallID;
                    Main.tile[x, y].Get<TileWallWireStateData>().HasTile = true;
                }
            }
        }

        public static void PlaceStalactite(int x, int y, int height, ushort type)
        {
            for (int dy = 0; dy < height; dy++)
            {
                ushort oldWall = Main.tile[x, y + dy].WallType;
                Main.tile[x, y + dy].ClearEverything();
                Main.tile[x, y + dy].WallType = oldWall;
                Main.tile[x, y + dy].TileType = type;
                Main.tile[x, y + dy].TileFrameY = (short)(dy * 18);
                Main.tile[x, y + dy].Get<TileWallWireStateData>().HasTile = true;
            }
        }

        public static void PlaceStalacmite(int x, int y, int height, ushort type)
        {
            for (int dy = height - 1; dy > 0; dy--)
            {
                ushort oldWall = Main.tile[x, y + dy].WallType;
                Main.tile[x, y - dy].ClearEverything();
                Main.tile[x, y - dy].WallType = oldWall;
                Main.tile[x, y - dy].TileType = type;
                Main.tile[x, y - dy].TileFrameY = (short)(height * 18 - dy * 18);
                Main.tile[x, y - dy].Get<TileWallWireStateData>().HasTile = true;
            }
        }
        #endregion
    }
}

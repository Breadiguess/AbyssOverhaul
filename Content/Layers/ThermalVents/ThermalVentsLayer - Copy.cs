using AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles;
using AbyssOverhaul.Content.Layers.TheVeil.NPCs.VoidDreamerNPC;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Abyss.AbyssAmbient;
using CalamityMod.Tiles.Ores;
using CalamityMod.Waters;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using PlantyMush = CalamityMod.Tiles.Abyss.PlantyMush;

namespace AbyssOverhaul.Content.Layers.ThermalVents
{
    internal class ThermalVentsLayer : AbyssLayer
    {
        public static Vector2 ExitPosition { get; private set; } = Vector2.Zero;
        public override int StartHeight => AbyssGenUtils.YAt(0.55f);
        public override int EndHeight => AbyssGenUtils.YAt(0.7f);

        public static ThermalVentsLayer Instance => ModContent.GetInstance<ThermalVentsLayer>();
        public override ModWaterStyle ModWaterStyle => ThermalVentsWater.Instance;
        public override int MusicSlot
        {
            get
            {
                if (CalamityPlayer.areThereAnyDamnBosses)
                    return Main.curMusic;

                int? musicSlot = CalamityClientConfig.Instance.AbyssLayer3Alt ?
                   ModContent.GetInstance<CalamityMod.CalamityMod>().GetMusicFromMusicMod("AbyssLayer3Alt") :
                    ModContent.GetInstance<CalamityMod.CalamityMod>().GetMusicFromMusicMod("AbyssLayer3");

                return musicSlot ?? MusicID.Hell;
            }
        }
    

        private static int PyreMantleType => ModContent.TileType<PyreMantle>();
        private static int MoltenPyreMantleType => ModContent.TileType<PyreMantleMolten>();
        private static int ScoriaType => ModContent.TileType<ScoriaOre>();
        private static int MantleGravelType => ModContent.TileType<MantleGravel_Tile>();
        private static int MineralSlagType => ModContent.TileType<MantleGravel_Tile>();
        private static readonly int[] VentTypes =
         {
            ModContent.TileType<ThermalVent1>(),
            ModContent.TileType<ThermalVent2>(),
            ModContent.TileType<ThermalVent3>()
        };
        private static int PlantyMushType => ModContent.TileType<PlantyMush>(); // replace if exact type name differs
        public override void ModifyGenTasks()
        {
            AddGenTask("ThermalVents", static (_, progress, config) =>
            {
               GenerateThermalVents(progress);
            });
        }

        public override Dictionary<int, float> NPCSpawnPool => new()
        {
            [ModContent.NPCType<DevilFish>()] = 1.2f,
            [ModContent.NPCType<ChaoticPuffer>()] = 0.8f,
            [ModContent.NPCType<Eidolist>()] = 0.2f,
            [ModContent.NPCType<Laserfish>()] = 1.2f,
            [ModContent.NPCType<GulperEelHead>()] = 0.5f,
            [ModContent.NPCType<OarfishHead>()] = 0.6f,
            [ModContent.NPCType<Viperfish>()] = 1.2f,
            [ModContent.NPCType<MirageJelly>()] = 1.2f,
            [ModContent.NPCType<LuminousCorvina>()] = 1.2f,
            [ModContent.NPCType<ColossalSquid>()] = 0.8f,
            [ModContent.NPCType<VoidDreamer>()] = 0.06f,
        }; 
        private static void GenerateThermalVents(GenerationProgress progress)
        {
            progress.Message = "Shaping the Thermal Vents";

            int minX = AbyssGenUtils.MinX;
            int maxX = AbyssGenUtils.MaxX;
            int startY = Instance.StartY;
            int endY = Instance.EndY;

            int width = maxX - minX;
            int height = endY - startY;
            int centerX = (minX + maxX) / 2;

            // 1) Fill the entire layer solid first.
            FillBaseMass(minX, maxX, startY, endY);

            // 2) Build a tunnel graph.
            List<Tunnel> tunnels = BuildTunnelNetwork(minX, maxX, startY, endY, centerX, width, height);

            BuildSideWall(minX, startY, endY, true);
            BuildSideWall(maxX, startY, endY, false);
            // 3) Carve tunnels.
            CarveTunnelNetwork(tunnels);


            // 5) Force proper entrance / exit connection to neighboring layers.
            GuaranteeEntrance(centerX, startY, width);


            GarenteeExit(tunnels, minX, maxX, endY, width);

            CarveVentRooms(tunnels, minX, maxX, startY, endY);

            // 6) Connectivity / clearance pass.
            //EnsureTraversableRoutes(minX, maxX, startY, endY, centerX, width, height);

            // 7) Secondary erosion to roughen tunnel walls.
            ErodeEdges(minX, maxX, startY, endY, PyreMantleType, 3);
            ErodeEdges(minX, maxX, startY, endY, MoltenPyreMantleType, 16);
            ErodeEdges(minX, maxX, startY, endY, ScoriaType,14);
            // 8) Materials afterward, once the cave shape exists.
            //PlaceScoriaPockets(minX, maxX, startY, endY);
            PlaceScoriaDeposits(minX, maxX, startY, endY, centerX, width, height);
            PlaceVisibleScoriaTeasers(minX, maxX, startY, endY);
            PlaceMineralSlagPockets(minX, maxX, startY, endY);
            AddMantleGravel(minX, maxX, startY, endY);
            PlaceThermalVentsAndFloraFromTunnels(tunnels, minX, maxX, startY, endY);

            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, startY, endY, 2, 3, true);
            AbyssWorldGenHelper.ReframeArea(minX, maxX, startY, endY);
        }

        private static void EnsureTraversableRoutes(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            Point start = FindNearestOpenPoint(centerX, startY + 14, minX, maxX, startY, endY);
            Point goal = FindNearestOpenPoint(centerX - (int)(width * 0.08f), endY - 14, minX, maxX, startY, endY);

            if (start == Point.Zero || goal == Point.Zero)
            {
                // Hard fallback.
                AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                    new Vector2(centerX, startY + 12),
                    new Vector2(centerX - width * 0.08f, endY - 12),
                    7, 8, 0.12f, true);
                return;
            }

            if (HasTraversablePath(start, goal, minX, maxX, startY, endY))
                return;

            // Rescue carve if connectivity failed.
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                start.ToVector2(),
                goal.ToVector2(),
                7, 8, 0.14f, true);
        }
        private static Point FindNearestOpenPoint(int centerX, int centerY, int minX, int maxX, int minY, int maxY)
        {
            for (int r = 0; r <= 20; r++)
            {
                for (int x = centerX - r; x <= centerX + r; x++)
                {
                    for (int y = centerY - r; y <= centerY + r; y++)
                    {
                        if (x < minX + 3 || x > maxX - 3 || y < minY + 3 || y > maxY - 3)
                            continue;

                        if (CanPlayerFitAt(x, y))
                            return new Point(x, y);
                    }
                }
            }

            return Point.Zero;
        }
        private static bool CanPlayerFitAt(int x, int y)
        {
            // Approximate standing/swimming clearance.
            // Centered around a tile location.
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    Tile tile = Framing.GetTileSafely(x + dx, y + dy);
                    if (tile.HasTile)
                        return false;
                }
            }

            return true;
        }
        private static bool HasTraversablePath(Point start, Point goal, int minX, int maxX, int minY, int maxY)
        {
            Queue<Point> queue = new();
            HashSet<Point> visited = new();

            queue.Enqueue(start);
            visited.Add(start);

            Point[] dirs =
            {
        new Point(1, 0),
        new Point(-1, 0),
        new Point(0, 1),
        new Point(0, -1)
    };

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();

                if (Vector2.Distance(p.ToVector2(), goal.ToVector2()) <= 3f)
                    return true;

                for (int i = 0; i < dirs.Length; i++)
                {
                    Point n = p + dirs[i];

                    if (n.X < minX + 3 || n.X > maxX - 3 || n.Y < minY + 3 || n.Y > maxY - 3)
                        continue;

                    if (visited.Contains(n))
                        continue;

                    if (!CanPlayerFitAt(n.X, n.Y))
                        continue;

                    visited.Add(n);
                    queue.Enqueue(n);
                }
            }

            return false;
        }
        private static List<VentRoom> CarveVentRooms(List<Tunnel> tunnels, int minX, int maxX, int startY, int endY)
        {
            UnifiedRandom rand = WorldGen.genRand;
            List<VentRoom> rooms = new();

            // Shuffle candidate anchors so results vary.
            List<Vector2> candidates = new();
            for (int i = 0; i < tunnels.Count; i++)
            {
                candidates.Add(tunnels[i].Start);
                candidates.Add(tunnels[i].End);

                // Also allow a midpoint sometimes so rooms are not only at endpoints.
                if (rand.NextBool(4))
                    candidates.Add(Vector2.Lerp(tunnels[i].Start, tunnels[i].End, rand.NextFloat(0.3f, 0.7f)));
            }

            // Fisher-Yates shuffle.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            int desiredRooms = Math.Min(7, Math.Max(4, tunnels.Count / 3));
            int attempts = Math.Min(candidates.Count, desiredRooms * 6);

            for (int i = 0; i < attempts && rooms.Count < desiredRooms; i++)
            {
                Vector2 p = candidates[i];

                int rx = rand.Next(10, 46);
                int ry = rand.Next(6, 32);

                VentRoom candidate = new(
                    new Vector2(
                        MathHelper.Clamp(p.X, minX + rx + 6, maxX - rx - 6),
                        MathHelper.Clamp(p.Y, startY + ry + 6, endY - ry - 6)),
                    rx,
                    ry);

                if (!CanPlaceRoom(candidate, rooms))
                    continue;

                rooms.Add(candidate);

                AbyssWorldGenHelper.CarveBlob(
                    (int)candidate.Center.X,
                    (int)candidate.Center.Y,
                    candidate.RadiusX,
                    candidate.RadiusY,
                   // TileID.Stone,
                   0.8f,
                    true);
            }

            return rooms;
        }
        private static bool CanPlaceRoom(VentRoom candidate, List<VentRoom> existingRooms)
        {
            for (int i = 0; i < existingRooms.Count; i++)
            {
                VentRoom other = existingRooms[i];

                // Treat the spacing like expanded ellipse bounds.
                float dx = Math.Abs(candidate.Center.X - other.Center.X);
                float dy = Math.Abs(candidate.Center.Y - other.Center.Y);

                // Padding is important. Without it, rooms will visually merge even if
                // their nominal radii barely do not intersect.
                float minDx = candidate.RadiusX + other.RadiusX + 10f;
                float minDy = candidate.RadiusY + other.RadiusY + 8f;

                if (dx < minDx && dy < minDy)
                    return false;
            }

            return true;
        }



        private static void GarenteeExit(List<Tunnel> tunnels, int minX, int maxX, int endY, int width)
        {
            if (tunnels is null || tunnels.Count == 0)
                return;

            List<Vector2> candidates = new();

            for (int i = 0; i < tunnels.Count; i++)
            {
                candidates.Add(tunnels[i].Start);
                candidates.Add(tunnels[i].End);
            }

            Vector2 best = candidates[0];

            for (int i = 1; i < candidates.Count; i++)
            {
                Vector2 p = candidates[i];

                float bestScore = best.Y - Math.Abs(((minX + maxX) * 0.5f) - best.X) * 0.08f;
                float score = p.Y - Math.Abs(((minX + maxX) * 0.5f) - p.X) * 0.08f;

                if (score > bestScore)
                    best = p;
            }

            Vector2 start = new Vector2(
                MathHelper.Clamp(best.X, minX + 12, maxX - 12),
                Math.Min(best.Y + 2f, endY - 6f));

            Vector2 down = new(start.X, endY + 22f);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(down, start, 6, 8, 0.10f, true);

            float horizontalBias = start.X < (minX + maxX) * 0.5f ? width * 0.08f : -width * 0.08f;
            Vector2 lowerTurn = new(start.X + horizontalBias, endY + 52f);

            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(down, lowerTurn, 5, 6, 0.12f, true);

            ExitPosition = lowerTurn;
        }


        private static void FillBaseMass(int minX, int maxX, int startY, int endY)
        {
            UnifiedRandom rand = WorldGen.genRand;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    float heatNoise = AbyssWorldGenHelper.FractalNoise(x * 0.028f, y * 0.28f, 3, 0.5f, 0.2f);

                    int tileType = heatNoise > 0.62f && rand.NextBool(3)
                        ? MoltenPyreMantleType
                        : PyreMantleType;

                    AbyssWorldGenHelper.FillBlob(x, y, 1,1, tileType, 0.2f, true);
                }
            }
        }
        private static int[] GetVentTypes() => new int[]
        {
            ModContent.TileType<ThermalVent1>(),
            ModContent.TileType<ThermalVent2>(),
            ModContent.TileType<ThermalVent3>()
        };
        private static int ChooseVentType()
        {
            int[] ventTypes = GetVentTypes();
            return ventTypes[WorldGen.genRand.Next(ventTypes.Length)];
        }
        private static void PlaceThermalVentsAndFloraFromTunnels(List<Tunnel> tunnels, int minX, int maxX, int minY, int maxY)
        {
            if (tunnels is null || tunnels.Count == 0)
                return;

            UnifiedRandom rand = WorldGen.genRand;
            List<Point> placedVents = new();

            for (int i = 0; i < tunnels.Count; i++)
            {
                Tunnel t = tunnels[i];

                float length = Vector2.Distance(t.Start, t.End);
                int samples = Math.Max(2, (int)(length / 24f));

                for (int s = 0; s <= samples; s++)
                {
                    // Do not try every sample every time, or it gets too dense.
                    if (!rand.NextBool())
                        continue;

                    float tValue = s / (float)Math.Max(1, samples);
                    Vector2 sample = Vector2.Lerp(t.Start, t.End, tValue);

                    // Slight jitter so vents do not look mathematically attached to line samples.
                    sample.X += rand.NextFloat(-4f, 4f);
                    sample.Y += rand.NextFloat(-2f, 2f);

                    Point? ventAnchor = FindVentAnchorNearTunnelSample((int)sample.X, (int)sample.Y, minX, maxX, minY, maxY);
                    if (ventAnchor is null)
                        continue;

                    Point p = ventAnchor.Value;

                    bool tooClose = false;
                    for (int j = 0; j < placedVents.Count; j++)
                    {
                        if (Vector2.Distance(placedVents[j].ToVector2(), p.ToVector2()) < 22f)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                        continue;

                    int ventType = ChooseVentType();

                    WorldGen.PlaceTile(p.X, p.Y, ventType, mute: true, forced: true);
                    Tile placed = Framing.GetTileSafely(p.X, p.Y);

                    if (!placed.HasTile || placed.TileType != ventType)
                        continue;

                    placedVents.Add(p);

                    PlacePlantyMushNearVent(p.X, p.Y, minX, maxX, minY, maxY);
                }
            }
        }
        private static Point? FindVentAnchorNearTunnelSample(int sampleX, int sampleY, int minX, int maxX, int minY, int maxY)
        {
            // Search a local column/area under and around the tunnel sample.
            for (int radius = 0; radius <= 8; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = sampleX + dx;
                    if (x < minX + 4 || x > maxX - 4)
                        continue;

                    // Search slightly downward first, because vents usually sit on cave floors.
                    for (int dy = 0; dy <= 10; dy++)
                    {
                        int y = sampleY + dy;
                        if (y < minY + 4 || y > maxY - 4)
                            continue;

                        if (IsValidVentAnchorFromFloor(x, y))
                            return new Point(x, y);
                    }
                }
            }

            return null;
        }
        private static bool IsValidVentAnchorFromFloor(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 10))
                return false;

            Tile tile = Framing.GetTileSafely(x, y);
            Tile above = Framing.GetTileSafely(x, y - 1);
            Tile above2 = Framing.GetTileSafely(x, y - 2);

            

            if (tile.TileType != PyreMantleType &&
                tile.TileType != MoltenPyreMantleType &&
                tile.TileType != MantleGravelType &&
                tile.TileType != MineralSlagType)
                return false;

            if (above.HasTile || above2.HasTile)
                return false;

            // Require this to actually be a floor surface, not just random exposed rock.
            if (!Framing.GetTileSafely(x, y + 1).HasTile)
                return false;

            // Avoid narrow shafts / cramped pockets.
            bool leftAir = !Framing.GetTileSafely(x - 1, y - 1).HasTile;
            bool rightAir = !Framing.GetTileSafely(x + 1, y - 1).HasTile;
            if (!leftAir && !rightAir)
                return false;

            return true;
        }
  
        private static void PlacePlantyMushNearVent(int ventX, int ventY, int minX, int maxX, int minY, int maxY)
        {
            UnifiedRandom rand = WorldGen.genRand;

            int attempts = 430;
            int placed = 0;
            int desired = rand.Next(20, 60);

            for (int i = 0; i < attempts && placed < desired; i++)
            {
                int dx = rand.Next(-11, 11);
                int dy = rand.Next(-2, 9);

                if (Math.Abs(dx) <= 1 && dy >= 1)
                    continue;

                int x = ventX + dx;
                int y = ventY + dy;

                if (x < minX + 4 || x > maxX - 4 || y < minY + 4 || y > maxY - 4)
                    continue;

                if (!IsValidPlantyMushAnchor(x, y, ventX, ventY))
                    continue;

                AbyssWorldGenHelper.FillBlobReplace(x, y, 3, 2, PlantyMushType, 0.2f, MantleGravelType, PyreMantleType );
                if (Framing.GetTileSafely(x, y).HasTile && Framing.GetTileSafely(x, y).TileType == PlantyMushType)
                    placed++;
            }
        }
        private static bool IsValidPlantyMushAnchor(int x, int y, int ventX, int ventY)
        {
            return true;
        }
     
        private static void GuaranteeEntrance(int centerX, int startY, int width)
        {
            Vector2 start = new Vector2(centerX, startY - 8);
            Vector2 end = start + Vector2.UnitY * 34f;
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start, end, 6, 10, 0.08f, true);

            Vector2 lowerTurn = end + new Vector2(-width * 0.08f, 34f);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(end, lowerTurn, 5, 5, 0.10f, true);
        }

       
        private static int CountOpenTilesNearby(int centerX, int centerY, int radius)
        {
            int open = 0;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (!WorldGen.InWorld(x, y, 10))
                        continue;

                    if (!Framing.GetTileSafely(x, y).HasTile)
                        open++;
                }
            }

            return open;
        }
        private static bool IsGoodScoriaAnchor(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 10))
                return false;

            Tile tile = Framing.GetTileSafely(x, y);
            if (!tile.HasTile)
                return false;

            if (tile.TileType != PyreMantleType && tile.TileType != MoltenPyreMantleType)
                return false;

            // Require solid neighbors in most directions so the ore sits inside mass.
            int solidNeighbors = 0;
            if (Framing.GetTileSafely(x + 1, y).HasTile) solidNeighbors++;
            if (Framing.GetTileSafely(x - 1, y).HasTile) solidNeighbors++;
            if (Framing.GetTileSafely(x, y + 1).HasTile) solidNeighbors++;
            if (Framing.GetTileSafely(x, y - 1).HasTile) solidNeighbors++;

            if (solidNeighbors < 3)
                return false;

            // Reject anchors too close to open cave space.
            if (CountOpenTilesNearby(x, y, 4) > 8)
                return false;

            return true;
        }
        private static void PlaceScoriaDeposits(int minX, int maxX, int minY, int maxY, int centerX, int width, int height)
        {
            UnifiedRandom rand = WorldGen.genRand;

            int depositCount = 12 + rand.Next(4);

            for (int i = 0; i < depositCount; i++)
            {
                bool leftSide = rand.NextBool();
                float sideBandT = rand.NextFloat(0.14f, 0.34f);

                int targetX = leftSide
                    ? minX + (int)(width * sideBandT)
                    : maxX - (int)(width * sideBandT);

                int targetY = rand.Next(minY + 16, maxY - 16);

                Point? anchor = FindScoriaAnchorNear(targetX, targetY, 20);
                if (anchor is null)
                    continue;

                int rx;
                int ry;

                int roll = rand.Next(120);
                if (roll < 60)
                {
                    rx = rand.Next(5, 9);
                    ry = rand.Next(4, 7);
                }
                else if (roll < 90)
                {
                    rx = rand.Next(8, 13);
                    ry = rand.Next(5, 9);
                }
                else
                {
                    rx = rand.Next(12, 18);
                    ry = rand.Next(7, 11);
                }

                AbyssWorldGenHelper.FillBlobReplace(
                    anchor.Value.X,
                    anchor.Value.Y,
                    rx,
                    ry,
                    ScoriaType,
                    3f,
                    PyreMantleType,
                    MoltenPyreMantleType);

                if (rand.NextBool(3))
                {
                    int offsetX = rand.Next(-rx / 2, rx / 2 + 1);
                    int offsetY = rand.Next(-ry / 2, ry / 2 + 1);

                    AbyssWorldGenHelper.FillBlobReplace(
                        anchor.Value.X + offsetX,
                        anchor.Value.Y + offsetY,
                        Math.Max(5, rx - rand.Next(2, 5)),
                        Math.Max(3, ry - rand.Next(1, 4)),
                        ScoriaType,
                        2f,
                        PyreMantleType,
                        MoltenPyreMantleType);
                }
            }
        }
        private static Point? FindScoriaAnchorNear(int targetX, int targetY, int searchRadius)
        {
            UnifiedRandom rand = WorldGen.genRand;

            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = targetX + rand.Next(-searchRadius, searchRadius + 1);
                int y = targetY + rand.Next(-searchRadius, searchRadius + 1);

                if (IsGoodScoriaAnchor(x, y))
                    return new Point(x, y);
            }

            return null;
        }
        private static void PlaceMineralSlagPockets(int minX, int maxX, int minY, int maxY)
        {
            for (int x = minX + 3; x < maxX - 3; x++)
            {
                for (int y = minY + 3; y < maxY - 3; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile || tile.TileType != PyreMantleType)
                        continue;

                    bool nearOpen =
                        !Framing.GetTileSafely(x + 1, y).HasTile ||
                        !Framing.GetTileSafely(x - 1, y).HasTile ||
                        !Framing.GetTileSafely(x, y + 1).HasTile ||
                        !Framing.GetTileSafely(x, y - 1).HasTile;

                    if (!nearOpen)
                        continue;

                    float noise = AbyssWorldGenHelper.FractalNoise(x * 0.055f + 40f, y * 0.055f + 90f, 2, 1f, 1f);
                    if (noise > 0.71f && WorldGen.genRand.NextBool(8))
                    {
                        int rx = WorldGen.genRand.Next(2, 5);
                        int ry = WorldGen.genRand.Next(2, 4);
                        AbyssWorldGenHelper.FillBlobReplace(
                            x, y, rx, ry, MineralSlagType, 0.10f,
                            PyreMantleType, MoltenPyreMantleType);
                    }
                }
            }
        }
        private static void PlaceVisibleScoriaTeasers(int minX, int maxX, int minY, int maxY)
        {
            UnifiedRandom rand = WorldGen.genRand;

            for (int x = minX + 3; x < maxX - 3; x++)
            {
                for (int y = minY + 3; y < maxY - 3; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile)
                        continue;

                    if (tile.TileType != PyreMantleType && tile.TileType != MoltenPyreMantleType)
                        continue;

                    bool nearOpen =
                        !Framing.GetTileSafely(x + 1, y).HasTile ||
                        !Framing.GetTileSafely(x - 1, y).HasTile ||
                        !Framing.GetTileSafely(x, y + 1).HasTile ||
                        !Framing.GetTileSafely(x, y - 1).HasTile;

                    if (!nearOpen)
                        continue;

                    float noise = AbyssWorldGenHelper.FractalNoise(x * 0.065f + 15f, y * 0.065f + 70f, 2, 1f, 1f);

                    // Much rarer and much smaller than the current pocket pass.
                    if (noise > 0.78f && rand.NextBool(16))
                    {
                        AbyssWorldGenHelper.FillBlobReplace(
                            x, y,
                            rand.Next(2, 4),
                            rand.Next(2, 3),
                            ScoriaType,
                            0.10f,
                            PyreMantleType,
                            MoltenPyreMantleType);
                    }
                }
            }
        }
        private static void AddMantleGravel(int minX, int maxX, int minY, int maxY)
        {
            for (int x = minX + 2; x < maxX - 2; x++)
            {
                for (int y = minY + 2; y < maxY - 2; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile)
                        continue;

                    if (tile.TileType != PyreMantleType)
                        continue;

                    bool exposedAbove = !Framing.GetTileSafely(x, y - 1).HasTile;
                    bool supportedBelow = Framing.GetTileSafely(x, y + 1).HasTile;

                    if (!exposedAbove || !supportedBelow)
                        continue;

                    float t = (y - minY) / (float)Math.Max(1, maxY - minY);
                    float noise = AbyssWorldGenHelper.FractalNoise(x * 0.06f, y * 0.06f, 2, 1f, 1f);

                    if (t > 0.55f && noise > 0.64f && WorldGen.genRand.NextBool(5))
                        tile.TileType = (ushort)MantleGravelType;
                }
            }
        }

        private static void ErodeEdges(int minX, int maxX, int minY, int maxY, int tileType, int chance)
        {
            for (int x = minX + 2; x < maxX - 2; x++)
            {
                for (int y = minY + 2; y < maxY - 2; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile || tile.TileType != tileType)
                        continue;

                    if (!WorldGen.genRand.NextBool(chance))
                        continue;

                    int exposedSides = 0;
                    if (!Framing.GetTileSafely(x + 1, y).HasTile) exposedSides++;
                    if (!Framing.GetTileSafely(x - 1, y).HasTile) exposedSides++;
                    if (!Framing.GetTileSafely(x, y + 1).HasTile) exposedSides++;
                    if (!Framing.GetTileSafely(x, y - 1).HasTile) exposedSides++;

                    if (exposedSides >= 2)
                        WorldGen.KillTile(x, y, false, false, true);
                }
            }
        }

        private static void BuildSideWall(int edgeX, int startY, int endY, bool leftSide)
        {
            int dir = leftSide ? 1 : -1;

            for (int offset = 0; offset < 38; offset++)
            {
                int x = edgeX + offset * dir;
                int inset = 10 + offset / 2;

                for (int y = startY + inset; y <= endY - inset; y++)
                {
                    int type = WorldGen.genRand.NextBool(4) ? MoltenPyreMantleType : PyreMantleType;
                    AbyssWorldGenHelper.PlaceSolidTile(x, y, type, true);
                }
            }
        }
    
        #region Tunnels

        private static List<Tunnel> BuildTunnelNetwork(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            UnifiedRandom rand = WorldGen.genRand;
            List<Tunnel> tunnels = new();

            Vector2 entry = new(centerX, startY + 18);
            Vector2 exit = new(centerX - width * 0.1f, endY - 18);

            // Main descending spine through the layer.
            List<Vector2> spinePoints = new();
            spinePoints.Add(entry);

            int spineSegments = 12;
            for (int i = 1; i < spineSegments; i++)
            {
                float t = i / (float)(spineSegments - 1);

                float x =
                    MathHelper.Lerp(centerX, centerX - width * 0.08f, t) +
                    rand.NextFloat(-width * 0.12f, width * 0.12f);

                float y =
                    MathHelper.Lerp(startY + 22, endY - 22, t) +
                    rand.NextFloat(-12f, 12f);

                x = MathHelper.Clamp(x, minX + 24, maxX - 24);
                y = MathHelper.Clamp(y, startY + 18, endY - 18);

                spinePoints.Add(new Vector2(x, y));
            }

            spinePoints.Add(exit);

            for (int i = 0; i < spinePoints.Count - 1; i++)
            {
                tunnels.Add(new Tunnel(
                    spinePoints[i],
                    spinePoints[i + 1],
                    rand.Next(6, 10),
                    rand.Next(6, 10),
                    rand.NextFloat(0.14f, 1f),
                    true));
            }

            // Add branches off the spine.
            int branchCount = 3;
            for (int i = 0; i < branchCount; i++)
            {
                Vector2 anchor = spinePoints[rand.Next(1, spinePoints.Count - 1)];

                float dir = rand.NextBool() ? -1f : 1f;
                float branchLength = rand.NextFloat(width * 0.22f, width * 0.5f);
                float rise = rand.NextFloat(-height * 0.12f, height * 0.12f);

                Vector2 end = new(
                    MathHelper.Clamp(anchor.X + dir * branchLength, minX + 18, maxX - 18),
                    MathHelper.Clamp(anchor.Y + rise, startY + 18, endY - 18));

                tunnels.Add(new Tunnel(
                    anchor,
                    end,
                    rand.Next(4, 8),
                    rand.Next(4, 7),
                    rand.NextFloat(0.38f, 1.6f),
                    true));

                // Occasional sub-branch.
                if (rand.NextBool(3))
                {
                    Vector2 mid = Vector2.Lerp(anchor, end, rand.NextFloat(0.35f, 0.7f));
                    Vector2 end2 = new(
                        MathHelper.Clamp(mid.X + rand.NextFloat(-width * 0.18f, width * 0.18f), minX + 18, maxX - 18),
                        MathHelper.Clamp(mid.Y + rand.NextFloat(-height * 0.14f, height * 0.14f), startY + 18, endY - 18));

                    tunnels.Add(new Tunnel(
                        mid,
                        end2,
                        rand.Next(3, 6),
                        rand.Next(3, 6),
                        rand.NextFloat(0.18f, 0.32f),
                        true));
                }
            }

            return tunnels;
        }
        private static void CarveTunnelNetwork(List<Tunnel> tunnels)
        {
            for (int i = 0; i < tunnels.Count; i++)
            {
                Tunnel t = tunnels[i];
                AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                    t.Start,
                    t.End,
                    t.RadiusX,
                    t.RadiusY,
                    t.Irregularity,
                    t.FillWithWater);
            }
        }


        private struct Tunnel
        {
            public Vector2 Start;
            public Vector2 End;
            public int RadiusX;
            public int RadiusY;
            public float Irregularity;
            public bool FillWithWater;

            public Tunnel(Vector2 start, Vector2 end, int radiusX, int radiusY, float irregularity, bool fillWithWater = true)
            {
                Start = start;
                End = end;
                RadiusX = radiusX;
                RadiusY = radiusY;
                Irregularity = irregularity;
                FillWithWater = fillWithWater;
            }
        }

        #endregion

        private struct VentRoom
        {
            public Vector2 Center;
            public int RadiusX;
            public int RadiusY;

            public VentRoom(Vector2 center, int radiusX, int radiusY)
            {
                Center = center;
                RadiusX = radiusX;
                RadiusY = radiusY;
            }
        }


    }
}
using AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles;
using AbyssOverhaul.Content.Layers.TheVeil.NPCs.VoidDreamerNPC;
using AbyssOverhaul.Content.NPCs.DeepSnapperNPC;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Waters;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace AbyssOverhaul.Content.Layers.TenebrousMarsh
{
    internal class TenebrousMarshLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(0.4f);
        public override int EndHeight => AbyssGenUtils.YAt(0.55f);

        public override ModWaterStyle ModWaterStyle => MiddleAbyssWater.Instance;

        public override Dictionary<int, float> NPCSpawnPool => new()
        {
            [ModContent.NPCType<DevilFish>()] = 1.2f,
            [ModContent.NPCType<GiantSquid>()] = 0.8f,
            [ModContent.NPCType<GulperEelHead>()] = 0.2f,
            [ModContent.NPCType<Laserfish>()] = 0.9f,
            [ModContent.NPCType<OarfishHead>()] = 0.7f,
            [ModContent.NPCType<Viperfish>()] = 0.8f,
            [ModContent.NPCType<VoidDreamer>()] = 0.02f,
            [ModContent.NPCType<DeepSnapper>()] = 1.1f,
        };

        private static int PyreMantleType => ModContent.TileType<MantleGravel_Tile>();
        private static int MoltenPyreMantleType => ModContent.TileType<PyreMantleMolten>();
        private static int PlantyMush => ModContent.TileType<PlantyMush>();
        private static int MantleGravelType => ModContent.TileType<PyreMantle>();
        private static int TenebrisType => ModContent.TileType<Tenebris_Tile>();
        private static int BrineCrystalTile => ModContent.TileType<SmoothedBrineCrystal>();
        private static int BaroRoot => TileID.LivingMahogany;

        private enum ShelfAnchor
        {
            None,
            Left,
            Right
        }

        private readonly struct ShelfSeed
        {
            public readonly int CenterX;
            public readonly int CenterY;
            public readonly int HalfWidth;
            public readonly int HalfHeight;
            public readonly ShelfAnchor Anchor;
            public readonly float Slope;
            public readonly bool AddStem;

            public ShelfSeed(int centerX, int centerY, int halfWidth, int halfHeight, ShelfAnchor anchor, float slope, bool addStem)
            {
                CenterX = centerX;
                CenterY = centerY;
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
                Anchor = anchor;
                Slope = slope;
                AddStem = addStem;
            }
        }

        public override void ModifyGenTasks()
        {
            AddGenTask("Tenebrous Marsh Layout", GenerateTerrain);
        }

        private static void GenerateTerrain(AbyssLayer layer, GenerationProgress progress, GameConfiguration configuration)
        {
            TenebrousMarshLayer marsh = (TenebrousMarshLayer)layer;

            progress.Message = "Shaping the Tenebrous Marsh";

            int minX = AbyssGenUtils.MinX + 10;
            int maxX = AbyssGenUtils.MaxX - 10;
            int startY = marsh.StartY + 4;
            int endY = marsh.EndY - 4;

            ClearRegionToWater(minX, maxX, startY, endY);
            //BuildSideFrames(minX, maxX, startY, endY);

            List<ShelfSeed> shelves = CreateShelfSeeds(minX, maxX, startY, endY);

            foreach (ShelfSeed shelf in shelves)
                StampShelf(shelf, major: true);

            AddMinorShelves(minX, maxX, startY, endY, 5 + WorldGen.genRand.Next(4));


            // Add local pockets and underside voids for the overhang feel.
           // foreach (ShelfSeed shelf in shelves)
            //    CarveShelfPockets(shelf);

            //GOD I LOVE WRITING OVERLY COMPLICATED CODE THAT NOT ONLY DOESN;T SEEM TO DO WHAT I WANT IT TO DO, BUT ALSO SUCKS !!!
           // CarveExtraGapChambers(minX, maxX, startY, endY, 4 + WorldGen.genRand.Next(3));
            //ApplyMaterialVariation(minX, maxX, startY, endY);
            BuildSideFrames(minX, maxX, startY, endY);
            // Do this after shelf placement so the route is always guaranteed open.
            //CarveMainRoute(minX, maxX, startY, endY);
            AbyssWorldGenHelper.FloodOpenSpace(minX, maxX, startY, endY);
            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, startY, endY, maxNeighbors: 1, chanceDenominator: 1, fillWithWater: true);
            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, startY, endY, maxNeighbors: 2, chanceDenominator: 2, fillWithWater: true);
            AbyssWorldGenHelper.ReframeArea(minX, maxX, startY, endY);
        }

        private static void ClearRegionToWater(int minX, int maxX, int startY, int endY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = startY; y <= endY; y++)
                    AbyssWorldGenHelper.ClearTile(x, y, fillWithWater: true);
            }
        }

        private static void BuildSideFrames(int minX, int maxX, int startY, int endY)
        {
            int leftWidth = 14 + WorldGen.genRand.Next(4);
            int rightWidth = 14 + WorldGen.genRand.Next(4);

            AbyssWorldGenHelper.ForceSolidRect(minX, minX + leftWidth, startY, endY, TenebrisType, fillWithWater: false);
            AbyssWorldGenHelper.ForceSolidRect(maxX - rightWidth, maxX, startY, endY, TenebrisType, fillWithWater: false);

            // Small broken caps help frame the region and keep it from reading like a giant empty rectangle.
            for (int i = 0; i < 3; i++)
            {
                int x = WorldGen.genRand.Next(minX + 25, maxX - 25);
                int y = startY + WorldGen.genRand.Next(2, 10);
                AbyssWorldGenHelper.FillBlob(x, y, WorldGen.genRand.Next(16, 30), WorldGen.genRand.Next(8, 14), PlantyMush, 0.64f, false);
            }

            for (int i = 0; i < 2; i++)
            {
                int x = WorldGen.genRand.Next(minX + 30, maxX - 30);
                int y = endY - WorldGen.genRand.Next(2, 10);
                AbyssWorldGenHelper.FillBlob(x, y, WorldGen.genRand.Next(20, 34), WorldGen.genRand.Next(8, 14), PlantyMush, 0.9f, false);
            }
        }

        private static List<ShelfSeed> CreateShelfSeeds(int minX, int maxX, int startY, int endY)
        {
            List<ShelfSeed> shelves = new();

            int width = maxX - minX;
            bool upperLeft = WorldGen.genRand.Next(2) == 0;

            shelves.Add(CreateAnchoredShelf(
                upperLeft ? ShelfAnchor.Left : ShelfAnchor.Right,
                minX, maxX,
                LerpI(startY, endY, 0.14f) + WorldGen.genRand.Next(-4, 5),
                width / 7 + WorldGen.genRand.Next(8, 20),
                WorldGen.genRand.Next(10, 16),
                WorldGen.genRand.NextFloat(-0.18f, 0.18f),
                addStem: true));

            shelves.Add(CreateAnchoredShelf(
                upperLeft ? ShelfAnchor.Right : ShelfAnchor.Left,
                minX, maxX,
                LerpI(startY, endY, 0.28f) + WorldGen.genRand.Next(-5, 6),
                width / 8 + WorldGen.genRand.Next(6, 16),
                WorldGen.genRand.Next(10, 16),
                WorldGen.genRand.NextFloat(-0.25f, 0.25f),
                addStem: true));

            shelves.Add(new ShelfSeed(
                centerX: minX + width / 2 + WorldGen.genRand.Next(-width / 10, width / 10 + 1),
                centerY: LerpI(startY, endY, 0.44f) + WorldGen.genRand.Next(-6, 7),
                halfWidth: width / 5 + WorldGen.genRand.Next(10, 22),
                halfHeight: WorldGen.genRand.Next(14, 22),
                anchor: ShelfAnchor.None,
                slope: WorldGen.genRand.NextFloat(-0.35f, 0.35f),
                addStem: true));

            shelves.Add(CreateAnchoredShelf(
                upperLeft ? ShelfAnchor.Right : ShelfAnchor.Left,
                minX, maxX,
                LerpI(startY, endY, 0.60f) + WorldGen.genRand.Next(-6, 7),
                width / 7 + WorldGen.genRand.Next(10, 20),
                WorldGen.genRand.Next(12, 18),
                WorldGen.genRand.NextFloat(-0.30f, 0.30f),
                addStem: true));

            shelves.Add(new ShelfSeed(
                centerX: minX + width / 2 + WorldGen.genRand.Next(-width / 8, width / 8 + 1),
                centerY: LerpI(startY, endY, 0.80f) + WorldGen.genRand.Next(-4, 5),
                halfWidth: width / 5 + WorldGen.genRand.Next(8, 18),
                halfHeight: WorldGen.genRand.Next(12, 18),
                anchor: WorldGen.genRand.Next(3) == 0 ? (upperLeft ? ShelfAnchor.Left : ShelfAnchor.Right) : ShelfAnchor.None,
                slope: WorldGen.genRand.NextFloat(-0.20f, 0.20f),
                addStem: true));

            shelves.Add(CreateAnchoredShelf(
                WorldGen.genRand.Next(2) == 0 ? ShelfAnchor.Left : ShelfAnchor.Right,
                minX, maxX,
                LerpI(startY, endY, 0.90f) + WorldGen.genRand.Next(-3, 4),
                width / 10 + WorldGen.genRand.Next(4, 10),
                WorldGen.genRand.Next(8, 13),
                WorldGen.genRand.NextFloat(-0.18f, 0.18f),
                addStem: true));

            return shelves;
        }

        private static ShelfSeed CreateAnchoredShelf(ShelfAnchor anchor, int minX, int maxX, int centerY, int halfWidth, int halfHeight, float slope, bool addStem)
        {
            int overlap = 50 + WorldGen.genRand.Next(5);

            int centerX = anchor switch
            {
                ShelfAnchor.Left => minX + halfWidth - overlap,
                ShelfAnchor.Right => maxX - halfWidth + overlap,
                _ => (minX + maxX) / 2
            };

            return new ShelfSeed(centerX, centerY, halfWidth, halfHeight, anchor, slope, addStem);
        }

        private static void StampShelf(ShelfSeed seed, bool major)
        {

            //tends to place shelf halfway out of bounds.
            int left = seed.CenterX - seed.HalfWidth;
            int right = seed.CenterX + seed.HalfWidth;

            int lobeCount = Math.Max(3, seed.HalfWidth / 14);

            for (int i = 0; i < lobeCount; i++)
            {
                float t = lobeCount <= 1 ? 0.5f : i / (float)(lobeCount - 1);
                int x = LerpI(left, right, t) + WorldGen.genRand.Next(-3, 4);

                float arch = MathF.Sin(t * MathF.PI) * 0.652f;
                float sloped = MathHelper.Lerp(-seed.Slope, seed.Slope, t);

                int y = seed.CenterY + (int)((arch + sloped) * seed.HalfHeight * 0.45f) + WorldGen.genRand.Next(-2, 3);
                int rx = (int)(Math.Max(8, seed.HalfWidth / Math.Max(2, lobeCount - 1) + WorldGen.genRand.Next(5, 10))*0.5f);
                int ry = Math.Max(6, seed.HalfHeight + WorldGen.genRand.Next(-2, 3));

                AbyssWorldGenHelper.FillBlob(x, y, rx, ry, MantleGravelType, 0.9f, false);
            }

            int topY = seed.CenterY - seed.HalfHeight / 3;
            for (int x = left + 6; x <= right - 6; x += 10)
            {
                AbyssWorldGenHelper.FillBlob(
                    x + WorldGen.genRand.Next(-1, 2),
                    topY ,
                    8,
                    Math.Max(4, seed.HalfHeight / 2),
                    PyreMantleType,
                    0.6f,
                    false);
            }

            if (seed.AddStem)
                AddStem(seed);

            //todo: seed anchors seem to place outside of the bounds of the abyss?
            switch (seed.Anchor)
            {
                case ShelfAnchor.Left:
                    AbyssWorldGenHelper.FillBlob(left - 6, seed.CenterY, 12, seed.HalfHeight + 3, TileID.LivingWood, 0.86f, false);
                    break;

                case ShelfAnchor.Right:
                    AbyssWorldGenHelper.FillBlob(right + 6, seed.CenterY, 12, seed.HalfHeight + 3, TileID.LesionBlock, 1f, false);
                    break;
            }


            int scoopCount = major ? Math.Max(4, seed.HalfWidth / 18) : 1;

            for (int i = 0; i < scoopCount; i++)
            {
                float t = (i + 1f) / (scoopCount + 1f);
                int scoopX = LerpI(left + 6, right - 6, t) + WorldGen.genRand.Next(-4, 5);
                int scoopY = seed.CenterY + seed.HalfHeight / 2 + WorldGen.genRand.Next(3, 9);
                int scoopRx = WorldGen.genRand.Next(7, Math.Max(10, seed.HalfWidth / 4 + 8));
                int scoopRy = WorldGen.genRand.Next(4, Math.Max(6, seed.HalfHeight / 2 + 5));

                //disabled for now.
                AbyssWorldGenHelper.CarveBlob(scoopX, scoopY, scoopRx, scoopRy, 0.90f, true);
            }
        }

        private static void AddStem(ShelfSeed seed)
        {
            int stemX = seed.CenterX + WorldGen.genRand.Next(-Math.Max(3, seed.HalfWidth / 4), Math.Max(4, seed.HalfWidth / 4 + 1));
            int stemStartY = seed.CenterY + seed.HalfHeight / 2;
            int stemLength = WorldGen.genRand.Next(seed.HalfHeight + 10, seed.HalfHeight + 24);
            int blobCount = Math.Max(6, stemLength / 2);

            for (int i = 0; i < blobCount; i++)
            {
                int y = stemStartY + i * 6;
                int Height = WorldGen.genRand.Next(9, 40);
                AbyssWorldGenHelper.FillBlob(
                    stemX + WorldGen.genRand.Next(-2, 3),
                    y+Height,
                    WorldGen.genRand.Next(4, 7),
                    Height,
                    BaroRoot,
                    0.94f,
                    false);
            }
        }

        private static void AddMinorShelves(int minX, int maxX, int startY, int endY, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ShelfAnchor anchor = WorldGen.genRand.Next(4) switch
                {
                    0 => ShelfAnchor.Left,
                    1 => ShelfAnchor.Right,
                    _ => ShelfAnchor.None
                };

                int halfWidth = WorldGen.genRand.Next(12, 24);
                int halfHeight = WorldGen.genRand.Next(6, 11);

                int centerX = anchor switch
                {
                    ShelfAnchor.Left => minX + halfWidth - WorldGen.genRand.Next(5, 10),
                    ShelfAnchor.Right => maxX - halfWidth + WorldGen.genRand.Next(5, 10),
                    _ => WorldGen.genRand.Next(minX + halfWidth + 12, maxX - halfWidth - 12)
                };

                int centerY = WorldGen.genRand.Next(startY + 16, endY - 16);

                StampShelf(
                    new ShelfSeed(
                        centerX,
                        centerY,
                        halfWidth,
                        halfHeight,
                        anchor,
                        WorldGen.genRand.NextFloat(-0.25f, 0.25f),
                        addStem: false),
                    major: false);
            }
        }

        private static void CarveMainRoute(int minX, int maxX, int startY, int endY)
        {
            int width = maxX - minX;

            int startX = Utils.Clamp(AbyssGenUtils.ChasmX + WorldGen.genRand.Next(-24, 25), minX + 30, maxX - 30);

            Vector2[] nodes =
            {
                new(startX, startY + 8f),
                new(Utils.Clamp(minX + width / 3 + WorldGen.genRand.Next(-20, 21), minX + 25, maxX - 25), LerpI(startY, endY, 0.22f)),
                new(Utils.Clamp(minX + width / 2 + WorldGen.genRand.Next(-34, 35), minX + 25, maxX - 25), LerpI(startY, endY, 0.46f)),
                new(Utils.Clamp(minX + (width * 2) / 3 + WorldGen.genRand.Next(-20, 21), minX + 25, maxX - 25), LerpI(startY, endY, 0.70f)),
                new(Utils.Clamp(startX + WorldGen.genRand.Next(-40, 41), minX + 28, maxX - 28), endY - 8f),
            };

            for (int i = 0; i < nodes.Length - 1; i++)
            {
                int rx = 4 + WorldGen.genRand.Next(10);
                int ry = 4 + WorldGen.genRand.Next(7);

                AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                    nodes[i],
                    nodes[i + 1],
                    rx,
                    ry,
                    irregularity: 0.9f,
                    fillWithWater: true,
                    sampleSpacingFactor: 0.28f,
                    wanderStrength: 0.18f,
                    radiusJitterFactor: 0.10f);
            }

            foreach (Vector2 node in nodes)
            {
                AbyssWorldGenHelper.CarveBlob(
                    (int)node.X + WorldGen.genRand.Next(-10, 11),
                    (int)node.Y,
                    WorldGen.genRand.Next(5, 12),
                    WorldGen.genRand.Next(8, 40),
                    0.96f,
                    true);
            }
        }

        private static void CarveShelfPockets(ShelfSeed seed)
        {
            int left = seed.CenterX - seed.HalfWidth;
            int right = seed.CenterX + seed.HalfWidth;

            int pocketCount = seed.HalfWidth >= 32 ? 2 : 1;

            for (int i = 0; i < pocketCount; i++)
            {
                float t = (i + 1f) / (pocketCount + 1f);
                int x = LerpI(left + 8, right - 8, t) + WorldGen.genRand.Next(-5, 6);
                int y = seed.CenterY + seed.HalfHeight / 2 + WorldGen.genRand.Next(6, 14);

                AbyssWorldGenHelper.CarveBlob(
                    x,
                    y,
                    WorldGen.genRand.Next(8, Math.Max(11, seed.HalfWidth / 4 + 9)),
                    WorldGen.genRand.Next(5, Math.Max(7, seed.HalfHeight / 2 + 6)),
                    0.18f,
                    true);
            }

            if (WorldGen.genRand.Next(2) == 0)
            {
                int dir = WorldGen.genRand.Next(2) == 0 ? -1 : 1;
                Vector2 start = new(seed.CenterX + dir * seed.HalfWidth / 4f, seed.CenterY + WorldGen.genRand.Next(-2, 3));
                Vector2 end = new(seed.CenterX + dir * (seed.HalfWidth + 20), seed.CenterY + WorldGen.genRand.Next(-8, 9));

                AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                    start,
                    end,
                    radiusX: 10,
                    radiusY: 7,
                    irregularity: 0.15f,
                    fillWithWater: true,
                    sampleSpacingFactor: 0.34f,
                    wanderStrength: 0.12f,
                    radiusJitterFactor: 0.08f);
            }
        }

        private static void CarveExtraGapChambers(int minX, int maxX, int startY, int endY, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = WorldGen.genRand.Next(minX + 24, maxX - 24);
                int y = WorldGen.genRand.Next(startY + 18, endY - 18);

                AbyssWorldGenHelper.CarveBlob(
                    x,
                    y,
                    WorldGen.genRand.Next(12, 24),
                    WorldGen.genRand.Next(8, 15),
                    1f,
                    true);
            }
        }

        private static void ApplyMaterialVariation(int minX, int maxX, int startY, int endY)
        {
            int height = Math.Max(1, endY - startY);

            for (int x = minX + 1; x < maxX - 1; x++)
            {
                for (int y = startY + 1; y < endY - 1; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                        continue;

                    float localT = (y - startY) / (float)height;
                    float noiseA = AbyssWorldGenHelper.FractalNoise(x * 0.055f, y * 0.055f, 3);
                    float noiseB = AbyssWorldGenHelper.FractalNoise(x * 0.12f + 120f, y * 0.12f - 30f, 2);

                    ushort type = (ushort)TenebrisType;

                    if (localT > 0.92f && noiseA > 0.84f && noiseB > 0.72f)
                        type = (ushort)MoltenPyreMantleType;
                    else if (localT > 0.72f && noiseB > 0.66f)
                        type = (ushort)MantleGravelType;
                    else if (localT > 0.56f && noiseA > 0.73f)
                        type = (ushort)PyreMantleType;

                    if (IsExposedToOpenWater(x, y) && noiseA > 0.82f)
                        type = (ushort)BrineCrystalTile;

                    tile.TileType = type;
                }
            }
        }

        private static bool IsExposedToOpenWater(int x, int y)
        {
            return !Main.tile[x - 1, y].HasTile ||
                   !Main.tile[x + 1, y].HasTile ||
                   !Main.tile[x, y - 1].HasTile ||
                   !Main.tile[x, y + 1].HasTile;
        }
        /// <summary>
        /// the most pointless thing ever.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static int LerpI(int a, int b, float t) => (int)MathHelper.Lerp(a, b, t);
    }
}
using AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles;
using AbyssOverhaul.Content.Layers.TheVeil.NPCs.VoidDreamerNPC;
using AbyssOverhaul.Content.NPCs.DeepSnapperNPC;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.SunkenSea;
using CalamityMod.Waters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

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
            [ModContent.NPCType<GulperEelHead>()] =0.2f,
            [ModContent.NPCType<Laserfish>()] = 0.9f,
            [ModContent.NPCType<OarfishHead>()] = 0.7f,
            [ModContent.NPCType<Viperfish>()] = 0.8f,
            [ModContent.NPCType<VoidDreamer>()] = 0.02f,
            [ModContent.NPCType<DeepSnapper>()] = 1.1f,

        };

        private static int PyreMantleType => ModContent.TileType<MantleGravel_Tile>(); 
        private static int MoltenPyreMantleType => ModContent.TileType<PyreMantleMolten>();
        private static int MantleGravelType => ModContent.TileType<PyreMantle>();
        private static int TenebrisType => ModContent.TileType<Tenebris_Tile>();

        public override void ModifyGenTasks()
        {
            AddGenTask("Tenebrous Marsh", GenerateMarshLayer);
        }

        private static void GenerateMarshLayer(AbyssLayer layer, GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Drowning the abyss in marsh growth";

            AbyssRegion region = new(AbyssGenUtils.MinX, AbyssGenUtils.MaxX, layer.StartY, layer.EndY);

            int minX = region.MinX + 10;
            int maxX = region.MaxX - 10;
            int startY = region.StartY;
            int endY = region.EndY;
            int width = maxX - minX;
            int height = endY - startY;
            int centerX = (minX + maxX) / 2;

            // 1) Fill the whole layer with mantle first.
            AbyssWorldGenHelper.ForceSolidRect(minX, maxX, startY, endY, PyreMantleType, true);

            CarveMainWaterBody(minX, maxX, startY, endY, centerX, width, height);


            BuildBrokenShelves(minX, maxX, startY, endY, centerX, width, height);

            BuildSideMasses(minX, maxX, startY, endY, width);

            BuildMainCentralPlatform(minX, maxX, startY, endY, centerX, width, height);

            AddSiltBands(minX, maxX, startY, endY, width, height);

            // 6) Grow Tenebris patches onto floors, undersides, and walls.
            AddTenebrisGrowth(minX, maxX, startY, endY);
            AddBrineCrystals(minX, maxX, startY, endY);
            // 7) Add a few brighter molten/shallow pockets near lower cavity lips.
            AddMoltenPockets(minX, maxX, startY, endY, width, height);

            // 8) Cleanup.
            for(int i = 0; i< 4; i++)
            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, startY, endY, maxNeighbors: 2, chanceDenominator: 3, fillWithWater: true);
            AbyssWorldGenHelper.FloodOpenSpace(minX, maxX, startY, endY);
            AbyssWorldGenHelper.ReframeArea(minX, maxX, startY, endY);
        }

        private static void BuildMainCentralPlatform(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            AbyssWorldGenHelper.FillBlob(centerX, startY + 15, width/4, height/8, PyreMantleType, 1.2f);

            bool Flip = Terraria.WorldGen.genRand.NextBool();

            Vector2 start = new Vector2(centerX, startY-15);
            Vector2 end = new Vector2(centerX + width / 24 * (Flip? -1:1), startY+15);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start, end, 5, 4, -1);



            Vector2 start2 = end;
            Vector2 end2 = end + new Vector2(width / 16 * (!Flip ? -1 : 1), + 35);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start2, end2, 2, 4,0, true, wanderStrength: 2);
        }

        private static void CarveMainWaterBody(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            int samples = 18;
            Vector2 previous = Vector2.Zero;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);

                int y = (int)MathHelper.Lerp(startY + 10, endY - 10, t);

                // Much gentler meander so the space stays readable.
                float meander =
                    MathF.Sin(t * MathHelper.TwoPi * 1.1f + 0.7f) * width * 0.035f +
                    (AbyssWorldGenHelper.FractalNoise(100f + i * 0.21f, 33f, 2) - 0.5f) * width * 0.04f;

                int x = centerX + (int)meander;

                // Wide enough to be traversable, but not so wide it deletes shelf identity.
                int rx = (int)MathHelper.Lerp(width * 0.11f, width * 0.16f, 1f - MathF.Abs(t - 0.5f) * 2f);
                int ry = Terraria.WorldGen.genRand.Next(10, 16);

                Vector2 current = new(x, y);

                AbyssWorldGenHelper.CarveBlob(x, y, rx, ry, 0.12f, true);

                if (i > 0)
                    AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(previous, current, rx, ry - 2, 0.08f, true);

                previous = current;
            }
        }
        private static void BuildBrokenShelves(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            int shelfCount =12;

            for (int i = 0; i < shelfCount; i++)
            {
                float t = (i + 0.6f) / shelfCount;
                int y = (int)MathHelper.Lerp(startY + 10, endY - 12, t);

                int thickness = Terraria.WorldGen.genRand.Next(8, 16);
                int leftX = minX + Terraria.WorldGen.genRand.Next(20, 52);
                int rightX = maxX - Terraria.WorldGen.genRand.Next(20, 52);

                int gapCenter = centerX + Terraria.WorldGen.genRand.Next(-width / 8, width / 8);
                int gapHalfWidth = Terraria.WorldGen.genRand.Next(18, 42);

                // Left shelf segment bounds
                int leftSegMin = leftX;
                int leftSegMax = gapCenter - gapHalfWidth;

                if (leftSegMax - leftSegMin > 8)
                {
                    int segCenterX = (leftSegMin + leftSegMax) / 2;
                    int segRadiusX = (leftSegMax - leftSegMin) / 2;
                    int segRadiusY = Math.Max(2, thickness / 2);

                    AbyssWorldGenHelper.FillBlob(segCenterX, y, segRadiusX, segRadiusY, PyreMantleType, 1f, true);
                    AbyssWorldGenHelper.FillBlobReplace(segCenterX, y, segRadiusX, segRadiusY, MantleGravelType, 4f, PyreMantleType);
                }

                // Right shelf segment bounds
                int rightSegMin = gapCenter + gapHalfWidth;
                int rightSegMax = rightX;

                if (rightSegMax - rightSegMin > 8)
                {
                    int segCenterX = (rightSegMin + rightSegMax) / 2;
                    int segRadiusX = (rightSegMax - rightSegMin) / 2;
                    int segRadiusY = Math.Max(2, thickness / 2);

                    AbyssWorldGenHelper.FillBlob(segCenterX, y, segRadiusX, segRadiusY, PyreMantleType, 1, true);

                    AbyssWorldGenHelper.FillBlobReplace(segCenterX, y, segRadiusX, segRadiusY, MantleGravelType, 4f, PyreMantleType);
                }

                int lumpCount = Terraria.WorldGen.genRand.Next(2, 12);
                for (int j = 0; j < lumpCount; j++)
                {
                    int lx = Terraria.WorldGen.genRand.Next(leftX, rightX);
                    int ly = y + (Terraria.WorldGen.genRand.NextBool()
                        ? Terraria.WorldGen.genRand.Next(-10, -2)
                        : Terraria.WorldGen.genRand.Next(2, 10));

                    int rx = Terraria.WorldGen.genRand.Next(10, 20);
                    int ry = Terraria.WorldGen.genRand.Next(4, 9);

                    AbyssWorldGenHelper.FillBlob(lx, ly, rx, ry, PyreMantleType, 1f, true);
                }
            }
        }

        private static void BuildSideMasses(int minX, int maxX, int startY, int endY, int width)
        {
            int sideWidth = Math.Max(48, width / 14);

            // Left side pillar.
            AbyssWorldGenHelper.ForceSolidRect(minX, minX + sideWidth, startY, endY, PyreMantleType, true);

            // Right side pillar.
            AbyssWorldGenHelper.ForceSolidRect(maxX - sideWidth, maxX, startY, endY, PyreMantleType, true);


            for (int i = 0; i < 8; i++)
            {
                int y = Terraria.WorldGen.genRand.Next(startY + 10, endY - 29);
                int ry = Terraria.WorldGen.genRand.Next(8, 18);
                int rx = Terraria.WorldGen.genRand.Next(8, 16);




                if (WorldGen.genRand.NextBool())
                {
                    AbyssWorldGenHelper.CarveBlob(minX + sideWidth, y, rx, ry, 1f, true);

                    Vector2 PocketLoc = new Vector2(minX + sideWidth, y);
                    Vector2 PocketEndLoc = new Vector2(AbyssGenUtils.ChasmX, y);
                    AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(PocketLoc, PocketEndLoc, 4, 4, 1);
                }

                if (WorldGen.genRand.NextBool())
                {
                    AbyssWorldGenHelper.CarveBlob(maxX - sideWidth, y, rx, ry, 1f, true);
                    Vector2 PocketLoc = new Vector2(maxX - sideWidth, y);
                    Vector2 PocketEndLoc = new Vector2(AbyssGenUtils.ChasmX, y);
                    AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(PocketLoc, PocketEndLoc, 4, 4, 1);

                  

                    // AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(new Vector2(minX+ sideWidth, y), new Vector2(minX + 60, y), rx/4, ry/4, -1.2f);
                }


                //A
            }
        }

        private static void AddSiltBands(int minX, int maxX, int startY, int endY, int width, int height)
        {
            
        }

        private static void AddTenebrisGrowth(int minX, int maxX, int startY, int endY)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool swap = WorldGen.genRand.NextBool();
                for (int x = minX + 1; x < maxX - 1; x++)
                {
                    for (int y = startY + 1; y < endY - 1; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        if (!tile.HasTile)
                            continue;

                        ushort type = tile.TileType;
                        if (type != PyreMantleType && type != MantleGravelType)
                            continue;

                        bool exposedAbove = !Framing.GetTileSafely(x, y - 1).HasTile;
                        bool exposedBelow = !Framing.GetTileSafely(x, y + 1).HasTile;
                        bool exposedLeft = !Framing.GetTileSafely(x - 1, y).HasTile;
                        bool exposedRight = !Framing.GetTileSafely(x + 1, y).HasTile;

                        bool candidate =
                            exposedAbove ||
                            exposedBelow ||
                            (exposedLeft && Terraria.WorldGen.genRand.NextBool(2)) ||
                            (exposedRight && Terraria.WorldGen.genRand.NextBool(2));

                        if (!candidate)
                            continue;

                        float growthChance = 0.06f;

                        if (exposedAbove)
                            growthChance += 0.10f;
                        if (exposedBelow)
                            growthChance += 0.05f;

                        if (Terraria.WorldGen.genRand.NextFloat() < growthChance)
                        {
                            tile.TileType = (ushort)(!swap ? TenebrisType : ModContent.TileType<PlantyMush>());

                            if (Terraria.WorldGen.genRand.NextBool(3))
                            {
                                int rx = Terraria.WorldGen.genRand.Next(4, 10);
                                int ry = Terraria.WorldGen.genRand.Next(2, 5);
                                AbyssWorldGenHelper.FillBlobReplace(x, y, rx, ry, (ushort)(!swap ? TenebrisType : ModContent.TileType<PlantyMush>()),0.35f, PyreMantleType, MantleGravelType);
                            }
                        }
                    }
                }
            }
        }
        private static void AddBrineCrystals(int minX, int maxX, int startY, int endY)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool swap = WorldGen.genRand.NextBool();
                for (int x = minX + 1; x < maxX - 1; x++)
                {
                    for (int y = startY + 1; y < endY - 1; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        if (!tile.HasTile)
                            continue;

                        ushort type = tile.TileType;
                        if (type != PyreMantleType && type != MantleGravelType)
                            continue;

                        bool exposedAbove = !Framing.GetTileSafely(x, y - 1).HasTile;
                        bool exposedBelow = !Framing.GetTileSafely(x, y + 1).HasTile;
                        bool exposedLeft = !Framing.GetTileSafely(x - 1, y).HasTile;
                        bool exposedRight = !Framing.GetTileSafely(x + 1, y).HasTile;

                        bool candidate =
                            exposedAbove ||
                            exposedBelow ||
                            (exposedLeft && Terraria.WorldGen.genRand.NextBool(2)) ||
                            (exposedRight && Terraria.WorldGen.genRand.NextBool(2));

                        if (!candidate)
                            continue;

                        float growthChance = 0.03f;

                        if (exposedAbove)
                            growthChance += 0.10f;
                        if (exposedBelow)
                            growthChance += 0.05f;

                        if (Terraria.WorldGen.genRand.NextFloat() < growthChance)
                        {
                            tile.TileType = (ushort)(!swap ? TenebrisType : ModContent.TileType<SeaPrism>());

                            if (Terraria.WorldGen.genRand.NextBool(3))
                            {
                                int rx = Terraria.WorldGen.genRand.Next(4, 10);
                                int ry = Terraria.WorldGen.genRand.Next(2, 5);
                                AbyssWorldGenHelper.FillBlobReplace(x, y, rx, ry, (ushort)(!swap ? TenebrisType : ModContent.TileType<SeaPrism>()), 0.35f, PyreMantleType, MantleGravelType);
                            }
                        }
                    }
                }
            }
        }

        private static void AddMoltenPockets(int minX, int maxX, int startY, int endY, int width, int height)
        {
            int pocketCount =19;

            for (int i = 0; i < pocketCount; i++)
            {
                int x = Terraria.WorldGen.genRand.Next(minX + width / 8, maxX - width / 8);
                int y = Terraria.WorldGen.genRand.Next(startY + height / 3, endY - 8);

                // Try to place these near lower lips/floors of cavities.
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    Tile here = Framing.GetTileSafely(x, y);
                    Tile above = Framing.GetTileSafely(x, y - 1);

                    if (here.HasTile && !above.HasTile)
                        break;

                    x = Terraria.WorldGen.genRand.Next(minX + width / 8, maxX - width / 8);
                    y = Terraria.WorldGen.genRand.Next(startY + height / 3, endY - 8);
                }

                int rx = Terraria.WorldGen.genRand.Next(3,23);
                int ry = Terraria.WorldGen.genRand.Next(3, 7);

                AbyssWorldGenHelper.FillBlobReplace(x, y, rx, ry, MoltenPyreMantleType, 0.25f, PyreMantleType, MantleGravelType);
            }
        }
    }

}


using AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles;
using AbyssOverhaul.Content.NPCs.Critters.VoidDreamerNPC;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
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

namespace AbyssOverhaul.Content.Layers.ThermalVents
{
    internal class ThermalVentsLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(0.45f);
        public override int EndHeight => AbyssGenUtils.YAt(0.6f);

        public static ThermalVentsLayer Instance => ModContent.GetInstance<ThermalVentsLayer>();
        public override ModWaterStyle ModWaterStyle => ThermalVentsWater.Instance;

        private static int PyreMantleType => ModContent.TileType<PyreMantle>();
        private static int MoltenPyreMantleType => ModContent.TileType<PyreMantleMolten>();
        private static int ScoriaType => ModContent.TileType<ScoriaOre>();
        private static int MantleGravelType => ModContent.TileType<MantleGravel_Tile>();
        private static int MineralSlagType => TileID.SolarBrick;

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

            // 1) Fill the entire layer with pyre mantle / molten pyre mantle.
            FillBaseMass(minX, maxX, startY, endY);

            // 2) Carve the main open chamber first.
            CarveMainVentChamber(minX, maxX, startY, endY);

            // 3) Rebuild large shelves / islands into the chamber.
            BuildPrimaryMasses(minX, maxX, startY, endY, centerX, width, height);

            // 4) Carve corridors so the open area stays connected.
            CarveInnerChannels(minX, maxX, startY, endY, centerX, width, height);

            // 5) Ensure the player can actually enter the layer cleanly.
            GuaranteeEntrance(centerX, startY, width);

            // 6) Paint hot materials along exposed interior faces.
            PlaceScoriaPockets(minX, maxX, startY, endY);
            PlaceMineralSlagPockets(minX, maxX, startY, endY);

            // 7) Add gravel in some lower exposed ledges / basin areas.
            AddMantleGravel(minX, maxX, startY, endY);

            // 8) Light cleanup / shaping.
            ErodeEdges(minX, maxX, startY, endY, PyreMantleType, 16);
            ErodeEdges(minX, maxX, startY, endY, MoltenPyreMantleType, 20);
            ErodeEdges(minX, maxX, startY, endY, ScoriaType, 24);

            BuildSideWall(minX, startY, endY, true);
            BuildSideWall(maxX, startY, endY, false);

            GarenteeExit(centerX, endY, width);

            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, startY, endY, 2, 3, true);
            AbyssWorldGenHelper.ReframeArea(minX, maxX, startY, endY);

        }

        private static void GarenteeExit(int centerX, int endY, int width)
        {
            Vector2 start = new Vector2(centerX, endY - 8);
            Vector2 end = start + Vector2.UnitY * 34f;
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start, end, 6, 10, 0.08f, true);

            Vector2 lowerTurn = end + new Vector2(-width * 0.08f, 34f);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(end, lowerTurn, 5, 5, 0.10f, true);
        }

        private static void FillBaseMass(int minX, int maxX, int startY, int endY)
        {
            UnifiedRandom rand = WorldGen.genRand;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    float heatNoise = AbyssWorldGenHelper.FractalNoise(x * 0.028f, y * 0.028f, 3, 1f, 1f);

                    int tileType = heatNoise > 0.62f && rand.NextBool(3)
                        ? MoltenPyreMantleType
                        : PyreMantleType;

                    AbyssWorldGenHelper.PlaceSolidTile(x, y, tileType, true);
                }
            }
        }

        private static void CarveMainVentChamber(int minX, int maxX, int startY, int endY)
        {
            int width = maxX - minX;
            int height = endY - startY;

            for (int x = minX + 8; x <= maxX - 8; x++)
            {
                float t = (x - minX) / (float)Math.Max(1, width);

                float topWave =
                    (float)Math.Sin(t * MathHelper.TwoPi * 0.9f + 0.4f) * 8f +
                    (float)Math.Sin(t * MathHelper.TwoPi * 2.1f + 1.7f) * 5f;

                float bottomWave =
                    (float)Math.Sin(t * MathHelper.TwoPi * 1.1f + 2.4f) * 10f +
                    (float)Math.Sin(t * MathHelper.TwoPi * 1.9f + 0.8f) * 6f;

                int localTop = startY + height / 10 + (int)topWave;
                int localBottom = endY - height / 12 + (int)bottomWave;

                // Wider in the middle, tighter toward the sides.
                int sideInset = (int)(Math.Abs(t - 0.5f) * 34f);
                localTop += sideInset / 2;
                localBottom -= sideInset;

                if (localBottom <= localTop + 20)
                    localBottom = localTop + 20;

                for (int y = localTop; y <= localBottom; y++)
                    AbyssWorldGenHelper.ClearTile(x, y, true);
            }

            // A few large carved pockets to break up the chamber silhouette.
            AbyssWorldGenHelper.CarveBlob(
                minX + width / 2,
                startY + height / 2,
                width / 6,
                height / 7,
                0.24f,
                true);

            AbyssWorldGenHelper.CarveBlob(
                minX + width / 4,
                startY + height / 3,
                width / 10,
                height / 9,
                0.20f,
                true);

            AbyssWorldGenHelper.CarveBlob(
                maxX - width / 5,
                startY + height / 3,
                width / 11,
                height / 10,
                0.20f,
                true);

            AbyssWorldGenHelper.CarveBlob(
                maxX - width / 6,
                endY - height / 4,
                width / 9,
                height / 8,
                0.18f,
                true);
        }

        private static void BuildPrimaryMasses(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            // Left upper hanging shelf
            PlaceNoisyBlobMixed(
                centerX - width / 4,
                startY + height / 5,
                width / 7,
                height / 8,
                0.18f);

            // Upper middle shelf
            PlaceNoisyBlobMixed(
                centerX + width / 18,
                startY + height / 4,
                width / 6,
                height / 9,
                0.18f);

            // Right upper shelf
            PlaceNoisyBlobMixed(
                centerX + width / 4,
                startY + height / 4,
                width / 7,
                height / 8,
                0.18f);

            // Mid central hanging shelf
            PlaceNoisyBlobMixed(
                centerX + width / 12,
                startY + height / 2,
                width / 5,
                height / 10,
                0.20f);

            // Lower left basin rise
            PlaceNoisyBlobMixed(
                centerX - width / 5,
                endY - height / 5,
                width / 4,
                height / 7,
                0.20f);

            // Lower right rise
            PlaceNoisyBlobMixed(
                centerX + width / 4,
                endY - height / 6,
                width / 5,
                height / 7,
                0.20f);

            // Smaller floating fragments
            PlaceNoisyBlobMixed(
                centerX - width / 9,
                startY + height * 3 / 8,
                width / 12,
                height / 12,
                0.14f);

            PlaceNoisyBlobMixed(
                centerX + width / 3,
                startY + height * 5 / 8,
                width / 13,
                height / 11,
                0.14f);

            PlaceNoisyBlobMixed(
                centerX - width / 3,
                endY - height / 3,
                width / 14,
                height / 12,
                0.14f);
        }

        private static void CarveInnerChannels(int minX, int maxX, int startY, int endY, int centerX, int width, int height)
        {
            // Top horizontal corridor
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(minX + width * 0.12f, startY + height * 0.24f),
                new Vector2(maxX - width * 0.12f, startY + height * 0.28f),
                7, 7, 0.25f, true);

            // Middle corridor
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(minX + width * 0.10f, startY + height * 0.48f),
                new Vector2(maxX - width * 0.08f, startY + height * 0.45f),
                9, 8, 0.25f, true);

            // Lower corridor
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(minX + width * 0.18f, startY + height * 0.74f),
                new Vector2(maxX - width * 0.12f, startY + height * 0.70f),
                8, 7, 0.22f, true);

            // Vertical-ish connections
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(centerX - width * 0.08f, startY + height * 0.26f),
                new Vector2(centerX - width * 0.03f, startY + height * 0.63f),
                5, 7, 0.18f, true);

            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(centerX + width * 0.18f, startY + height * 0.38f),
                new Vector2(centerX + width * 0.12f, startY + height * 0.76f),
                5, 7, 0.18f, true);
        }

        private static void GuaranteeEntrance(int centerX, int startY, int width)
        {
            Vector2 start = new Vector2(centerX, startY - 8);
            Vector2 end = start + Vector2.UnitY * 34f;
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start, end, 6, 10, 0.08f, true);

            Vector2 lowerTurn = end + new Vector2(-width * 0.08f, 34f);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(end, lowerTurn, 5, 5, 0.10f, true);
        }

        private static void PlaceNoisyBlobMixed(int centerX, int centerY, int radiusX, int radiusY, float noiseStrength)
        {
            UnifiedRandom rand = WorldGen.genRand;
            float phaseA = rand.NextFloat(MathHelper.TwoPi);
            float phaseB = rand.NextFloat(MathHelper.TwoPi);

            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                {
                    if (!WorldGen.InWorld(x, y, 10))
                        continue;

                    float dx = (x - centerX) / (float)Math.Max(1, radiusX);
                    float dy = (y - centerY) / (float)Math.Max(1, radiusY);
                    float dist = dx * dx + dy * dy;

                    float angle = (float)Math.Atan2(dy, dx);
                    float radialNoise =
                        (float)Math.Sin(angle * 3f + phaseA) * noiseStrength +
                        (float)Math.Sin(angle * 5f + phaseB) * (noiseStrength * 0.5f);

                    if (dist > 1f + radialNoise)
                        continue;

                    float heatNoise = AbyssWorldGenHelper.FractalNoise(x * 0.05f, y * 0.05f, 3, 1f, 1f);
                    int type = heatNoise > 0.64f && rand.NextBool(2)
                        ? MoltenPyreMantleType
                        : PyreMantleType;

                    AbyssWorldGenHelper.PlaceSolidTile(x, y, type, true);
                }
            }
        }

        private static void PlaceScoriaPockets(int minX, int maxX, int minY, int maxY)
        {
            for (int x = minX + 2; x < maxX - 2; x++)
            {
                for (int y = minY + 2; y < maxY - 2; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile)
                        continue;

                    if (tile.TileType != PyreMantleType && tile.TileType != MoltenPyreMantleType)
                        continue;

                    bool openUp = !Framing.GetTileSafely(x, y - 1).HasTile;
                    bool openLeft = !Framing.GetTileSafely(x - 1, y).HasTile;
                    bool openRight = !Framing.GetTileSafely(x + 1, y).HasTile;
                    bool openDown = !Framing.GetTileSafely(x, y + 1).HasTile;

                    int exposure = 0;
                    if (openUp) exposure += 2;
                    if (openLeft) exposure++;
                    if (openRight) exposure++;
                    if (openDown) exposure++;

                    float heat = AbyssWorldGenHelper.FractalNoise(x * 0.07f, y * 0.07f, 3, 1f, 1f);

                    if (exposure >= 2 && heat > 0.58f && WorldGen.genRand.NextBool(3))
                    {
                        int rx = WorldGen.genRand.Next(3, 8);
                        int ry = WorldGen.genRand.Next(2, 6);
                        AbyssWorldGenHelper.FillBlobReplace(
                            x, y, rx, ry, ScoriaType, 0.18f,
                            PyreMantleType, MoltenPyreMantleType);
                    }
                }
            }
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
    }
}
using AbyssOverhaul.Content.Layers.ThermalVents;
using AbyssOverhaul.Content.Layers.TheVeil.Tiles;
using AbyssOverhaul.Content.NPCs.Critters.VoidDreamerNPC;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Waters;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace AbyssOverhaul.Content.Layers.TheVeil
{
    internal class TheVeilLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(0.6f);
        public override int EndHeight => AbyssGenUtils.YAt(0.8f);
        private static int SnowType => ModContent.TileType<marine_snow>();
        private static int MantleType => ModContent.TileType<VoidstoneMantle>();
        private static int VoidstoneType => ModContent.TileType<Voidstone>();
        public override ModWaterStyle ModWaterStyle => VoidWater.Instance;
        public override int MusicSlot => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/VeilOst");
        public override void ModifyGenTasks()
        {
            AddGenTask("TheVeil", static (_, progress, config) =>
            {
                GenerateVeil(progress);
            });
        }

        public override Dictionary<int, float> NPCSpawnPool => new()
        {
            
            [ModContent.NPCType<BobbitWormHead>()] = 0.2f,

            [ModContent.NPCType<GulperEelHead>()] = 0.4f,

            [ModContent.NPCType<EidolonWyrmHead>()] = 0.1f,
            [ModContent.NPCType<LuminousCorvina>()] = 1f,
            [ModContent.NPCType<Eidolist>()] = 0.1f,
            [ModContent.NPCType<ReaperShark>()] = 0.04f,
            [ModContent.NPCType<VoidDreamer>()] = 0.6f,
            [ModContent.NPCType<Bloatfish>()] = 0.9f,
        };

        private static void GenerateVeil(GenerationProgress progress)
        {
            progress.Message = "Shaping the Veil";

            TheVeilLayer instance = ModContent.GetInstance<TheVeilLayer>();
            int minX = AbyssGenUtils.MinX;
            int maxX = AbyssGenUtils.MaxX;
            int startY = instance.StartY;
            int endY = instance.EndY;

            int width = maxX - minX;
            int height = endY - startY;
            int centerX = (minX + maxX) / 2;

            UnifiedRandom rand = WorldGen.genRand;

            // World-specific shape knobs.
            float chamberTopAmpA = rand.NextFloat(5f, 11f);
            float chamberTopAmpB = rand.NextFloat(3f, 8f);
            float chamberBottomAmpA = rand.NextFloat(6f, 13f);
            float chamberBottomAmpB = rand.NextFloat(3f, 8f);

            float chamberTopFreqA = rand.NextFloat(0.65f, 1.25f);
            float chamberTopFreqB = rand.NextFloat(1.7f, 3.4f);
            float chamberBottomFreqA = rand.NextFloat(0.8f, 1.5f);
            float chamberBottomFreqB = rand.NextFloat(1.4f, 2.8f);

            float phaseA = rand.NextFloat(MathHelper.TwoPi);
            float phaseB = rand.NextFloat(MathHelper.TwoPi);
            float phaseC = rand.NextFloat(MathHelper.TwoPi);
            float phaseD = rand.NextFloat(MathHelper.TwoPi);

            int middleBias = rand.Next(-14, 15);
            int entryOffsetX = rand.Next(-55, 56);
            int entryDriftX = rand.NextBool() ? rand.Next(-60, -25) : rand.Next(25, 60);

            // Step 1: Fill with a mixture, not pure mantle.
            FillBaseVeilMaterial(minX, maxX, startY, endY);

            // Step 2: Carve the main chamber with randomized waveform parameters.
            CarveMainChamber(
                minX, maxX, startY, endY,
                chamberTopAmpA, chamberTopAmpB,
                chamberBottomAmpA, chamberBottomAmpB,
                chamberTopFreqA, chamberTopFreqB,
                chamberBottomFreqA, chamberBottomFreqB,
                phaseA, phaseB, phaseC, phaseD,
                middleBias);

            // Step 3: Rebuild major masses with randomized anchors and mixed materials.
            GenerateVeilMasses(centerX, minX, maxX, startY, endY, width, height);
            
            // Step 4: Carve main swim lanes with slight endpoint jitter.
            CarveLane(
                new Vector2(minX + width * 0.08f, startY + height * 0.48f),
                new Vector2(maxX - width * 0.10f, startY + height * 0.46f),
                8, 8, 0.45f, 26f,  true);

            CarveLane(
                new Vector2(minX + width * 0.18f, startY + height * 0.23f),
                new Vector2(maxX - width * 0.28f, startY + height * 0.30f),
                5, 5, 0.85f, 22f, true);

            CarveLane(
                new Vector2(minX + width * 0.28f, startY + height * 0.72f),
                new Vector2(maxX - width * 0.15f, startY + height * 0.66f),
                6, 6, 0.98f, 20f, true);

            CarveLane(
                new Vector2(centerX - width * 0.05f, startY + height * 0.30f),
                new Vector2(centerX - width * 0.02f, startY + height * 0.65f),
                5, 5, 0.18f, 14f, true);

            CarveLane(
                new Vector2(centerX + width * 0.22f, startY + height * 0.42f),
                new Vector2(centerX + width * 0.15f, startY + height * 0.70f),
                4, 4, 0.18f, 14f, true);

            // Optional extra lane sometimes.
            if (rand.NextBool())
            {
                CarveLane(
                    new Vector2(centerX + rand.Next(-80, 81), startY + height * rand.NextFloat(0.18f, 0.35f)),
                    new Vector2(centerX + rand.Next(-90, 91), startY + height * rand.NextFloat(0.58f, 0.82f)),
                    rand.Next(4, 7),
                    rand.Next(4, 7),
                    rand.NextFloat(0.12f, 0.4f),
                    rand.NextFloat(10f, 22f),
                    true);
            }
            
            GuaranteeEntrance(centerX + entryOffsetX, startY, entryDriftX);

            // Add clustered internal patches of normal voidstone after major shaping.
            ScatterVoidstonePatches(minX, maxX, startY, endY, width, height);

            // Surface dressing.
            CapExposedSurfacesWithMarineSnow(minX, maxX, startY, endY, 3);

            ErodeEdges(minX, maxX, startY, endY, MantleType, 14);
            ErodeEdges(minX, maxX, startY, endY, VoidstoneType, 16);
            ErodeEdges(minX, maxX, startY, endY, SnowType, 10);

            BuildSideWall(minX, startY, endY, true);
            BuildSideWall(maxX, startY, endY, false);

            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, startY, endY, 2, 3, true);
            AbyssWorldGenHelper.ReframeArea(minX, maxX, startY, endY);
        }

        private static void GarenteeEntrance(int centerX, int minX, int maxX, int width, int startY)
        {
            Vector2 start = new Vector2(centerX, startY - 10);
            Vector2 end = start + Vector2.UnitY * 30;
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start, end, 5, 10, 0);

            Vector2 NewEnd = end + new Vector2(-40, 45);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(end, NewEnd, 3, 3, 0);
        }
        private static void FillBaseVeilMaterial(int minX, int maxX, int startY, int endY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    float nLarge = AbyssWorldGenHelper.FractalNoise(x * 0.010f, y * 0.010f, 4);
                    float nFine = AbyssWorldGenHelper.FractalNoise(x * 0.035f, y * 0.035f, 3);
                    float blend = nLarge * 0.75f + nFine * 0.25f;

                    int tileType = blend > 0.57f ? MantleType : VoidstoneType;
                    AbyssWorldGenHelper.PlaceSolidTile(x, y, tileType, true);
                }
            }
        }
        private static void CarveMainChamber(
    int minX, int maxX, int startY, int endY,
    float topAmpA, float topAmpB,
    float bottomAmpA, float bottomAmpB,
    float topFreqA, float topFreqB,
    float bottomFreqA, float bottomFreqB,
    float phaseA, float phaseB, float phaseC, float phaseD,
    int middleBias)
        {
            int width = maxX - minX;
            int height = endY - startY;

            for (int x = minX + 6; x <= maxX - 6; x++)
            {
                float t = (x - minX) / (float)Math.Max(1, width);

                float topWave =
                    MathF.Sin(t * MathHelper.TwoPi * topFreqA + phaseA) * topAmpA +
                    MathF.Sin(t * MathHelper.TwoPi * topFreqB + phaseB) * topAmpB;

                float bottomWave =
                    MathF.Sin(t * MathHelper.TwoPi * bottomFreqA + phaseC) * bottomAmpA +
                    MathF.Sin(t * MathHelper.TwoPi * bottomFreqB + phaseD) * bottomAmpB;

                float waist = MathF.Abs(t - 0.5f);
                int inset = (int)(waist * (18f + middleBias));

                int localTop = startY + height / 8 + (int)topWave + inset;
                int localBottom = endY - height / 8 + (int)bottomWave - inset / 2;

                // Add a slow noise-based vertical wobble so it is not "just sine".
                float noiseShift = (AbyssWorldGenHelper.FractalNoise(x * 0.025f, startY * 0.01f, 3) - 0.5f) * 18f;
                localTop += (int)noiseShift;
                localBottom += (int)(noiseShift * 0.7f);

                if (localBottom <= localTop + 14)
                    localBottom = localTop + 14;

                for (int y = localTop; y <= localBottom; y++)
                    AbyssWorldGenHelper.ClearTile(x, y, true);
            }
        }
        private static void GenerateVeilMasses(int centerX, int minX, int maxX, int startY, int endY, int width, int height)
        {
            UnifiedRandom rand = WorldGen.genRand;

            int majorCount = rand.Next(4, 7);
            int minorCount = rand.Next(6, 11);

            // Major masses.
            for (int i = 0; i < majorCount; i++)
            {
                float px = rand.NextFloat(0.18f, 0.82f);
                float py = rand.NextFloat(0.12f, 0.82f);

                int cx = minX + (int)(width * px) + rand.Next(-30, 31);
                int cy = startY + (int)(height * py) + rand.Next(-20, 21);

                int rx = rand.Next(width / 10, width / 4);
                int ry = rand.Next(height / 12, height / 5);

                int mat = rand.NextBool(4) ? VoidstoneType : MantleType;
                PlaceNoisyBlob(cx, cy, rx, ry, mat, rand.NextFloat(0.12f, 0.28f));

                // Core reinforcement so some masses have layered geology.
                if (rand.NextBool(2))
                {
                    int innerMat = mat == MantleType ? VoidstoneType : MantleType;
                    PlaceNoisyBlob(
                        cx + rand.Next(-12, 13),
                        cy + rand.Next(-12, 13),
                        Math.Max(5, (int)(rx * rand.NextFloat(0.35f, 0.65f))),
                        Math.Max(4, (int)(ry * rand.NextFloat(0.35f, 0.65f))),
                        innerMat,
                        rand.NextFloat(0.08f, 0.18f));
                }
            }

            // Minor fragments / shelves.
            for (int i = 0; i < minorCount; i++)
            {
                int cx = centerX + rand.Next(-(int)(width * 0.40f), (int)(width * 0.40f));
                int cy = startY + rand.Next((int)(height * 0.10f), (int)(height * 0.90f));

                int rx = rand.Next(10, Math.Max(14, width / 12));
                int ry = rand.Next(6, Math.Max(10, height / 14));

                int mat = rand.NextBool(3) ? VoidstoneType : MantleType;
                PlaceNoisyBlob(cx, cy, rx, ry, mat, rand.NextFloat(0.08f, 0.18f));
            }
        }
        private static void CarveLane(Vector2 start, Vector2 end, int radiusX, int radiusY, float irregularity, float jitter, bool fillWithWater)
        {
            UnifiedRandom rand = WorldGen.genRand;

            Vector2 jitterStart = start + new Vector2(rand.NextFloat(-jitter, jitter), rand.NextFloat(-jitter * 0.5f, jitter * 0.5f));
            Vector2 jitterEnd = end + new Vector2(rand.NextFloat(-jitter, jitter), rand.NextFloat(-jitter * 0.5f, jitter * 0.5f));

            float bend = rand.NextFloat(-1f, 1f);
            Vector2 mid = Vector2.Lerp(jitterStart, jitterEnd, 0.5f);
            Vector2 normal = (jitterEnd - jitterStart).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            mid += normal * bend * jitter;

            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(jitterStart, mid, radiusX, radiusY, irregularity, fillWithWater);
            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(mid, jitterEnd, radiusX, radiusY, irregularity, fillWithWater);
        }
        private static void GuaranteeEntrance(int entryX, int startY, int driftX)
        {
            Vector2 start = ThermalVentsLayer.ExitPosition + new Vector2(0,-40);
            Vector2 mid = start + Vector2.UnitY * WorldGen.genRand.Next(24, 38);
            Vector2 end = mid + new Vector2(driftX, WorldGen.genRand.Next(35, 58));
            

            AbyssWorldGenHelper.CarveBlob(entryX, (int)start.Y, 120, 40, 2, true);
            //AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(start, mid, 5, 10, 0.08f, false);
        }
        private static void ScatterVoidstonePatches(int minX, int maxX, int startY, int endY, int width, int height)
        {
            UnifiedRandom rand = WorldGen.genRand;
            int patchCount = rand.Next(10, 18);

            for (int i = 0; i < patchCount; i++)
            {
                int cx = rand.Next(minX + width / 12, maxX - width / 12);
                int cy = rand.Next(startY + height / 12, endY - height / 12);
                int rx = rand.Next(8, 22);
                int ry = rand.Next(6, 18);

                for (int x = cx - rx - 2; x <= cx + rx + 2; x++)
                {
                    for (int y = cy - ry - 2; y <= cy + ry + 2; y++)
                    {
                        if (!WorldGen.InWorld(x, y, 10))
                            continue;

                        Tile tile = Framing.GetTileSafely(x, y);
                        if (!tile.HasTile)
                            continue;

                        if (tile.TileType != MantleType)
                            continue;

                        float dx = (x - cx) / (float)Math.Max(1, rx);
                        float dy = (y - cy) / (float)Math.Max(1, ry);
                        float dist = dx * dx + dy * dy;

                        float n = AbyssWorldGenHelper.FractalNoise(x * 0.18f, y * 0.18f, 3);
                        float threshold = 1f + (n - 0.5f) * 0.55f;

                        if (dist <= threshold)
                            tile.TileType = (ushort)VoidstoneType;
                    }
                }
            }
        }
        private static void PlaceNoisyBlob(int centerX, int centerY, int radiusX, int radiusY, int type, float noiseStrength)
        {
            UnifiedRandom rand = WorldGen.genRand;
            float phaseA = rand.NextFloat(MathHelper.TwoPi);
            float phaseB = rand.NextFloat(MathHelper.TwoPi);
            float worldOffsetX = rand.NextFloat(0f, 1000f);
            float worldOffsetY = rand.NextFloat(0f, 1000f);

            for (int x = centerX - radiusX - 2; x <= centerX + radiusX + 2; x++)
            {
                for (int y = centerY - radiusY - 2; y <= centerY + radiusY + 2; y++)
                {
                    if (!WorldGen.InWorld(x, y, 10))
                        continue;

                    float dx = (x - centerX) / (float)Math.Max(1, radiusX);
                    float dy = (y - centerY) / (float)Math.Max(1, radiusY);
                    float dist = dx * dx + dy * dy;

                    float angle = MathF.Atan2(dy, dx);
                    float radialNoise =
                        MathF.Sin(angle * 3f + phaseA) * noiseStrength +
                        MathF.Sin(angle * 5f + phaseB) * (noiseStrength * 0.5f);

                    float worldNoise = (AbyssWorldGenHelper.FractalNoise(
                        x * 0.07f + worldOffsetX,
                        y * 0.07f + worldOffsetY,
                        3) - 0.5f) * 0.45f;

                    if (dist <= 1f + radialNoise + worldNoise)
                        AbyssWorldGenHelper.PlaceSolidTile(x, y, type, true);
                }
            }
        }

        private static void CapExposedSurfacesWithMarineSnow(int minX, int maxX, int minY, int maxY, int depth)
        {
            for (int pass = 0; pass < depth; pass++)
            {
                for (int x = minX + 1; x < maxX - 1; x++)
                {
                    for (int y = minY + 1; y < maxY - 1; y++)
                    {
                        Tile tile = Framing.GetTileSafely(x, y);
                        if (!tile.HasTile || !(tile.TileType == MantleType|| tile.TileType == ModContent.TileType<Voidstone>()))
                            continue;

                        bool up = !Framing.GetTileSafely(x, y - 1).HasTile;
                        bool down = !Framing.GetTileSafely(x, y + 1).HasTile;
                        bool left = !Framing.GetTileSafely(x - 1, y).HasTile;
                        bool right = !Framing.GetTileSafely(x + 1, y).HasTile;

                        bool upLeft = !Framing.GetTileSafely(x - 1, y - 1).HasTile;
                        bool upRight = !Framing.GetTileSafely(x + 1, y - 1).HasTile;
                        bool downLeft = !Framing.GetTileSafely(x - 1, y + 1).HasTile;
                        bool downRight = !Framing.GetTileSafely(x + 1, y + 1).HasTile;

                        // Never accumulate on true undersides / hanging bottoms.
                        if (down)
                            continue;

                        int score = 0;

                        // Strong preference: open space directly above.
                        if (up)
                            score += 4;

                        // Gentle upper shoulders / ledges.
                        if (upLeft)
                            score += 2;
                        if (upRight)
                            score += 2;

                        // Slight side exposure can help, but should not dominate.
                        if (left)
                            score += 1;
                        if (right)
                            score += 1;

                        // Broad, stable top surfaces get more buildup.
                        bool supportedBelow = Framing.GetTileSafely(x, y + 1).HasTile;
                        bool supportedBelowLeft = Framing.GetTileSafely(x - 1, y + 1).HasTile;
                        bool supportedBelowRight = Framing.GetTileSafely(x + 1, y + 1).HasTile;

                        if (supportedBelow)
                            score += 2;
                        if (supportedBelowLeft && supportedBelowRight)
                            score += 1;

                        // Penalize very steep side walls.
                        bool verticalWallLeft = left && !up && !upLeft && !upRight;
                        bool verticalWallRight = right && !up && !upLeft && !upRight;
                        if (verticalWallLeft || verticalWallRight)
                            score -= 3;

                        // Penalize convex hanging corners.
                        if (downLeft || downRight)
                            score -= 1;

                        // Outer passes should only hit the best candidates.
                        int threshold = pass switch
                        {
                            0 => 6,
                            1 => 7,
                            _ => 8
                        };

                        if (score >= threshold)
                            tile.TileType = (ushort)SnowType;
                    }
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

            for (int offset = 0; offset < 40; offset++)
            {
                int x = edgeX + offset * dir;
                int inset = 12 + offset / 2;

                for (int y = startY + inset; y <= endY - inset; y++)
                    AbyssWorldGenHelper.PlaceSolidTile(x, y, MantleType, true); 
            }
        }


      
    }
}

using AbyssOverhaul.Content.Layers.FossilShale.Systems;
using CalamityMod;
using global::AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using Terraria.Utilities;
namespace AbyssOverhaul.Content.Layers.FossilShale.WorldGen
{

    public static partial class FossilShaleGen
    {

       

        public static void SeedCyanobacteriaVines(int minX, int maxX, int topY, int bottomY)
        {
            ushort shale = (ushort)ModContent.TileType<CarbonShale_Tile>();
            ushort vine = (ushort)ModContent.TileType<Cyanobacteria_Vines>();

            for (int x = minX; x < maxX; x++)
            {
                for (int y = topY; y < bottomY - 2; y++)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 2))
                        continue;

                    Tile ceiling = Main.tile[x, y];
                    Tile below = Main.tile[x, y + 1];

                    if (!ceiling.HasTile || ceiling.TileType != shale)
                        continue;
                    
                    if (ceiling.Slope != 0 || ceiling.IsHalfBlock)
                        continue;

                    // Need empty space below
                    if (below.HasTile)
                        continue;

                    if (Terraria.WorldGen.genRand.NextBool(3)) 
                    {
                        Terraria.WorldGen.PlaceTile(x, y + 1, vine, mute: true, forced: true);

                        Main.tile[x, y + 1].LiquidAmount = 0;

                        Terraria.WorldGen.SquareTileFrame(x, y + 1);
                    }
                }
            }
        }
        public static void GrowCyanobacteriaVines(int minX, int maxX, int topY, int bottomY)
        {
            ushort vine = (ushort)ModContent.TileType<Cyanobacteria_Vines>();

            for (int x = minX; x < maxX; x++)
            {
                for (int y = topY; y < bottomY - 2; y++)
                {
                    if (!Terraria.WorldGen.InWorld(x, y, 2))
                        continue;

                    Tile t = Main.tile[x, y];
                    if (!t.HasTile || t.TileType != vine)
                        continue;

                    // Grow 1–4 tiles downward if possible
                    int length = Terraria.WorldGen.genRand.Next(1, 5);

                    // If you want to use Calamity's helper:
                    CalamityUtils.GrowVines(x, y, length, vine);

                }
            }
        }



        internal static void PlaceBacteriaChainsInTunnels(List<Tunnel> tunnels, UnifiedRandom rand)
        {
            HashSet<string> usedPairs = new();

            for (int i = 0; i < tunnels.Count; i++)
                PlaceBacteriaChainsInTunnel(tunnels[i], rand, usedPairs);
        }

        private static void PlaceBacteriaChainsInTunnel(Tunnel tunnel, UnifiedRandom rand, HashSet<string> usedPairs, int minSpacing = 7)
        {
            if (tunnel.Points.Count < 3)
                return;

            int lastPlacedIndex = -9999;

            for (int i = 1; i < tunnel.Points.Count - 1; i++)
            {
                if (i - lastPlacedIndex < minSpacing)
                    continue;

                Point prev = tunnel.Points[i - 1];
                Point curr = tunnel.Points[i];
                Point next = tunnel.Points[i + 1];

                Vector2 tangent = next.ToVector2() - prev.ToVector2();
                if (tangent.LengthSquared() < 0.001f)
                    continue;

                tangent.Normalize();

                Vector2 normal = new Vector2(-tangent.Y, tangent.X);

                int radius = tunnel.Radius[i];
                if (radius < 4)
                    continue;

                float widthFactor = MathHelper.Clamp((radius - 4f) / 10f, 0f, 1f);
                float placeChance = MathHelper.Lerp(0.6f, 1f, widthFactor);

                if (rand.NextFloat() > placeChance)
                    continue;

                if (TryCreateBacteriaChainAtSample(curr, normal, radius, rand, usedPairs, out AnchoredTileChain chain))
                {
                    lastPlacedIndex = i;

                    // Pre-sag it a bit so it doesn't begin as a perfectly straight line.
                    SeedChainSag(chain, rand.NextFloat(1f, 6f));
                }
            }
        }

        private static bool TryCreateBacteriaChainAtSample(
            Point curr,
            Vector2 normal,
            int radius,
            UnifiedRandom rand,
            HashSet<string> usedPairs,
            out AnchoredTileChain chain)
        {
            chain = null;

            Vector2[] candidateDirs =
            {
                normal,
                -normal,
                Vector2.Normalize(normal.RotatedBy(rand.NextFloat(-0.35f, 0.35f))),
                Vector2.Normalize((-normal).RotatedBy(rand.NextFloat(-0.35f, 0.35f))),
                Vector2.Normalize(normal.RotatedBy(0.55f)),
                Vector2.Normalize(normal.RotatedBy(-0.55f))
            };

            for (int attempt = 0; attempt < candidateDirs.Length; attempt++)
            {
                Vector2 dir = candidateDirs[attempt];
                if (dir.LengthSquared() < 0.001f)
                    continue;

                dir.Normalize();

                if (!TryFindSolidTunnelAnchor(curr, dir, radius + 8, out Point16 a))
                    continue;

                if (!TryFindSolidTunnelAnchor(curr, -dir, radius + 8, out Point16 b))
                    continue;

                if (a == b)
                    continue;

                float tileDistance = Vector2.Distance(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y));
                if (tileDistance < 3.5f || tileDistance > 18f)
                    continue;

                string pairKey = MakeOrderedAnchorKey(a, b);
                if (usedPairs.Contains(pairKey))
                    continue;

                usedPairs.Add(pairKey);

                Vector2 startOffset = -dir * rand.NextFloat(4f, 7f);
                Vector2 endOffset = dir * rand.NextFloat(4f, 7f);

                TileToTileChainSystem.RemoveChainsBetween(a, b);

                chain = TileToTileChainSystem.AddChain(
                    a,
                    b,
                    pixelsPerSegment: rand.NextFloat(6f, 9f),
                    gravity: rand.NextFloat(0.05f, 0.14f),
                    damping: rand.NextFloat(0.986f, 0.994f),
                    simulateIterations: 5,
                    anchorIterations: 5,
                    collideWithTiles: true,
                    collisionRadius: rand.NextFloat(2.5f, 3.5f),
                    startOffset: startOffset,
                    endOffset: endOffset,
                    thickness: rand.NextFloat(1.5f, 2.7f));

                chain.ChainColor = rand.NextBool(3)
                    ? new Color(70, 150, 120, 220)
                    : new Color(100, 185, 135, 220);

                chain.ShadowColor = new Color(8, 20, 14, 90);

                return true;
            }

            return false;
        }

        private static bool TryFindSolidTunnelAnchor(Point start, Vector2 dir, int maxDistance, out Point16 anchorTile)
        {
            anchorTile = default;

            if (dir.LengthSquared() < 0.001f)
                return false;

            dir.Normalize();

            Vector2 startPos = start.ToVector2();

            for (int step = 1; step <= maxDistance; step++)
            {
                Point tilePos = (startPos + dir * step).ToPoint();

                if (!Terraria.WorldGen.InWorld(tilePos.X, tilePos.Y, 10))
                    return false;

                Tile tile = Framing.GetTileSafely(tilePos.X, tilePos.Y);

                if (tile.HasTile && !Main.tileSolidTop[tile.TileType])
                {
                    anchorTile = new Point16(tilePos.X, tilePos.Y);
                    return true;
                }
            }

            return false;
        }

        private static string MakeOrderedAnchorKey(Point16 a, Point16 b)
        {
            bool aFirst = a.X < b.X || (a.X == b.X && a.Y <= b.Y);

            if (aFirst)
                return $"{a.X},{a.Y}|{b.X},{b.Y}";

            return $"{b.X},{b.Y}|{a.X},{a.Y}";
        }

        private static void SeedChainSag(AnchoredTileChain chain, float maxSagPixels)
        {
            int pointCount = chain.Chain.Positions.Length;
            if (pointCount <= 2)
                return;

            for (int i = 1; i < pointCount - 1; i++)
            {
                float t = i / (float)(pointCount - 1);
                float sag = MathF.Sin(t * MathHelper.Pi) * maxSagPixels;

                chain.Chain.Positions[i] += Vector2.UnitY * sag;
                chain.Chain.OldPositions[i] += Vector2.UnitY * sag;
            }
        }
    }





}

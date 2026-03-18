
using CalamityMod;
using global::AbyssOverhaul.Content.Layers.FossilShale.Tiles;
namespace AbyssOverhaul.Content.Layers.FossilShale.WorldGen
{

    public static class FossilShaleGen
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









    }
}

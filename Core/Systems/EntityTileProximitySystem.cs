using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Systems
{
    public class ProximityLightSystem : ModSystem
    {
        public static Dictionary<Point, float> BrightnessByTile = new();

        public override void PostUpdateEverything()
        {
            BrightnessByTile.Clear();

            float maxRange = 240f;
            int tileRange = (int)(maxRange / 16f) + 1;

            foreach (Player player in Main.player)
            {
                if (player == null || !player.active || player.dead)
                    continue;

                Point centerTile = player.Center.ToTileCoordinates();

                for (int x = centerTile.X - tileRange; x <= centerTile.X + tileRange; x++)
                {
                    for (int y = centerTile.Y - tileRange; y <= centerTile.Y + tileRange; y++)
                    {
                        if (!Terraria.WorldGen.InWorld(x, y))
                            continue;

                        Tile tile = Main.tile[x, y];
                        if (tile == null || !tile.HasTile)
                            continue;
                        
                        if (tile.TileType != ModContent.TileType<CyanobacteriaCarpet>())
                            continue;

                        Vector2 tileCenter = new Vector2(x * 16f + 8f, y * 16f + 8f);
                        float dist = Vector2.Distance(tileCenter, player.Center);
                        if (dist > maxRange)
                            continue;

                        float t = 1f - dist / maxRange;
                        t = Utils.Clamp(t, 0f, 1f);
                        t *= t;

                        Point key = new Point(x, y);
                        if (!BrightnessByTile.TryGetValue(key, out float old))
                            old = 0f;

                        BrightnessByTile[key] = Math.Max(old, t);
                    }
                }
            }
        }
    }
}

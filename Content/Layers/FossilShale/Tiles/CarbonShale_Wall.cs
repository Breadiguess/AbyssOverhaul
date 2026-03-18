using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles
{
    internal class CarbonShale_Wall : ModWall
    {
        public override void SetStaticDefaults()
        {
            Main.wallHouse[Type] = false;

            DustType = DustID.Cloud;
            VanillaFallbackOnModDeletion = WallID.Sandstone;

            AddMapEntry(new Color(150, 150, 150));
        }
        public override void RandomUpdate(int i, int j)
        {
            if (Main.tile[i, j].LiquidAmount == 0 && j < Main.maxTilesY - 205)
            {
                Main.tile[i, j].Get<LiquidData>().LiquidType = LiquidID.Water;
                Main.tile[i, j].LiquidAmount = byte.MaxValue;
                Terraria.WorldGen.SquareTileFrame(i, j);
                if (Main.dedServ)
                    NetMessage.sendWater(i, j);
            }
        }
        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }
}

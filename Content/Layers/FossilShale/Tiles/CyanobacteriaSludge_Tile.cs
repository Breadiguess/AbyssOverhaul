using AbyssOverhaul.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles
{
    internal class CyanobacteriaSludge_Tile : ModTile
    {
        public override string LocalizationCategory => "Tiles";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            Main.tileMerge[Type][ModContent.TileType<CarbonShale_Tile>()] = true;
            DustType = DustID.t_Flesh;//ModContent.DustType<Sparkle>();
            VanillaFallbackOnModDeletion = TileID.DiamondGemspark;

            AbyssUtilities.MergeWithNewAbyss(Type);
            AddMapEntry(new Color(221, 154, 160));
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }
}

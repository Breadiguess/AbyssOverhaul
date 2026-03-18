using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Core.Utilities;
using CalamityMod;
using CalamityMod.Tiles.Abyss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles
{
    internal class Tenebris_Tile : ModTile
    {
        public override string LocalizationCategory => "Tiles";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileMerge[Type][ModContent.TileType<MantleGravel_Tile>()] = true;
            DustType = DustID.Cloud;//ModContent.DustType<Sparkle>();
            VanillaFallbackOnModDeletion = TileID.DiamondGemspark;



            AbyssUtilities.MergeWithNewAbyss(Type);
            AddMapEntry(new Color(56 ,104, 107));
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

    }
}

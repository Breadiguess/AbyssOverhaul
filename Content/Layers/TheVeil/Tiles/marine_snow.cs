using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Core.Utilities;
using CalamityMod;
using CalamityMod.Sounds;
using CalamityMod.Tiles.Abyss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.TheVeil.Tiles
{
    internal class marine_snow : ModTile
    {
        public override string LocalizationCategory => "Tiles";
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            DustType = DustID.Snow;//ModContent.DustType<Sparkle>();

            VanillaFallbackOnModDeletion = TileID.DiamondGemspark;
            Main.tileMerge[Type][ModContent.TileType<VoidstoneMantle>()] = true;
            Main.tileMerge[Type][ModContent.TileType<Voidstone>()] = true;
            MineResist = 5f;
            MinPick = 100;


            AbyssUtilities.MergeWithNewAbyss(Type); 
            AddMapEntry(new Color(205,205,205));
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
    }
}

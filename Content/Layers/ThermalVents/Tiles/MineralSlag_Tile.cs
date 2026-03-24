using AbyssOverhaul.Core.Utilities;
using CalamityMod.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.ThermalVents.Tiles
{
    internal class MineralSlag_Tile :ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;


            AbyssUtilities.MergeWithNewAbyss(Type);

            HitSound = CommonCalamitySounds.PlatingMine;
            MineResist = 10f;
            MinPick = 180;
            DustType = DustID.Ambient_DarkBrown;
            VanillaFallbackOnModDeletion = TileID.DiamondGemspark;

            AddMapEntry(new Color(189, 175, 158));
        }
    }
}

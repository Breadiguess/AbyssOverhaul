using CalamityMod.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles.Merges
{
    internal class CyanobacteriaSludge_Merge : TileBlendTexture
    {
        public override int TileType => ModContent.TileType<CyanobacteriaSludge_Tile>();
    }
}

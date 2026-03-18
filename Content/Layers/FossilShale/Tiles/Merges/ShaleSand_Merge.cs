using CalamityMod.Systems;
using Terraria.ID;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles.Merges
{
    public sealed class ShaleSand_Merge : TileBlendTexture
    {
        public override int TileType => ModContent.TileType<ShaleSand_Tile>();
    }
}

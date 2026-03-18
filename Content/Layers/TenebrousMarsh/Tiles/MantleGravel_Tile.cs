
using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Core.Utilities;
using CalamityMod;
using CalamityMod.Sounds;
using CalamityMod.Tiles.Abyss;

namespace AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles
{
    internal class MantleGravel_Tile : ModTile
    {
        public override string LocalizationCategory => "Tiles";
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

            AddMapEntry(new Color(69, 62, 62));
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

    }
}

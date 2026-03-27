using AbyssOverhaul.Core;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles;

internal class MicrobialSlate : SlateTile
{

    public override void SetStaticDefaults()
    {

        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
        Main.tileBlendAll[Type] = true;

        DustType = DustID.Pearlsand;
        HitSound = SoundID.Tink.WithPitchOffset(-0.5f);

        MineResist = 2;
        MinPick = 70;
        AddMapEntry(new Color(186, 137, 186));

        SetShaleStartCoords(12, 0);
    }
}

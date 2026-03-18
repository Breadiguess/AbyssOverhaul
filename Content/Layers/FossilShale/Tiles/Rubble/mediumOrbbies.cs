using Luminance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ObjectData;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles.Rubble
{
    public class MediumOrbbies : ModTile
    {
        public override string Texture => $"{this.GetPath()}";
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileWaterDeath[Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.StyleHorizontal = true;

            TileObjectData.addTile(Type);

            AddMapEntry(new Color(162, 55, 196));
            DustType = DustID.PurpleMoss;
            HitSound = SoundID.Dig;

            base.SetStaticDefaults();
        }


        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            base.ModifyLight(i, j, ref r, ref g, ref b);
        }
    }

}

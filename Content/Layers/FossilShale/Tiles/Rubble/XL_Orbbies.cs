using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ObjectData;

namespace AbyssOverhaul.Content.Layers.FossilShale.Tiles.Rubble
{
    internal class XL_Orbbies:ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            Main.tileLighted[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);

            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 4;

            TileObjectData.newTile.Origin = new Point16(1,3);

            TileObjectData.newTile.CoordinateHeights = new[]
            {
                16, 16, 16, 16
            };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;

            TileObjectData.newTile.StyleHorizontal = true;

            TileObjectData.newTile.StyleWrapLimit = 4;

            TileObjectData.newTile.RandomStyleRange = 16;


            TileObjectData.addTile(Type);

            AddMapEntry(new Color(255,0,0));

        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            base.ModifyLight(i, j, ref r, ref g, ref b);
            r = 1;
            g = 1;
            b = 1;
        }
    }
}

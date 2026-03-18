using AbyssOverhaul.Core.Utilities;
using CalamityMod;
using FullSerializer.Internal;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles
{
    internal class BrineCrystal_Tile : ModTile
    {
        public override void SetStaticDefaults()
        {

            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = false;
            //CalamityUtils.MergeWithGeneral(Type);
            //CalamityUtils.MergeSmoothTiles(Type);
            //CalamityUtils.MergeWithAbyss(Type);
            Main.tileLighted[Type] = true;
            Main.tileShine2[Type] = false;
            TileID.Sets.ChecksForMerge[Type] = true;
            TileID.Sets.WallsMergeWith[Type] = true;
            DustType = DustID.RainCloud;
            AddMapEntry(new Color(197, 220, 220));
            HitSound = SoundID.Shatter;
            MinPick = 55;

            AbyssUtilities.MergeWithNewAbyss(Type);
        }
    }
}

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
    internal class SmoothedBrineCrystal : ModTile
    {
        public short sub_sheet_w = 234;
        public short sub_sheet_h = 90;
        public override void SetStaticDefaults()
        {

            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileShine2[Type] = true;

            Main.tileBlendAll[Type] = true;

            HitSound = SoundID.Tink;

            DustType = DustID.Water_Snow;
            MineResist = 2;
            MinPick = 55;
            AddMapEntry(new Color(74, 68, 76), CreateMapEntryName());
        }
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            int chunkX = i / 8;
            int chunkY = j / 8;

            int chunkSeed = Main.ActiveWorldFileData.Seed + (chunkX * 1000) + chunkY;
            Random seedrand = new Random(chunkSeed);

            int variantIndex = seedrand.Next(0, 4);

            frameXOffset = (i % 8) * sub_sheet_w;
            frameYOffset = (j % 8 * sub_sheet_h) + (variantIndex * 720);
        }
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float value = (0.5f + MathF.Cos(Main.GlobalTimeWrappedHourly / 3) / 2) / 3;

            Color blue = new Color(110, 146, 177);

            Color lerpedColor = Color.Lerp(blue, Color.SeaGreen, value);

            float glow_multiple = MathF.Cos(Main.GlobalTimeWrappedHourly) / 8 + 0.85f;
            r = lerpedColor.R / 255f * glow_multiple;
            g = lerpedColor.G / 255f * glow_multiple;
            b = lerpedColor.B / 255f * glow_multiple;
        }

    }
}

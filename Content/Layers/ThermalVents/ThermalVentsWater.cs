using CalamityMod.Dusts.WaterSplash;
using CalamityMod.Gores.WaterDroplet;
using CalamityMod.Systems.Graphic.LiquidSystem;
using CalamityMod.Waters;
using Terraria.Graphics;

namespace AbyssOverhaul.Content.Layers.ThermalVents
{
    internal class ThermalVentsWaterflow : ModWaterfallStyle, IWaterfallStyleModifyColor
    {

        public void ModifyColor(in Tile tile, int x, int y, ref VertexColors liquidColor) => WaterStyleCommon.ModifyTransparentWaterColor(x, y, ref liquidColor, false);

    }

    public class ThermalVentsWater : ModWaterStyle, IWaterStyleModifyColor
    {
        public static ModWaterStyle Instance { get; private set; }
        public static ModWaterfallStyle WaterfallStyle { get; private set; }
        public static int SplashDust { get; private set; }
        public static int DropletGore { get; private set; }

        public override void SetStaticDefaults()
        {
            Instance = this;
            WaterfallStyle = ModContent.Find<ModWaterfallStyle>("CalamityMod/UpperAbyssWaterflow");
            SplashDust = ModContent.DustType<SunkenSeaBurrowsSplash>();
            DropletGore = ModContent.GoreType<SunkenSeaBurrowsWaterDroplet>();
        }

        public override void Unload()
        {
            Instance = null;
            WaterfallStyle = null;
            SplashDust = 0;
            DropletGore = 0;
        }

        public override int ChooseWaterfallStyle() => WaterfallStyle.Slot;
        public override int GetSplashDust() => SplashDust;
        public override int GetDropletGore() => DropletGore;
        public override Color BiomeHairColor() => new Color(9, 69, 82);
        public void ModifyColor(in Tile tile, int x, int y, ref VertexColors liquidColor, bool isSlope) => WaterStyleCommon.ModifyTransparentWaterColor(x, y, ref liquidColor, isSlope);
    }

}

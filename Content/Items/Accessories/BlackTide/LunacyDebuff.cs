using Luminance.Assets;
using Terraria.ID;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class LunacyDebuff: ModBuff
    {
        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;
        public override void SetStaticDefaults()
        {
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
    }
}
using Luminance.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideMoonProjectile : ModProjectile
    {
        public override string Texture =>  MiscTexturesRegistry.InvisiblePixelPath;
        public override void SetDefaults()
        {
            Projectile.Size = new(40);

            Projectile.friendly = true;
            Projectile.hostile = false;
        }
    }
}

using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Accessories.OmegaBlue_Wings
{
    [Autoload(Side = ModSide.Client)]
    internal sealed class OmegaBlue_TailsPixelLoader : ModSystem
    {
        private static OmegaBlue_TailsPixelDrawer drawer;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            drawer = new OmegaBlue_TailsPixelDrawer();
            PlayerPixelRegistry.Register(drawer);
        }

        public override void Unload()
        {
            if (!Main.dedServ && drawer is not null)
                PlayerPixelRegistry.Unregister(drawer);

            drawer = null;
        }
    }
}

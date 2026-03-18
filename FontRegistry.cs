using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul
{
    public static class FontRegistry
    {
        public static readonly DynamicSpriteFont BlackSide = ModContent.Request<DynamicSpriteFont>("AbyssOverhaul/Assets/Fonts/Blackside", AssetRequestMode.ImmediateLoad).Value;
        public static readonly DynamicSpriteFont BlackSide2 = ModContent.Request<DynamicSpriteFont>("AbyssOverhaul/Assets/Fonts/Blackside Personal Use Only", AssetRequestMode.ImmediateLoad).Value;

        public static readonly DynamicSpriteFont BlackForest = ModContent.Request<DynamicSpriteFont>("AbyssOverhaul/Assets/Fonts/Black Forest", AssetRequestMode.ImmediateLoad).Value;

    }
}

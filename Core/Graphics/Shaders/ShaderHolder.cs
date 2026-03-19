using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.Shaders
{
    internal class ShaderHolder : ModSystem
    {
        public static Asset<Effect> EschatonSlash;
        public override void Load()
        {
            if (Main.dedServ)
                return;
            EschatonSlash = ModContent.Request<Effect>("AbyssOverhaul/Assets/Shaders/EschatonSlash");

        }
    }
}

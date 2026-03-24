using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness
{
    internal partial class ReworkedAbyssLighting
    {
    

        public static void AddProjectileLight(
           Vector2 position,
            Texture2D texture = null,
            float scale = 1f,
            Vector2? vectorScale = null,
            float opacity = 1f,
            Color? color = null,
            int lifetime = 2,
            Rectangle? frame = null,
            float rotation = 0f,
            Vector2 origin = default)
        {
            if (Main.dedServ)
                return;

            Vector2 worldCenter = position;

            AddLight(new LightSource(
                center: worldCenter,
                texture: texture,
                scale: scale,
                rotation: rotation,
                vectorScale: vectorScale,
                opacity: opacity,
                origin: origin)
            {
                color = color ?? Color.White,
                lifetime = lifetime,
                frame = frame
            });
        }
    }
}

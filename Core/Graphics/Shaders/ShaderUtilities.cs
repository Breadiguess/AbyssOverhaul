using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.Shaders
{
    internal static class ShaderUtilities
    {
        public static void SetParameter(Effect effect, string name, float value)
            => effect.Parameters[name]?.SetValue(value);

        public static void SetParameter(Effect effect, string name, Microsoft.Xna.Framework.Vector2 value)
            => effect.Parameters[name]?.SetValue(value);

        public static void SetParameter(Effect effect, string name, Microsoft.Xna.Framework.Vector3 value)
            => effect.Parameters[name]?.SetValue(value);

        public static void SetParameter(Effect effect, string name, Microsoft.Xna.Framework.Vector4 value)
            => effect.Parameters[name]?.SetValue(value);

        public static void SetParameter(Effect effect, string name, Microsoft.Xna.Framework.Matrix value)
            => effect.Parameters[name]?.SetValue(value);

        public static void BindTexture(Texture2D texture, int slot, SamplerState sampler)
        {
            Main.instance.GraphicsDevice.Textures[slot] = texture;
            Main.instance.GraphicsDevice.SamplerStates[slot] = sampler;
        }
    }
}

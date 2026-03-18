using AbyssOverhaul.Core.ModPlayers;
using CalamityMod;
using CalamityMod.Utilities.Daybreak.Buffers;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.Graphics.Shaders;

namespace AbyssOverhaul.Core.Graphics
{
    internal class ReworkedAbyssLighting : ModSystem
    {
        private static Asset<Texture2D> _defaultTexture;
        public class LightSource
        {

            public Texture2D texture = _defaultTexture.Value;
            public float scale = 1;
            public Vector2 vectorScale = Vector2.One;
            public Vector2 center = Main.LocalPlayer.Center;
            public float rotation = 0;
            public float opacity = 1;
            public Color color = Color.White;
            public int lifetime = 1;
            public Rectangle? frame = null;
            public Vector2 Origin;
            public LightSource() { }

            // This constructor only gives the most common arguments. Frame, Color, lifetime, etc. must be set in curly braces afterwards to prevent this constructor getting too unwieldy.
            public LightSource(Vector2? center = null, Texture2D texture = null, float scale = 1, float rotation = 0, Vector2? vectorScale = null, float opacity = 1, Vector2 origin = default)
            {
                this.texture = texture ?? ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                this.scale = scale;
                this.vectorScale = vectorScale ?? Vector2.One;
                this.center = center ?? Main.LocalPlayer.Center;
                this.rotation = rotation;
                this.opacity = opacity;
                Origin = origin;
            }
        }

        public static List<LightSource> lights = new();
        public override void Load()
        {
            On_OverlayManager.Draw += DrawShadowOverlay;
            On_LightingEngine.UpdateLightDecay += AdjustTransmissiveness;
            _defaultTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
        }

        private void DrawShadowOverlay(On_OverlayManager.orig_Draw orig, OverlayManager self, SpriteBatch spriteBatch, RenderLayers layer, bool beginSpriteBatch)
        {
            orig(self, spriteBatch, layer, beginSpriteBatch);
            if (Main.gameMenu || layer != RenderLayers.All)
                return;

            var abyssPlayer = Main.LocalPlayer.Abyss();

            //Main.NewText(abyssPlayer.darknessIntensity);
           
            var device = Main.instance.GraphicsDevice;
            using var lease = Daybreak.Common.Rendering.RenderTargetPool.Shared.Rent(
                device,
                Main.screenWidth,
                Main.screenHeight,
                Daybreak.Common.Rendering.RenderTargetDescriptor.Default
            );

            using (lease.Scope(clearColor: Color.Black))
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
                foreach (var item in lights)
                {
                    Main.spriteBatch.Draw(item.texture, item.center - Main.screenPosition, item.frame, item.color * item.opacity, item.rotation, item.Origin == default? item.frame is null ? item.texture.Size() * 0.5f : item.frame.Value.Size() : item.Origin, item.vectorScale * item.scale, SpriteEffects.None, 0);
                }
                Main.spriteBatch.End();
            }

            using (Main.spriteBatch.Scope())
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                var shader = GameShaders.Misc["CalamityMod:DozeLightingShader"];
                shader.UseOpacity(abyssPlayer.darknessIntensity);
                shader.Apply();
                Main.spriteBatch.Draw(lease.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                spriteBatch.End();
            }
        }


        private const float VanillaWaterLightMult = 0.91f; //Vanilla water light multiplier.
        private void AdjustTransmissiveness(On_LightingEngine.orig_UpdateLightDecay orig, LightingEngine self)
        {
            orig(self);

            var mp = Main.LocalPlayer.Abyss();
            LightMap map = self._workingLightMap;
            if (true)//Main.LocalPlayer.GetModPlayer<PreventDeathFromJustBeingOutOfWater>().IsInModdedAbyssBiome)
            {
                //This converts the light decay amount from the amount it normally is in water into the amount it normally is in air, depending on the intensity of the abyss darkness.
                //This is to offset the abyss darkness system to make the parts that are supposed to be visible easier to see.
                //Dividing by 0.91 brings the water back to 100% transmissiveness with the original color
                map.LightDecayThroughWater = Vector3.Lerp(map.LightDecayThroughWater, (map.LightDecayThroughWater / VanillaWaterLightMult) * 0.98f, MathHelper.Clamp(mp.darknessIntensity, 0, 1));
            }
        }

        public override void OnWorldUnload()
        {
            lights.Clear();
        }

        public override void PreUpdateEntities()
        {
            //Every frame, the light sources are determined by what was added on that frame only. Therefore, we reset the light list every frame.
            //Lifetime is provided to smooth out things that don't run every frame consistently to prevent flickering, such as the abyss torches.
            for (var i = 0; i < lights.Count; i++)
            {
                var item = lights[i];
                item.lifetime--;
                if (item.lifetime <= 0)
                {
                    lights.Remove(item);
                    i--;
                }
            }
        }
    }
}

using AbyssOverhaul.Core.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Systems
{
    public class GiantCocoonSystem : ModSystem
    {
        public static readonly List<GiantCocoon> Cocoons = new();

        public static Asset<Texture2D> CocoonBack;
        public static Asset<Texture2D> CocoonMid;
        public static Asset<Texture2D> CocoonFront;
        public static Asset<Texture2D> CocoonStrands;
        public static Asset<Texture2D> CocoonGlow;

        public override void Load()
        {

            On_Main.DrawTiles += DrawCocoon;
            if (Main.dedServ)
                return;

            CocoonBack = Assets.Textures.Moon.Asset;
            CocoonMid = Assets.Textures.Moon.Asset;
            CocoonFront = Assets.Textures.Moon.Asset;
            CocoonStrands = Assets.Textures.Moon.Asset;// ModContent.Request<Texture2D>("YourMod/Assets/Cocoon/CocoonStrands");
            CocoonGlow = Assets.Textures.Moon.Asset; //ModContent.Request<Texture2D>("YourMod/Assets/Cocoon/CocoonGlow");
        }

       

        public override void Unload()
        {
            Cocoons.Clear();
        }

        public static GiantCocoon AddCocoon(Vector2 worldCenter, float scale = 1f, float rotation = 0f)
        {
            GiantCocoon cocoon = new(worldCenter, scale, rotation);
            Cocoons.Add(cocoon);
            return cocoon;
        }
        private void DrawCocoon(On_Main.orig_DrawTiles orig, Main self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets, int waterStyleOverride)
        {
            if (Main.dedServ || Cocoons.Count == 0)
            {

                orig(self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
                return;
            }

            SpriteBatch spriteBatch = Main.spriteBatch;

            foreach (var cocoon in Cocoons)
            {
                if (!cocoon.Active)
                    continue;

                DrawCocoon(spriteBatch, cocoon);
            }



            orig(self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
        }
       
        private static void DrawCocoon(SpriteBatch spriteBatch, GiantCocoon cocoon)
        {
            Vector2 cameraCenter = Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f;
            Vector2 cameraDelta = cameraCenter - cocoon.WorldCenter;

            float time = (float)Main.GlobalTimeWrappedHourly;

            DrawLayer(
                spriteBatch,
                CocoonBack.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.03f,
                scale: cocoon.BaseScale * 1.18f,
                rotation: cocoon.Rotation - 0.02f,
                color: Lighting.GetColor((int)cocoon.WorldCenter.X / 16, (int)cocoon.WorldCenter.Y / 16) * 0.65f,
                verticalBob: 2f,
                bobSpeed: 0.7f,
                time: time);

            DrawLayer(
                spriteBatch,
                CocoonMid.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.06f,
                scale: cocoon.BaseScale * 1.08f,
                rotation: cocoon.Rotation,
                color: Lighting.GetColor((int)cocoon.WorldCenter.X / 16, (int)cocoon.WorldCenter.Y / 16) * 0.85f,
                verticalBob: 3f,
                bobSpeed: 1.0f,
                time: time);

            DrawLayer(
                spriteBatch,
                CocoonStrands.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.095f,
                scale: cocoon.BaseScale * 1.02f,
                rotation: cocoon.Rotation + 0.01f,
                color: Color.White * 0.75f,
                verticalBob: 4f,
                bobSpeed: 1.3f,
                time: time);

            DrawLayer(
                spriteBatch,
                CocoonFront.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.13f,
                scale: cocoon.BaseScale,
                rotation: cocoon.Rotation + 0.03f,
                color: Color.White,
                verticalBob: 5f,
                bobSpeed: 1.6f,
                time: time);

            DrawLayer(
                spriteBatch,
                CocoonGlow.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.16f,
                scale: cocoon.BaseScale * 1.04f,
                rotation: cocoon.Rotation,
                color: new Color(120, 170, 200, 0) * 0.5f,
                verticalBob: 6f,
                bobSpeed: 1.8f,
                time: time);
        }

        private static void DrawLayer(
            SpriteBatch spriteBatch,
            Texture2D texture,
            Vector2 worldCenter,
            Vector2 cameraDelta,
            float parallaxFactor,
            float scale,
            float rotation,
            Color color,
            float verticalBob,
            float bobSpeed,
            float time)
        {
            Vector2 parallaxOffset = cameraDelta * parallaxFactor;
            Vector2 bobOffset = new Vector2(0f, (float)System.Math.Sin(time * bobSpeed) * verticalBob);

            Vector2 drawPos = worldCenter + parallaxOffset + bobOffset;
            Vector2 origin = texture.Size() * 0.5f;

            spriteBatch.Draw(
                texture,
                drawPos - Main.screenPosition,
                null,
                color,
                rotation,
                origin,
                scale,
                SpriteEffects.None,
                0f);
        }
    }
}

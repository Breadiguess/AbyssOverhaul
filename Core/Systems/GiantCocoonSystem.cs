using AbyssOverhaul.Core.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;

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
            CocoonFront = Assets.Textures.SilvaCocoon.Cocoon_Tex.Asset;
            CocoonStrands = Assets.Textures.SilvaCocoon.CocoonStrands.Asset;// ModContent.Request<Texture2D>("YourMod/Assets/Cocoon/CocoonStrands");
            CocoonGlow = Assets.Textures.Moon.Asset; //ModContent.Request<Texture2D>("YourMod/Assets/Cocoon/CocoonGlow");
        }

       

        public override void Unload()
        {
            Cocoons.Clear();
        }

        public override void OnWorldLoad()
        {
            Cocoons.Clear();
        }

        public override void OnWorldUnload()
        {
            Cocoons.Clear();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            List<TagCompound> cocoonTags = new();

            foreach (GiantCocoon cocoon in Cocoons)
            {
                if (cocoon is null)
                    continue;

                cocoonTags.Add(cocoon.Save());
            }

            tag["GiantCocoons"] = cocoonTags;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Cocoons.Clear();

            if (!tag.ContainsKey("GiantCocoons"))
                return;

            List<TagCompound> cocoonTags = (List<TagCompound>)tag.GetList<TagCompound>("GiantCocoons");

            foreach (TagCompound cocoonTag in cocoonTags)
            {
                Cocoons.Add(GiantCocoon.Load(cocoonTag));
            }
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
                CocoonStrands.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.5f,
                scale: cocoon.BaseScale * 0.92f,
                rotation: cocoon.Rotation + 0.01f,
                color: Color.White * 0.75f,
                verticalBob: 4f,
                bobSpeed: 0f,
                time: time);

            DrawLayer(
                spriteBatch,
                CocoonFront.Value,
                cocoon.WorldCenter,
                cameraDelta,
                parallaxFactor: 0.5f,
                scale: cocoon.BaseScale*0.8f,
                rotation: cocoon.Rotation + 0.03f,
                color: Color.White,
                verticalBob: 4f,
                bobSpeed:0,
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

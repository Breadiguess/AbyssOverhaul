using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace AbyssOverhaul.Core
{
    public class WorldBarSystem : ModSystem
    {
        public class WorldBar
        {
            public string Key;
            public Color BaseColor;
            public Color FillColor;
            public float FillPercent;
            public float DisplayedFillPercent;
            public int TimeLeft;
            public int MaxTimeLeft;
            public int Style;
            public Vector2 Offset;
            public Func<Vector2> GetWorldPosition;
            public bool Active = true;
            public bool SmoothFill = true;
            public float SmoothingSpeed = 0.18f;
            public int FadeInTime = 8;
            public int FadeOutTime = 20;
            public float Scale = 1f;

            public WorldBar(
                string key,
                Func<Vector2> getWorldPosition,
                Color baseColor,
                Color fillColor,
                float fillPercent,
                int timeLeft,
                int style,
                Vector2 offset)
            {
                Key = key;
                GetWorldPosition = getWorldPosition;
                BaseColor = baseColor;
                FillColor = fillColor;
                FillPercent = MathHelper.Clamp(fillPercent, 0f, 1f);
                DisplayedFillPercent = FillPercent;
                TimeLeft = timeLeft;
                MaxTimeLeft = timeLeft;
                Style = style;
                Offset = offset;
            }

            public float GetFadeOpacity()
            {
                float fadeIn = FadeInTime <= 0 ? 1f : Utils.GetLerpValue(0f, FadeInTime, MaxTimeLeft - TimeLeft, true);
                float fadeOut = FadeOutTime <= 0 ? 1f : Utils.GetLerpValue(0f, FadeOutTime, TimeLeft, true);
                return fadeIn * fadeOut;
            }
        }

        private static readonly Dictionary<string, WorldBar> ActiveBars = new();

        /// <summary>
        /// Creates or refreshes a bar with the given key.
        /// Reusing the same key updates the existing bar instead of creating duplicates.
        /// </summary>
        public static void SetBar(
            string key,
            Func<Vector2> getWorldPosition,
            Color baseColor,
            Color fillColor,
            float percent,
            int showTime = 120,
            int style = 0,
            Vector2 offset = default,
            bool smoothFill = true,
            float smoothingSpeed = 0.18f,
            float scale = 1f)
        {
            percent = MathHelper.Clamp(percent, 0f, 1f);

            if (ActiveBars.TryGetValue(key, out WorldBar existing))
            {
                existing.GetWorldPosition = getWorldPosition;
                existing.BaseColor = baseColor;
                existing.FillColor = fillColor;
                existing.FillPercent = percent;
                existing.TimeLeft = showTime;
                existing.MaxTimeLeft = showTime;
                existing.Style = style;
                existing.Offset = offset;
                existing.Active = true;
                existing.SmoothFill = smoothFill;
                existing.SmoothingSpeed = smoothingSpeed;
                existing.Scale = scale;
                return;
            }

            ActiveBars[key] = new WorldBar(
                key,
                getWorldPosition,
                baseColor,
                fillColor,
                percent,
                showTime,
                style,
                offset)
            {
                SmoothFill = smoothFill,
                SmoothingSpeed = smoothingSpeed,
                Scale = scale
            };
        }

        public static void RemoveBar(string key)
        {
            ActiveBars.Remove(key);
        }

        public static bool HasBar(string key) => ActiveBars.ContainsKey(key);

        public override void UpdateUI(GameTime gameTime)
        {
            List<string> toRemove = null;

            foreach (var pair in ActiveBars)
            {
                WorldBar bar = pair.Value;

                if (!bar.Active)
                {
                    toRemove ??= new();
                    toRemove.Add(pair.Key);
                    continue;
                }

                bar.TimeLeft--;

                if (bar.SmoothFill)
                    bar.DisplayedFillPercent = MathHelper.Lerp(bar.DisplayedFillPercent, bar.FillPercent, bar.SmoothingSpeed);
                else
                    bar.DisplayedFillPercent = bar.FillPercent;

                if (bar.TimeLeft <= 0)
                {
                    toRemove ??= new();
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (string key in toRemove)
                    ActiveBars.Remove(key);
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (ActiveBars.Count == 0)
                return;

            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Entity Health Bars"));
            if (index == -1)
                return;

            layers.Insert(index, new LegacyGameInterfaceLayer(
                $"{Mod.Name}: World Bars",
                DrawBars,
                InterfaceScaleType.UI));
        }

        private bool DrawBars()
        {
            foreach (var pair in ActiveBars)
            {
                WorldBar bar = pair.Value;

                if (!bar.Active || bar.GetWorldPosition is null)
                    continue;

                Texture2D frameTex = TextureAssets.MagicPixel.Value;//AssetDirectory.Textures.Bars.Bar[bar.Style].Value;
                Texture2D fillTex = TextureAssets.MagicPixel.Value;

                Vector2 worldPos = bar.GetWorldPosition.Invoke();
                Vector2 drawPos = worldPos - Main.screenPosition + bar.Offset;

                drawPos -= new Vector2(frameTex.Width * 0.5f, frameTex.Height + 8f);

                float opacity = bar.GetFadeOpacity();
                float clampedFill = MathHelper.Clamp(bar.DisplayedFillPercent, 0f, 1f);

                int fillWidth = clampedFill >= 0.999f
                    ? fillTex.Width
                    : (int)(fillTex.Width * clampedFill);
                  
                Rectangle fillFrame = new Rectangle(0, 0, fillWidth, fillTex.Height);

                Main.spriteBatch.Draw(
                    frameTex,
                    drawPos,
                    null,
                    bar.BaseColor * opacity,
                    0f,
                    Vector2.Zero,
                    bar.Scale,
                    SpriteEffects.None,
                    0f);

                Main.spriteBatch.Draw(
                    fillTex,
                    drawPos,
                    fillFrame,
                    bar.FillColor * opacity,
                    0f,
                    Vector2.Zero,
                    bar.Scale,
                    SpriteEffects.None,
                    0f);
            }

            return true;
        }
    }
}

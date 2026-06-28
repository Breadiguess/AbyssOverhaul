using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Rarities
{
    internal class AbyssalRarityItem : GlobalItem
    {
        public override bool InstancePerEntity => true;


        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.rare == ModContent.RarityType<AbyssalRarity>();
        }


        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (line.Mod != "Terraria" || line.Name != "ItemName")
            {
                return true;
            }

            var text = item.AffixName();
            var position = new Vector2(line.X, line.Y);


            var font = FontAssets.MouseText.Value;
            var size = font.MeasureString(text);

            var offset = size / 2f;

            var center = position + offset;


            DrawText(in position, text);

            return false;
        }


        private static readonly Color[] PulseColors =
        [
             Color.DeepSkyBlue,
            Color.Purple,
        ];

        private static void DrawText(in Vector2 position, string text)
        {
            var font = FontRegistry.BlackSide;
            var batch = Main.spriteBatch;

            const float baseScale = 1.25f;
            const float cycleDuration = 3f;
            const float transitionWidth = 78f;

            float time = Main.GlobalTimeWrappedHourly;

            float cycleRaw = time / cycleDuration;
            int cycleIndex = (int)MathF.Floor(cycleRaw);
            float cycleProgress = cycleRaw - cycleIndex;

            Color oldColor = PulseColors[cycleIndex % PulseColors.Length];
            Color newColor = PulseColors[(cycleIndex + 1) % PulseColors.Length];

            float totalWidth = font.MeasureString(text).X * baseScale;
            float centerX = position.X + totalWidth * 0.5f;
            float maxDistance = MathF.Max(totalWidth * 0.5f, 1f);

            float rawProgress = cycleProgress;

            float easedProgress = rawProgress * rawProgress * rawProgress *
                                  (rawProgress * (rawProgress * 6f - 15f) + 10f);

            float startDistance = -transitionWidth;
            float endDistance = maxDistance + transitionWidth;

            float frontDistance = MathHelper.Lerp(startDistance, endDistance, easedProgress);

            var cursor = position;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                default,
                Main.graphics.GraphicsDevice.RasterizerState,
                null,
                Main.UIScaleMatrix
            );

            for (int i = 0; i < text.Length; i++)
            {
                string letter = text[i].ToString();

                float letterWidth = font.MeasureString(letter).X;
                bool empty = string.IsNullOrWhiteSpace(letter);

                float letterCenterX = cursor.X + letterWidth * baseScale * 0.5f;
                float distanceFromCenter = MathF.Abs(letterCenterX - centerX);
                
                float reached = frontDistance - distanceFromCenter;

                float swapAmount = MathHelper.Clamp(reached / transitionWidth, 0f, 1f);
                swapAmount = MathHelper.SmoothStep(0f, 1f, swapAmount);

                Color color = Color.Lerp(oldColor, newColor, swapAmount);
                color.A = 255;

                float bandCenterDistance = MathF.Abs(reached);

                float bandAmount = 1f - bandCenterDistance / transitionWidth;
                bandAmount = MathHelper.Clamp(bandAmount, 0f, 1f);

                float birthFade = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(rawProgress / 0.18f, 0f, 1f));

                bandAmount *= birthFade;
                bandAmount = MathHelper.Clamp(bandAmount, 0f, 1f);

                float wave = MathF.Cos(i * 0.35f + time * 3f);
                float scale = baseScale + wave * 0.01f;

                if (!empty)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        float angle = x / 10f * MathHelper.TwoPi + time * 2f;
                        Vector2 glowOffset = new Vector2(1f).RotatedBy(angle) * (1f + bandAmount * 3f);

                        Utils.DrawBorderStringFourWay(
                            batch,
                            font,
                            letter,
                            cursor.X + glowOffset.X,
                            cursor.Y + glowOffset.Y + wave,
                            color,
                            Color.Black,
                            Vector2.Zero,
                            scale
                        );
                    }
                }

                cursor.X += letterWidth * scale;
            }

            Main.spriteBatch.ResetToDefaultUI();

            cursor = position;

            for (int i = 0; i < text.Length; i++)
            {
                string letter = text[i].ToString();

                float letterWidth = font.MeasureString(letter).X;
                bool empty = string.IsNullOrWhiteSpace(letter);

                float letterCenterX = cursor.X + letterWidth * baseScale * 0.5f;
                float distanceFromCenter = MathF.Abs(letterCenterX - centerX);

                float reached = frontDistance - distanceFromCenter;

                float swapAmount = MathHelper.Clamp(reached / transitionWidth, 0f, 1f);
                swapAmount = MathHelper.SmoothStep(0f, 1f, swapAmount);

                Color color = Color.Lerp(oldColor, newColor, swapAmount);
                color.A = 255;

                float wave = MathF.Cos(i * 0.35f + time * 3f);
                float scale = baseScale + wave * 0.01f;

                if (!empty)
                {
                    Utils.DrawBorderStringFourWay(
                        batch,
                        font,
                        letter,
                        cursor.X,
                        cursor.Y + wave,
                        Color.Black,
                        Color.Transparent,
                        Vector2.Zero,
                        scale
                    );
                }

                cursor.X += letterWidth * scale;
            }
        }
    }
}

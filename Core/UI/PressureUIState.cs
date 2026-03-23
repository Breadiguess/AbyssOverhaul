using AbyssOverhaul.Core.ModPlayers;
using Terraria.GameContent;
using Terraria.UI;
namespace AbyssOverhaul.Core.UI
{

    namespace AbyssOverhaul.Core.UI
    {
        public class PressureUIState : UIState
        {
            private float visibility;
            private float displayedEffectivePressure;
            private float displayedEffectiveStress;
            private float displayedResidue;

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (Main.gameMenu)
                {
                    visibility = 0f;
                    return;
                }

                Player player = Main.LocalPlayer;
                if (player is null || !player.active)
                {
                    visibility = 0f;
                    return;
                }

                PressurePlayer pressure = player.GetModPlayer<PressurePlayer>();
                bool shouldShow = pressure.InPressureZone || pressure.PressureResidue > 0.5f;

                float targetVisibility = shouldShow ? 1f : 0f;
                visibility = MathHelper.Lerp(visibility, targetVisibility, shouldShow ? 0.12f : 0.08f);

                displayedEffectivePressure = MathHelper.Lerp(displayedEffectivePressure, pressure.EffectiveAmbientPressure, 0.10f);
                displayedEffectiveStress = MathHelper.Lerp(displayedEffectiveStress, pressure.EffectivePressureStress, 0.12f);
                displayedResidue = MathHelper.Lerp(displayedResidue, pressure.PressureResidue, 0.08f);
            }

            public override void Draw(SpriteBatch spriteBatch)
            {
                if (visibility <= 0.01f || Main.gameMenu)
                    return;

                Player player = Main.LocalPlayer;
                if (player is null || !player.active)
                    return;

                PressurePlayer pressure = player.GetModPlayer<PressurePlayer>();
                PressureDangerInfo danger = PressureUIHelper.GetDangerInfo(pressure.EffectivePressureStress);

                int panelWidth = 320;
                int panelHeight = 118;

                int left = Main.screenWidth - panelWidth - 404;
                int top = Main.playerInventory ? 260 : 96;

                Rectangle panel = new(left, top, panelWidth, panelHeight);
                Rectangle pressureBar = new(left + 12, top + 48, panelWidth - 24, 14);
                Rectangle dangerBar = new(left + 12, top + 84, panelWidth - 24, 14);

                DrawPanel(spriteBatch, panel, new Color(10, 16, 28) * visibility, new Color(70, 92, 120) * visibility);

                Utils.DrawBorderString(spriteBatch, "ABYSSAL PRESSURE", new Vector2(left + 12f, top + 10f), Color.White * visibility, 0.72f);
                Utils.DrawBorderString(spriteBatch, danger.Name, new Vector2(left + 210f, top + 10f), danger.Color * visibility, 0.72f);

                string pressureText =
                    $"Eff {displayedEffectivePressure:0}   Raw {pressure.AmbientPressure:0}   Res {pressure.PressureResistance:0}   Adapt {pressure.Adaptation:0}";
                Utils.DrawBorderString(spriteBatch, pressureText, new Vector2(left + 12f, top + 28f), Color.White * visibility, 0.60f);

                DrawPressureBar(spriteBatch, pressureBar, pressure);

                string dangerText =
                    $"Stress {pressure.PressureStress:0}   Residue {displayedResidue:0}";
                Utils.DrawBorderString(spriteBatch, dangerText, new Vector2(left + 12f, top + 64f), Color.White * visibility, 0.60f);

                DrawDangerBar(spriteBatch, dangerBar, pressure, danger);

                string footer =
                    pressure.InPressureZone
                    ? (danger.DefenseLoss > 0
                        ? $"-{danger.DefenseLoss} defense active"
                        : "No active pressure penalty")
                    : (pressure.PressureResidue > 0.5f
                        ? "Residual decompression danger"
                        : "Pressure stable");

                Utils.DrawBorderString(spriteBatch, footer, new Vector2(left + 12f, top + 100f), danger.Color * visibility, 0.58f);
            }

            private void DrawPressureBar(SpriteBatch spriteBatch, Rectangle rect, PressurePlayer pressure)
            {
                DrawBarBack(spriteBatch, rect);

                float effectivePressurePercent = MathHelper.Clamp(displayedEffectivePressure / PressureUIHelper.AmbientPressureBarMax, 0f, 1f);
                Color pressureColor = Color.Lerp(new Color(70, 150, 255), new Color(235, 85, 85), effectivePressurePercent) * visibility;

                DrawFill(spriteBatch, rect, effectivePressurePercent, pressureColor);

                // Raw ambient marker (red)
                DrawMarker(spriteBatch, rect, pressure.AmbientPressure / PressureUIHelper.AmbientPressureBarMax, new Color(255, 110, 110) * visibility);

                // Resistance marker (green)
                DrawMarker(spriteBatch, rect, pressure.PressureResistance / PressureUIHelper.AmbientPressureBarMax, new Color(100, 255, 140) * visibility);

                // Adaptation marker (white)
                DrawMarker(spriteBatch, rect, pressure.Adaptation / PressureUIHelper.AmbientPressureBarMax, Color.White * visibility);

                DrawBorder(spriteBatch, rect, new Color(90, 110, 130) * visibility);
            }

            private void DrawDangerBar(SpriteBatch spriteBatch, Rectangle rect, PressurePlayer pressure, PressureDangerInfo danger)
            {
                DrawBarBack(spriteBatch, rect);

                DrawZone(spriteBatch, rect, 0f, 10f, new Color(70, 140, 255) * (0.35f * visibility));
                DrawZone(spriteBatch, rect, 10f, 25f, new Color(160, 220, 90) * (0.35f * visibility));
                DrawZone(spriteBatch, rect, 25f, 45f, new Color(255, 215, 90) * (0.35f * visibility));
                DrawZone(spriteBatch, rect, 45f, 70f, new Color(255, 120, 60) * (0.35f * visibility));
                DrawZone(spriteBatch, rect, 70f, 80f, new Color(255, 70, 70) * (0.35f * visibility));

                float dangerFill = MathHelper.Clamp(displayedEffectiveStress / PressureUIHelper.DangerBarMax, 0f, 1f);
                DrawFill(spriteBatch, rect, dangerFill, danger.Color * (0.85f * visibility));

                DrawThresholdTick(spriteBatch, rect, 10f);
                DrawThresholdTick(spriteBatch, rect, 25f);
                DrawThresholdTick(spriteBatch, rect, 45f);
                DrawThresholdTick(spriteBatch, rect, 70f);

                // Actual stress marker
                DrawMarker(spriteBatch, rect, pressure.PressureStress / PressureUIHelper.DangerBarMax, Color.White * visibility);

                // Residue marker
                DrawMarker(spriteBatch, rect, pressure.PressureResidue / PressureUIHelper.DangerBarMax, new Color(210, 120, 255) * visibility);

                DrawBorder(spriteBatch, rect, new Color(90, 110, 130) * visibility);
            }

            private void DrawZone(SpriteBatch spriteBatch, Rectangle rect, float min, float max, Color color)
            {
                float start = MathHelper.Clamp(min / PressureUIHelper.DangerBarMax, 0f, 1f);
                float end = MathHelper.Clamp(max / PressureUIHelper.DangerBarMax, 0f, 1f);

                int x = rect.X + (int)(rect.Width * start);
                int width = Math.Max(1, (int)(rect.Width * (end - start)));

                DrawRect(spriteBatch, new Rectangle(x, rect.Y, width, rect.Height), color);
            }

            private void DrawThresholdTick(SpriteBatch spriteBatch, Rectangle rect, float value)
            {
                float percent = MathHelper.Clamp(value / PressureUIHelper.DangerBarMax, 0f, 1f);
                int x = rect.X + (int)(rect.Width * percent) - 1;
                DrawRect(spriteBatch, new Rectangle(x, rect.Y - 1, 2, rect.Height + 2), Color.Black * (0.6f * visibility));
            }

            private void DrawBarBack(SpriteBatch spriteBatch, Rectangle rect)
            {
                DrawRect(spriteBatch, rect, new Color(20, 28, 40) * (0.95f * visibility));
            }

            private void DrawFill(SpriteBatch spriteBatch, Rectangle rect, float percent, Color color)
            {
                int fillWidth = (int)(rect.Width * MathHelper.Clamp(percent, 0f, 1f));
                if (fillWidth <= 0)
                    return;

                DrawRect(spriteBatch, new Rectangle(rect.X, rect.Y, fillWidth, rect.Height), color);
            }

            private void DrawMarker(SpriteBatch spriteBatch, Rectangle rect, float percent, Color color)
            {
                percent = MathHelper.Clamp(percent, 0f, 1f);
                int x = rect.X + (int)(rect.Width * percent) - 1;
                DrawRect(spriteBatch, new Rectangle(x, rect.Y - 2, 2, rect.Height + 4), color);
            }

            private void DrawPanel(SpriteBatch spriteBatch, Rectangle rect, Color fillColor, Color borderColor)
            {
                DrawRect(spriteBatch, rect, fillColor);
                DrawBorder(spriteBatch, rect, borderColor);
            }

            private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
            {
                DrawRect(spriteBatch, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
                DrawRect(spriteBatch, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
                DrawRect(spriteBatch, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
                DrawRect(spriteBatch, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
            }

            private void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
            {
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, rect, color);
            }
        }
    }
}

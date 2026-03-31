using AbyssOverhaul.Core.Ecosystem.Simulation.AbyssOverhaul.Core.Ecosystem.Persistence;
using Terraria.GameContent;


namespace AbyssOverhaul.Core.Ecosystem.Simulation
{


    internal sealed class EcologyDebugOverlaySystem : ModSystem
    {
        public static bool Visible = false;

        public static int VisibleRadiusX = 4;
        public static int VisibleRadiusY = 3;
        public static int CellPixelSize = 72;

        public static Vector2 PanelOrigin = new(36f, 120f);

        private static Texture2D _pixel;

        public override void Load()
        {
            On_Main.DrawInterface += DrawOverlayHook;
        }

        public override void Unload()
        {
            On_Main.DrawInterface -= DrawOverlayHook;
            _pixel = null;
        }

        private void DrawOverlayHook(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
        {
            if (!Visible || Main.gameMenu || Main.LocalPlayer is null || !Main.LocalPlayer.active)
            {

                orig(self, gameTime);
                return;
            }
            if (EcologySystem.Instance is null)
            {
                orig(self, gameTime);
                return;
            }

            _pixel ??= TextureAssets.MagicPixel.Value;

            SpriteBatch spriteBatch = Main.spriteBatch;

            spriteBatch.ResetToDefaultUI(false);
            DrawOverlay(spriteBatch);
            spriteBatch.End();




            orig(self, gameTime);

        }

        private void DrawOverlay(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            Point centerCell = EcologyMath.WorldToCell(player.Center);

            EcologyCell hoveredCell = null;
            Rectangle hoveredRect = Rectangle.Empty;

            Rectangle gridBounds = GetGridBounds();

            DrawPanelBackdrop(spriteBatch, gridBounds, new Color(12, 16, 24, 210), Color.White * 0.18f);

            for (int y = -VisibleRadiusY; y <= VisibleRadiusY; y++)
            {
                for (int x = -VisibleRadiusX; x <= VisibleRadiusX; x++)
                {
                    Point coord = new(centerCell.X + x, centerCell.Y + y);
                    Rectangle rect = GetCellScreenRect(coord, centerCell, PanelOrigin);

                    EcologyCell cell = EcologySystem.Instance.GetOrCreateDebugCell(coord);
                    bool hovered = rect.Contains((Main.MouseWorld-Main.screenPosition).ToPoint());

                    DrawCell(spriteBatch, coord, cell, rect, hovered, coord == centerCell);

                    if (hovered)
                    {
                        hoveredCell = cell;
                        hoveredRect = rect;
                    }
                }
            }

            DrawLegend(spriteBatch, gridBounds);

            if (hoveredCell is not null)
                DrawInspectorPanel(spriteBatch, hoveredCell, hoveredRect);
        }

        private Rectangle GetGridBounds()
        {
            int width = (VisibleRadiusX * 2 + 1) * CellPixelSize;
            int height = (VisibleRadiusY * 2 + 1) * CellPixelSize;

            return new Rectangle(
                (int)PanelOrigin.X - 8,
                (int)PanelOrigin.Y - 24,
                width + 16,
                height + 32);
        }

        private Rectangle GetCellScreenRect(Point coord, Point centerCoord, Vector2 origin)
        {
            int relativeX = coord.X - centerCoord.X + VisibleRadiusX;
            int relativeY = coord.Y - centerCoord.Y + VisibleRadiusY;

            return new Rectangle(
                (int)origin.X + relativeX * CellPixelSize,
                (int)origin.Y + relativeY * CellPixelSize,
                CellPixelSize,
                CellPixelSize);
        }

        private void DrawCell(SpriteBatch spriteBatch, Point coord, EcologyCell cell, Rectangle rect, bool hovered, bool isCenterCell)
        {
            Color fill = GetCellFillColor(cell);
            Color border = GetCellBorderColor(cell, hovered, isCenterCell);

            DrawRectFilled(spriteBatch, rect, fill);
            DrawRectOutline(spriteBatch, rect, border, 2);

            Rectangle innerRect = InflateRect(rect, -4);

            DrawActorsInCell(spriteBatch, cell, innerRect);

            string actorCountText = $"{cell.ActorIDs.Count}";
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                actorCountText,
                rect.X + 4,
                rect.Y + 2,
                Color.White,
                Color.Black,
                Vector2.Zero);

            string coordText = $"{coord.X},{coord.Y}";
            Vector2 size = FontAssets.MouseText.Value.MeasureString(coordText) * 0.55f;
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                coordText,
                rect.Right - size.X - 4f,
                rect.Bottom - size.Y - 2f,
                Color.Silver,
                Color.Black,
                Vector2.Zero, 0.55f);

            if (hovered)
                DrawRectOutline(spriteBatch, InflateRect(rect, 2), Color.White, 2);
        }

        private void DrawActorsInCell(SpriteBatch spriteBatch, EcologyCell cell, Rectangle rect)
        {
            List<EcologyActor> actors = EcologySystem.Instance.EnumerateActorsInCell(cell).ToList();
            if (actors.Count == 0)
                return;

            int maxIconsToDraw = 16;
            int drawCount = Math.Min(maxIconsToDraw, actors.Count);

            int iconSize = actors.Count switch
            {
                <= 1 => 30,
                <= 4 => 20,
                <= 9 => 14,
                _ => 10
            };

            int columns = Math.Max(1, rect.Width / iconSize);
            int rows = Math.Max(1, rect.Height / iconSize);
            int capacity = columns * rows;
            drawCount = Math.Min(drawCount, capacity);

            for (int i = 0; i < drawCount; i++)
            {
                EcologyActor actor = actors[i];

                int col = i % columns;
                int row = i / columns;
                Rectangle iconRect = new(
                    rect.X + col * iconSize,
                    rect.Y + row * iconSize,
                    iconSize,
                    iconSize);

                DrawActorIcon(spriteBatch, actor, iconRect);
            }

            if (actors.Count > drawCount)
            {
                string extra = $"+{actors.Count - drawCount}";
                Vector2 size = FontAssets.MouseText.Value.MeasureString(extra) * 0.55f;
                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    extra,
                    rect.Right - size.X - 2f,
                    rect.Bottom - size.Y,
                    Color.Yellow,
                    Color.Black,Vector2.zeroVector, 0.55f
                    );
            }
        }

        private void DrawActorIcon(SpriteBatch spriteBatch, EcologyActor actor, Rectangle iconRect)
        {
            if (actor.SpeciesID < 0 || actor.SpeciesID >= TextureAssets.Npc.Length)
            {
                DrawRectFilled(spriteBatch, iconRect, Color.Magenta * 0.7f);
                return;
            }

            Texture2D tex = TextureAssets.Npc[actor.SpeciesID].Value;
            if (tex is null)
            {
                DrawRectFilled(spriteBatch, iconRect, Color.Magenta * 0.7f);
                return;
            }

            int frameCount = 1;
            if (actor.SpeciesID >= 0 && actor.SpeciesID < Main.npcFrameCount.Length && Main.npcFrameCount[actor.SpeciesID] > 0)
                frameCount = Main.npcFrameCount[actor.SpeciesID];

            int frameHeight = tex.Height / Math.Max(frameCount, 1);
            Rectangle source = new(0, 0, tex.Width, frameHeight);

            Vector2 sourceSize = new(source.Width, source.Height);
            float scale = MathF.Min(iconRect.Width / sourceSize.X, iconRect.Height / sourceSize.Y);
            Vector2 drawPos = iconRect.Center.ToVector2();
            Vector2 origin = sourceSize * 0.5f;

            Color tint = GetActorTint(actor);

            spriteBatch.Draw(tex, drawPos, source, tint, 0f, origin, scale, SpriteEffects.None, 0f);

            Color outline = GetActorOutlineColor(actor);
            DrawRectOutline(spriteBatch, iconRect, outline, 1);
        }

        private void DrawInspectorPanel(SpriteBatch spriteBatch, EcologyCell cell, Rectangle hoveredRect)
        {
            Rectangle panel = new(
                hoveredRect.Right + 18,
                hoveredRect.Top,
                360,
                360);

            if (panel.Right > Main.screenWidth - 20)
                panel.X = hoveredRect.Left - panel.Width - 18;

            if (panel.X < 12)
                panel.X = 12;

            if (panel.Bottom > Main.screenHeight - 12)
                panel.Y = Main.screenHeight - panel.Height - 12;


            int maxActorLines = 12;
            panel.Height += (cell.ActorIDs.Count < maxActorLines ? cell.ActorIDs.Count: maxActorLines+1) *11;
            DrawRectFilled(spriteBatch, panel, new Color(10, 14, 22, 235));
            DrawRectOutline(spriteBatch, panel, Color.White * 0.35f, 2);

            int x = panel.X + 10;
            int y = panel.Y + 8;
            int line = 0;

            void DrawLine(string text, Color color, float scale = 0.8f)
            {
                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    text,
                    x,
                    y + line * 18,
                    color,
                    Color.Black,
                    new Vector2(),scale);
                line++;
            }

            DrawLine("Ecology Cell", Color.White, 0.95f);
            DrawLine($"Coord: {cell.Coord.X}, {cell.Coord.Y}", Color.Silver);
            DrawLine($"WorldBounds: {cell.WorldBounds}", Color.Silver);
            DrawLine($"Actors: {cell.ActorIDs.Count}", Color.White);
            DrawLine($"Population Buckets: {cell.Populations.Count}", Color.White);
            DrawLine($"PlantFood: {cell.PlantFood:0.00}", Color.LightGreen);
            DrawLine($"MeatFood: {cell.MeatFood:0.00}", Color.IndianRed);
            DrawLine($"Carrion: {cell.Carrion:0.00}", Color.Peru);
            DrawLine($"Shelter: {cell.Shelter:0.00}", Color.LightBlue);
            DrawLine($"Threat: {cell.Threat:0.00}", Color.Orange);
            DrawLine($"HabitatQuality: {cell.HabitatQuality:0.00}", Color.LightSkyBlue);
            DrawLine($"LastSimTime: {cell.LastSimulatedTime:0.##}", Color.Gray);

            line++;
            DrawLine("Actors:", Color.White, 0.9f);

            int actorLinesStart = line;

            

            List<EcologyActor> actors = EcologySystem.Instance.EnumerateActorsInCell(cell).ToList();

            for (int i = 0; i < actors.Count && i < maxActorLines; i++)
            {
                EcologyActor actor = actors[i];
                string text =
                    $"#{actor.ActorID} S:{actor.SpeciesID} {actor.Intent} " +
                    $"H: {actor.Hunger}/{actor.MaxHungerSpecies}, " +
                    $"Loaded: {(actor.IsLoaded ? "Y" : "N")}, " +
                    $"Alive: {(actor.Alive ? "Y" : "N")}";

                DrawLine(text, GetActorTint(actor), 0.7f);
            }

            if (actors.Count > maxActorLines)
                DrawLine($"+{actors.Count - maxActorLines} more...", Color.Yellow, 0.7f);

            int iconPanelTop = panel.Y + Math.Max(230, (actorLinesStart + maxActorLines + 1) * 18);
            Rectangle iconStrip = new(panel.X + 8, iconPanelTop, panel.Width - 16, panel.Bottom - iconPanelTop - 8);

            if (iconStrip.Height > 24)
                DrawActorStrip(spriteBatch, actors, iconStrip);
        }

        private void DrawActorStrip(SpriteBatch spriteBatch, List<EcologyActor> actors, Rectangle rect)
        {
            DrawRectFilled(spriteBatch, rect, new Color(255, 255, 255, 10));
            DrawRectOutline(spriteBatch, rect, Color.White * 0.15f, 1);

            if (actors.Count == 0)
                return;

            int iconSize = Math.Min(32, rect.Height - 8);
            if (iconSize <= 4)
                return;

            int x = rect.X + 4;
            int y = rect.Y + 4;

            for (int i = 0; i < actors.Count; i++)
            {
                Rectangle iconRect = new(x, y, iconSize, iconSize);
                if (iconRect.Right > rect.Right - 4)
                    break;

                DrawActorIcon(spriteBatch, actors[i], iconRect);
                x += iconSize + 4;
            }
        }

        private void DrawLegend(SpriteBatch spriteBatch, Rectangle gridBounds)
        {
            int x = gridBounds.X + 8;
            int y = gridBounds.Y - 20;

            Utils.DrawBorderStringFourWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                "Ecology Debug Grid",
                x,
                y,
                Color.White,
                Color.Black,
                Vector2.Zero, 0.8f);
        }

        private Color GetCellFillColor(EcologyCell cell)
        {
            float threat = MathHelper.Clamp(cell.Threat, 0f, 1f);
            float plant = MathHelper.Clamp(cell.PlantFood / 100f, 0f, 1f);
            float carrion = MathHelper.Clamp(cell.Carrion / 100f, 0f, 1f);

            Color baseColor = new(18, 24, 34, 200);

            baseColor = Color.Lerp(baseColor, new Color(40, 85, 40, 220), plant * 0.35f);
            baseColor = Color.Lerp(baseColor, new Color(105, 55, 40, 220), carrion * 0.35f);
            baseColor = Color.Lerp(baseColor, new Color(100, 35, 35, 220), threat * 0.45f);

            return baseColor;
        }

        private Color GetCellBorderColor(EcologyCell cell, bool hovered, bool isCenterCell)
        {
            if (hovered)
                return Color.White;

            if (isCenterCell)
                return Color.Cyan;

            if (cell.Threat >= 0.66f)
                return Color.OrangeRed;

            if (cell.ActorIDs.Count > 0)
                return Color.LightGray;

            return new Color(80, 90, 110);
        }

        private Color GetActorTint(EcologyActor actor)
        {
            if (!actor.Alive)
                return new Color(140, 45, 45);

            return actor.Intent switch
            {
                EcologyIntent.Hunting => Color.Orange,
                EcologyIntent.Fleeing => Color.Cyan,
                EcologyIntent.Resting => Color.SlateBlue,
                EcologyIntent.Feeding => Color.LightGreen,
                EcologyIntent.Scavenging => Color.Goldenrod,
                _ => actor.IsLoaded ? Color.White : new Color(170, 185, 205)
            };
        }

        private Color GetActorOutlineColor(EcologyActor actor)
        {
            if (!actor.Alive)
                return Color.DarkRed;

            return actor.IsLoaded ? Color.White * 0.5f : Color.Black * 0.5f;
        }

        private static Rectangle InflateRect(Rectangle rect, int amount)
        {
            return new Rectangle(
                rect.X - amount,
                rect.Y - amount,
                rect.Width + amount * 2,
                rect.Height + amount * 2);
        }

        private void DrawPanelBackdrop(SpriteBatch spriteBatch, Rectangle rect, Color fill, Color border)
        {
            DrawRectFilled(spriteBatch, rect, fill);
            DrawRectOutline(spriteBatch, rect, border, 2);
        }

        private void DrawRectFilled(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.Draw(_pixel, rect, color);
        }

        private void DrawRectOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }

    internal static class EcologyDebugOverlayExtensions
    {
        public static EcologyCell GetOrCreateDebugCell(this EcologySystem system, Point coord)
        {
            if (system.Cells.TryGetValue(coord, out EcologyCell cell))
                return cell;

            return new EcologyCell
            {
                Coord = coord,
                WorldBounds = EcologyMath.CellToWorldBounds(coord),
                PlantFood = 0f,
                MeatFood = 0f,
                Carrion = 0f,
                Shelter = 0f,
                Threat = 0f,
                HabitatQuality = 0f,
                LastSimulatedTime = 0d
            };
        }
    }

}

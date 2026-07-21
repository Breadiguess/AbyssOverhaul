using AbyssOverhaul.Core.Subworlds.TransitionScreen;
using AbyssOverhaul.Core.Subworlds.WorldGen;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using BreadLibrary.Core.Graphics.Spritebatch;
using SubworldLibrary;
using Terraria.GameContent;

namespace AbyssOverhaul.Core.Subworlds
{
    internal class AbyssSubworld : Subworld
    {

        public override int Width => 1000;
        public override int Height => 2400;

        public override bool ShouldSave => true;

        public override void Load()
        {
            On_WorldGen.oceanDepths += FixOceanWaterTakingPriority;

        }
        private bool FixOceanWaterTakingPriority(On_WorldGen.orig_oceanDepths orig, int x, int y) =>
            orig(x, y) && !SubworldSystem.IsActive<AbyssSubworld>();

        private Player? _menuPlayerClone;
        private int _menuPlayerSource = -1;
        private Player GetOrCreateMenuPlayerClone(Player source)
        {
            if (_menuPlayerClone is not null &&
                _menuPlayerSource == source.whoAmI)
            {
                return _menuPlayerClone;
            }

            // Expensive operation, but now it only happens once per transition.
            _menuPlayerClone = source.SerializedClone();
            _menuPlayerSource = source.whoAmI;

            Player clone = _menuPlayerClone;

            clone.isDisplayDollOrInanimate = true;
            clone.dead = false;
            clone.ghost = false;

            clone.honeyWet = false;
            clone.lavaWet = false;

            clone.UpdateDyes();

            return clone;
        }

        private float _layer1ScrollY;
        private float _layer2ScrollY;
        private float _layer3ScrollY;


        private readonly List<Bubble> _bubbles = new();

        private float _bubbleSpawnTimer;
        private bool _bubblesInitialized;

        private const int MaximumBubbles = 55;
        public override void DrawMenu(GameTime gameTime)
        {

            Player source = Main.LocalPlayer;

            var tex = TextureAssets.MagicPixel.Value;
            Main.EntitySpriteDraw(tex, Vector2.zeroVector, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(12, 41, 41), 0, Vector2.zeroVector, 1, SpriteEffects.None);



                MenuSwimCloneSystem.UpdateMenuPlayer(gameTime);
            if (source is null || !source.active || !MenuSwimCloneSystem.Initialized)
                return;


            UpdateLoopingLayers(gameTime);

            //todo: draw Background
            DrawLoopingLayer3();
            DrawLoopingLayer2();
            DrawPlayer(source);
            UpdateBubbles(gameTime);
            DrawBubbles(foreground: false);

            DrawLoopingLayer1();
            if (source is not null &&
                source.active &&
                MenuSwimCloneSystem.Initialized)
            {
                DrawPlayer(source);
            }
            DrawBubbles(foreground: true);
            DrawGradient();

            RenderDebugText(gameTime);
            
        }

        private void UpdateBubbles(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            deltaTime = Math.Min(deltaTime, 1f / 20f);

            /*
             * Populate the screen immediately when the menu opens.
             * Otherwise, every bubble would initially have to enter from the bottom.
             */
            if (!_bubblesInitialized)
            {
                _bubblesInitialized = true;

                for (int i = 0; i < 24; i++)
                {
                    SpawnBubble(
                        Main.rand.NextFloat(0f, Main.screenHeight)
                    );
                }
            }

            _bubbleSpawnTimer -= deltaTime;

            if (_bubbleSpawnTimer <= 0f && _bubbles.Count < MaximumBubbles)
            {
                int spawnCount = Main.rand.NextBool(5)
                    ? Main.rand.Next(2, 5)
                    : 1;

                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnBubble(
                        Main.screenHeight + Main.rand.NextFloat(10f, 90f)
                    );
                }

                _bubbleSpawnTimer = Main.rand.NextFloat(0.08f, 0.32f);
            }

            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = _bubbles[i];

                bubble.Update(gameTime);

                if (bubble.ShouldBeRemoved)
                    _bubbles.RemoveAt(i);
            }
        }
        private void DrawBubbles(bool foreground)
        {
            foreach (Bubble bubble in _bubbles)
            {
                if (bubble.Foreground == foreground)
                    bubble.Draw(Main.spriteBatch);
            }
        }
        private void SpawnBubble(float y)
        {
            bool foreground = Main.rand.NextBool(4);

            float scale;

            if (foreground)
            {
                scale = Main.rand.NextFloat(0.8f, 1.35f);
            }
            else
            {
                scale = Main.rand.NextFloat(0.3f, 0.85f);
            }

            float riseSpeed = MathHelper.Lerp(
                35f,
                105f,
                MathHelper.Clamp(scale / 1.35f, 0f, 1f)
            )
                * 4;

            Vector2 position = new Vector2(
                Main.rand.NextFloat(-20f, Main.screenWidth + 20f),
                y - 1
            );

            _bubbles.Add(
                new Bubble(
                    position,
                    scale,
                    riseSpeed,
                    foreground
                )
            );
        }
        private void RenderDebugText(GameTime gameTime)
        {
            base.DrawMenu(gameTime);
        }
        private void DrawGradient()
        {
            var tex = Assets.Textures.Extra.Overlay.Asset.Value;
            var cap = Main.spriteBatch.Capture();

            Main.spriteBatch.UseBlendState(BlendState.NonPremultiplied);

            Main.EntitySpriteDraw(tex, Vector2.zeroVector, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White *0.6f, 0, Vector2.zeroVector, 1, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(cap);
        }
        private void DrawLoopingLayer3()
        {
            Texture2D texture = Assets.Textures.Extra.Layer3.Asset.Value;

            DrawVerticallyLoopingLayer(
                texture,
                _layer3ScrollY,
                Color.White
            );
        }

        #region Draw Entrance and Stuff

        private void UpdateLoopingLayers(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds*1.2f;

            const float foregroundSpeed = 100f*5;
            const float midgroundSpeed = 45f*5;

            _layer1ScrollY += foregroundSpeed * deltaTime;
            _layer2ScrollY += midgroundSpeed * deltaTime;

            _layer3ScrollY += midgroundSpeed * deltaTime*0.5f;
        }

        // Foreground.
        private void DrawLoopingLayer1()
        {
            Texture2D texture = Assets.Textures.Extra.Layer1.Asset.Value;

            DrawVerticallyLoopingLayer(
                texture,
                _layer1ScrollY,
                Color.White
            );
        }

        // Midground.
        private void DrawLoopingLayer2()
        {
            Texture2D texture = Assets.Textures.Extra.Layer2.Asset.Value;

            DrawVerticallyLoopingLayer(
                texture,
                _layer2ScrollY,
                Color.White
            );
        }

        private static void DrawVerticallyLoopingLayer(
            Texture2D texture,
            float scrollY,
            Color color)
        {
            if (texture.Width <= 0 || texture.Height <= 0)
                return;

      
      
      
      
            float scale = Main.screenWidth / (float)texture.Width;

            float scaledWidth = texture.Width * scale;
            float scaledHeight = texture.Height * scale;

        
            float wrappedScroll = PositiveModulo(scrollY, scaledHeight);

            float startY = -wrappedScroll - scaledHeight;
            for (
                float drawY = startY;
                drawY < Main.screenHeight + scaledHeight;
                drawY += scaledHeight
            )
            {
                Main.EntitySpriteDraw(
                    texture,
                    new Vector2(0f, MathF.Floor(drawY)),
                    null,
                    color,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private static float PositiveModulo(float value, float modulus)
        {
            float result = value % modulus;

            if (result < 0f)
                result += modulus;

            return result;
        }

        #endregion
        private void DrawPlayer(Player source)
        {
            Player clone = GetOrCreateMenuPlayerClone(source);

            clone.dead = false;
            clone.ghost = false;

            clone.wet = true;
            clone.wetCount = 10;
            clone.honeyWet = false;
            clone.lavaWet = false;

            clone.direction = MenuSwimCloneSystem.Direction;
            clone.velocity = MenuSwimCloneSystem.ScreenVelocity;

            Vector2 bob = new(
                MathF.Cos((float)Main.GlobalTimeWrappedHourly * 1.4f) * 2f,
                MathF.Sin((float)Main.GlobalTimeWrappedHourly * 2.7f) * 3f
            );

            Vector2 screenPosition =
                MenuSwimCloneSystem.ScreenCenter +
                bob -
                clone.Size * 0.5f;

            Vector2 worldPosition = Main.screenPosition + screenPosition;

            clone.position = worldPosition;
            clone.oldPosition = worldPosition - clone.velocity;

            clone.fullRotation = MathHelper.Clamp(
                clone.velocity.Y * 0.045f,
                -0.30f,
                0.30f
            );

            clone.fullRotationOrigin = clone.Size * 0.5f;
            clone.gfxOffY = 0f;

            clone.bodyFrameCounter = MenuSwimCloneSystem.BodyFrameCounter;
            clone.legFrameCounter = MenuSwimCloneSystem.LegFrameCounter;

            clone.ResetEffects();
            clone.ResetVisibleAccessories();
            clone.DisplayDollUpdate();
            clone.UpdateSocialShadow();
            clone.UpdateDyes();
            clone.PlayerFrame();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.Transform
            );

            Main.PlayerRenderer.DrawPlayer(
                Main.Camera,
                clone,
                clone.position,
                clone.fullRotation,
                clone.fullRotationOrigin,
                0f,
                1f
            );

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.UIScaleMatrix
            );
        }
        public override bool NoPlayerSaving => false;
        // Silence the loading screen.
        public override bool ChangeAudio()
        {
            Main.newMusic = 0;
            return true;       
        }

        public static int EntryTileX => ModContent.GetInstance<AbyssSubworld>().Width / 2;
        public const int EntryTileY = 0;


        public override List<GenPass> Tasks => BuildAbyssTasks();

        private static List<GenPass> BuildAbyssTasks()
        {
            List<GenPass> tasks = new()
            {
                new AbyssBootstrapPass(),
                new AbyssSubworldBoundsPass(),
                new AbyssSubworldAbyssPass()
            };

            foreach (var layer in AbyssLayerRegistry.Layers)
            {
                layer.Tasks.Clear();
                layer.ModifyGenTasks();

                foreach (var entry in layer.Tasks)
                {
                    string passName = $"{layer.GetType().Name}: {entry.Key}";

                    tasks.Add(new PassLegacy(
                        passName,
                        (progress, config) => entry.Value(layer, progress, config)
                    ));
                }
            }

            tasks.Add(new PassLegacy("Flood The Sea", (progress, config) =>
            {
                progress.Message = "Flooding the abyss";
                AbyssWorldGenHelper.FloodOpenSpace(
                    AbyssGenUtils.AbyssWorldMinX,
                    AbyssGenUtils.AbyssWorldMaxX,
                    AbyssGenUtils.TopY,
                    AbyssGenUtils.BottomY
                );
            }));

            tasks.Add(new AbyssEntryPocketPass());

            return tasks;
        }

        public override void OnLoad()
        {
            Main.worldSurface = Main.maxTilesY - 42;
            Main.rockLayer = Main.maxTilesY;

        }
    }

}
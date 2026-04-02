using AbyssOverhaul.Core.Subworlds.TransitionScreen;
using AbyssOverhaul.Core.Subworlds.WorldGen;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod;
using SubworldLibrary;

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


        public override void DrawMenu(GameTime gameTime)
        {

            base.DrawMenu(gameTime);
            Player source = Main.LocalPlayer;
            if (source is null || !source.active || !MenuSwimCloneSystem.Initialized)
                return;

            Player clone = source.SerializedClone();
            clone.isDisplayDollOrInanimate = true;

            clone.dead = false;
            clone.ghost = false;
            clone.wet = true;
            clone.wetCount = 10;
            clone.honeyWet = false;
            clone.lavaWet = false;

            clone.direction = MenuSwimCloneSystem.Direction;
            clone.velocity = MenuSwimCloneSystem.ScreenVelocity;

            Vector2 bob =
                new Vector2(
                    MathF.Cos((float)Main.GlobalTimeWrappedHourly * 1.4f) * 2f,
                    MathF.Sin((float)Main.GlobalTimeWrappedHourly * 2.7f) * 3f
                );

            Vector2 screenPos = MenuSwimCloneSystem.ScreenCenter + bob - clone.Size * 0.5f;
            Vector2 worldPos = Main.screenPosition + screenPos;

            clone.position = worldPos;
            clone.oldPosition = worldPos - clone.velocity;
            clone.fullRotation = MathHelper.Clamp(clone.velocity.Y * 0.045f, -0.30f, 0.30f);
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
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.UIScaleMatrix
            );
        }
        public override bool NoPlayerSaving => false;
        // Silence the loading screen.
        public override bool ChangeAudio()
        {
            Main.newMusic = 0; // no music
            return true;       // suppress vanilla music choice
        }

        public static int EntryTileX => ModContent.GetInstance<AbyssSubworld>().Width / 2;
        public const int EntryTileY = 90;


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


    internal sealed class AbyssSubworldUpdateSystem : ModSystem
    {

        public override void Load()
        {
            OnEnter += PlayTransition;
        }

        private void PlayTransition()
        {

        }


        /// <summary>
        /// An event that's invoked when the Abyss is entered.
        /// </summary>
        public static event Action OnEnter;
        public static bool WasInSubworldLastFrame
        {
            get;
            private set;
        }


        public override void PreUpdateEntities()
        {
            bool inSubworld = SubworldSystem.IsActive<AbyssSubworld>();
            if (WasInSubworldLastFrame != inSubworld)
            {
                WasInSubworldLastFrame = inSubworld;
                if (inSubworld)
                    OnEnter?.Invoke();
            }

            if (!WasInSubworldLastFrame)
                return;


        }





        public override void PreUpdateWorld()
        {
            if (!SubworldSystem.IsActive<AbyssSubworld>())
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // suspicious system here

            Wiring.UpdateMech();

            TileEntity.UpdateStart();
            foreach (TileEntity te in TileEntity.ByID.Values)
                te.Update();
            TileEntity.UpdateEnd();




            sw.Stop();
            if (sw.ElapsedMilliseconds > 5)
                Main.NewText($"PreUpdateWorld section took {sw.ElapsedMilliseconds} ms");


            if (Main.GameUpdateCount % 120 == 0)
            {
                Main.NewText($"Liquids: {Liquid.numLiquid}");
                Main.NewText($"TileEntities: {TileEntity.ByID.Count}");
            }
        }

    }
}
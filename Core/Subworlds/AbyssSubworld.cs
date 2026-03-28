using AbyssOverhaul.Core.Subworlds.TransitionScreen;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using static CalamityMod.World.CustomAbyssHole;

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
        // Usually what you want for subworlds.
        // No loading text at all.

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
        public override bool NoPlayerSaving => AbyssTransitionSystem.SuppressPlayerSaving;
        // Silence the loading screen.
        public override bool ChangeAudio()
        {
            Main.newMusic = 0; // no music
            return true;       // suppress vanilla music choice
        }

        // This is the fixed landing point inside the subworld.x
        public const int EntryTileX = 1000 / 2;
        public const int EntryTileY = 90;
        private float _swimTime;
        private double _bodyFrameCounter;
        private float _legFrameCounter;
        private Vector2 _previousSwimCenter;
        private bool _initializedSwimCenter;

        public static Vector2 EntryWorld => new(EntryTileX * 16f, EntryTileY * 16f);

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
            // Keep vanilla underground layers hidden in tiny subworlds.
            Main.worldSurface = Main.maxTilesY - 42;
            Main.rockLayer = Main.maxTilesY;

            // Important:
            // Do not forcibly reset time/day/rain here if you want the handoff
            // to feel continuous.
        }
    }

    internal sealed class AbyssBootstrapPass : GenPass
    {
        public AbyssBootstrapPass() : base("Abyss bootstrap", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Stabilizing abyss entrance";
            Main.spawnTileX = AbyssSubworld.EntryTileX;
            Main.spawnTileY = AbyssSubworld.EntryTileY;
        }
    }

    internal sealed class AbyssSubworldBoundsPass : GenPass
    {
        public AbyssSubworldBoundsPass() : base("Abyss bounds", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Defining abyss bounds";

            const int sidePadding = 10;
            const int topPadding = 10;
            const int bottomPadding = 14;

            int minX = sidePadding;
            int maxX = Main.maxTilesX - 1 - sidePadding;
            int topY = topPadding;
            int bottomY = Main.maxTilesY - 1 - bottomPadding;
            int chasmX = Main.maxTilesX / 2;

            AbyssGenUtils.SetBounds(
                minX,
                maxX,
                topY,
                bottomY,
                chasmX,
                false,
                ModContent.GetInstance<AbyssOverhaul>()
            );
        }
    }

    internal sealed class AbyssSubworldAbyssPass : GenPass
    {
        public AbyssSubworldAbyssPass() : base("Abyss", 10f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Generating abyss";
            CustomAbyssHole.PlaceAbyssFromCurrentBounds();
        }
    }

    internal sealed class AbyssEntryPocketPass : GenPass
    {
        public AbyssEntryPocketPass() : base("Entry pocket", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Opening entry pocket";

            int x = AbyssSubworld.EntryTileX;
            int y = AbyssSubworld.EntryTileY;

            AbyssWorldGenHelper.CarveBlob(x, y, 44, 20, 0.35f, true);

            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(x, y + 10),
                new Vector2(AbyssGenUtils.ChasmX, AbyssGenUtils.TopY + 24),
                12,
                16,
                0.2f,
                true
            );

            AbyssWorldGenHelper.FloodOpenSpace(x - 60, x + 60, y - 30, y + 45);
            AbyssWorldGenHelper.ReframeArea(x - 65, x + 65, y - 35, y + 50);
        }
    }

    internal sealed class AbyssSubworldUpdateSystem : ModSystem
    {
        public override void PreUpdateWorld()
        {
            if (!SubworldSystem.IsActive<AbyssSubworld>())
                return;

            Wiring.UpdateMech();

            TileEntity.UpdateStart();
            foreach (TileEntity te in TileEntity.ByID.Values)
                te.Update();
            TileEntity.UpdateEnd();

            if (++Liquid.skipCount > 1)
            {
                Liquid.UpdateLiquid();
                Liquid.skipCount = 0;
            }
        }
    }
}
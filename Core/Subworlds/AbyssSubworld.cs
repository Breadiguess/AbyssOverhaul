using AbyssOverhaul.Core.Subworlds.TransitionScreen;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace AbyssOverhaul.Core.Subworlds
{
    internal class AbyssSubworld : Subworld
    {
        public override int Width => 2400;
        public override int Height => 1400;

        public override bool ShouldSave => true;

        // Usually what you want for subworlds.
        // No loading text at all.

        public override void DrawMenu(GameTime gameTime)
        {
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

        // This is the fixed landing point inside the subworld.
        // Put it in your entrance chamber / staging pocket.
        public const int EntryTileX =2400/2;
        public const int EntryTileY = 90;
        private float _swimTime;
        private double _bodyFrameCounter;
        private float _legFrameCounter;
        private Vector2 _previousSwimCenter;
        private bool _initializedSwimCenter;

        public static Vector2 EntryWorld => new(EntryTileX * 16f, EntryTileY * 16f);

        public override List<GenPass> Tasks => new()
        {
            new AbyssBootstrapPass(),
            // Put your real abyss generation passes here.
            // Do NOT keep the old "fill everything with dirt" demo pass.
        };

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

            // Set the world spawn so first entry lands somewhere intentional.
            Main.spawnTileX = AbyssSubworld.EntryTileX;
            Main.spawnTileY = AbyssSubworld.EntryTileY;

            // Minimal bootstrap only.
            // Replace this with your real gen later.
            // The important part is: no giant full-world fill loop here.
        }
    }

    // Keep your subworld-only updates separate if you need them.
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
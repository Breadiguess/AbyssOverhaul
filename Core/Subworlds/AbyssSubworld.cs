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
        // Pick real numbers for your actual subworld.
        public override int Width => 2400;
        public override int Height => 1400;

        // This is critical. Do not regenerate the whole thing every time.
        public override bool ShouldSave => true;

        // Usually what you want for subworlds.
        // No loading text at all.
        public override void DrawMenu(GameTime gameTime)
        {
            base.DrawMenu(gameTime);
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
using AbyssOverhaul.Content.Layers.FossilShale.Tiles.Rubble;
using AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness.Projectiles;
using CalamityMod.Projectiles.Environment;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Abyss.AbyssAmbient;
using CalamityMod.Tiles.Merges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness
{
    internal class ProjectileLightRegistrySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            RegisterVanillaLights();
            RegisterCrossModLights();
        }

        public override void Unload()
        {
            ProjectileLightRegistry.Clear();
        }

        private static void RegisterVanillaLights()
        {
            /*
            // Example: all vanilla torches.
            TileLightRegistry.Register(TileID.Torches, (i, j, type, tile) =>
            {
                // Avoid weird duplicate emission from alternating frame pieces if needed.
                ReworkedAbyssLighting.AddTileLight(
                    i, j,
                    scale: 0.3f,
                    opacity: 0.9f,
                    color: new Color(255, 190, 90),
                    lifetime: 20,
                    worldOffset: new Vector2(0f, -4f));

                return false;
            });

            // Example: campfires (3x2 multitile). Emit once from the top-left piece only.
            TileLightRegistry.Register(TileID.Campfire, (i, j, type, tile) =>
            {
                if (!TileLightRegistry.IsTopLeftOfMultiTile(tile, 54, 36))
                    return false;

                ReworkedAbyssLighting.AddTileLight(
                    i, j,
                    scale: 3.2f,
                    vectorScale: new Vector2(1.25f, 0.9f),
                    opacity: 0.85f,
                    color: new Color(255, 170, 110),
                    lifetime: 2,
                    worldOffset: new Vector2(16f, 8f));

                return true;
            });*/
        }

        private static void RegisterCrossModLights()
        {

            ProjectileLightRegistry.Register(ModContent.ProjectileType<LavaChunk>(), (proj) =>
            {
                ReworkedAbyssLighting.AddProjectileLight(proj.Center, scale: 0.4f);
                
                return true;
            });
            
          
        }
    }
}

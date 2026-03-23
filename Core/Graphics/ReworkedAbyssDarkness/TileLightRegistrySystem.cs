using CalamityMod.Tiles.Abyss.AbyssAmbient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness
{
    internal class TileLightRegistrySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            RegisterVanillaLights();
            RegisterCrossModLights();
        }

        public override void Unload()
        {
            TileLightRegistry.Clear();
        }

        private static void RegisterVanillaLights()
        {
            // Example: all vanilla torches.
            TileLightRegistry.Register(TileID.Torches, (i, j, type, tile) =>
            {
                // Avoid weird duplicate emission from alternating frame pieces if needed.
                ReworkedAbyssLighting.AddTileLight(
                    i, j,
                    scale: 1.9f,
                    opacity: 0.9f,
                    color: new Color(255, 190, 90),
                    lifetime: 2,
                    worldOffset: new Vector2(0f, -4f));

                return true;
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
            });
        }

        private static void RegisterCrossModLights()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModTile>("AbyssTorch", out ModTile abyssTorch))
                {
                    TileLightRegistry.Register(abyssTorch.Type, (i, j, type, tile) =>
                    {
                        ReworkedAbyssLighting.AddTileLight(
                            i, j,
                            scale: 2.4f,
                            opacity: 1f,
                            color: new Color(90, 180, 255),
                            lifetime: 2,
                            worldOffset: new Vector2(0f, -2f));

                        return true;
                    });
                }
            }



            TileLightRegistry.Register(ModContent.TileType<AbyssGiantKelp1>(), (i, j, type, tile) =>
            {
                ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 4, lifetime: 10, color: Color.Thistle);
                return true;
            });

            TileLightRegistry.Register(ModContent.TileType<AbyssGiantKelp2>(), (i, j, type, tile) =>
            {
                ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 4, lifetime: 10, color: Color.Thistle);
                return true;
            });
            TileLightRegistry.Register(ModContent.TileType<AbyssGiantKelp3>(), (i, j, type, tile) =>
            {
                ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 4, lifetime: 10, color: Color.Thistle);
                return true;
            });

            TileLightRegistry.Register(ModContent.TileType<AbyssGiantKelp4>(), (i, j, type, tile) =>
            {
                ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 4, lifetime: 10, color: Color.Thistle);
                return true;
            });
        }
    }
}

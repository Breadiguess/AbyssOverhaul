using AbyssOverhaul.Content.Layers.FossilShale.Tiles.Rubble;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Abyss.AbyssAmbient;
using CalamityMod.Tiles.Merges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness.Tiles
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
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind("AbyssTorch", out ModTile abyssTorch))
                {
                    TileLightRegistry.Register(abyssTorch.Type, (i, j, type, tile) =>
                    {
                        ReworkedAbyssLighting.AddTileLight(
                            i, j,
                            scale: 2.4f,
                            opacity: 0.4f,
                            color: new Color(90, 180, 255),
                            lifetime: 60,
                            worldOffset: new Vector2(0f, -2f));

                        return true;
                    });
                }
            }
        
            TileLightRegistry.Register(ModContent.TileType<ThermalVent1>(), (i, j, type, tile) =>
            {
                if (TileLightRegistry.IsTopLeftOfMultiTile(tile, 16, 16))
                    ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 1.5f, lifetime: 30, color: Color.Red * 0.8f);
                return true;
            });

            TileLightRegistry.Register(ModContent.TileType<MediumOrbbies>(), (i, j, type, tile) =>
            {
                if (TileLightRegistry.IsTopLeftOfMultiTile(tile, 16, 16))
                    ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 1.5f, lifetime: 30, color: Color.Thistle*0.8f);
                return true;
            });
            TileLightRegistry.Register(ModContent.TileType<XL_Orbbies>(), (i, j, type, tile) =>
            {
               if(TileLightRegistry.IsTopLeftOfMultiTile(tile, 16, 16))
                ReworkedAbyssLighting.AddTileLight(i, j, ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value, 1.5f, lifetime: 60, color: Color.Thistle*0.8f);
                return true;
            });

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

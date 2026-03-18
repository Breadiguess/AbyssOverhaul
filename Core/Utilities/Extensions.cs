using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Content.Layers.TenebrousMarsh.Tiles;
using AbyssOverhaul.Content.Layers.TheVeil.Tiles;
using AbyssOverhaul.Core.NPCOverrides;
using CalamityMod;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Tiles.Ores;

using static Terraria.ModLoader.ModContent;
namespace AbyssOverhaul.Core.Utilities
{
    public static partial class AbyssUtilities
    {
        public static GlobalFishNPC Abyss(this NPC npc) => npc.GetGlobalNPC<GlobalFishNPC>();


        public static void MergeWithNewAbyss(int type) => CalamityUtils.MergeWithSet(type, new int[] {
            // Sulphurous Sea
            TileType<SulphurousSand>(),
            TileType<SulphurousSandstone>(),
            TileType<SulphurousShale>(),
            // Abyss
            TileType<AbyssGravel>(),
            TileType<PyreMantle>(),
            TileType<PyreMantleMolten>(),
            TileType<Voidstone>(),
            TileType<PlantyMush>(),
            TileType<ScoriaOre>(),
            //Fossil Shale
            TileType<ShaleSand_Tile>(),
            TileType<CarbonShale_Tile>(),
            TileType<CyanobacteriaSludge_Tile>(),


            //TenebrousMarsh
            TileType<MantleGravel_Tile>(),
            TileType<Tenebris_Tile>(),


            //Veil
            TileType<marine_snow>(),
            TileType<VoidstoneMantle>(),

        });
    }
}

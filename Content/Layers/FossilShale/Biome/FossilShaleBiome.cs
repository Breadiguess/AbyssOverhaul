using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Waters;

namespace AbyssOverhaul.Content.Layers.FossilShale.Biome
{
    internal class FossilShaleBiome : ModBiome
    {
        
        public override int BiomeTorchItemType => ModContent.ItemType<KelpTorch>();
        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;
        public override ModWaterStyle WaterStyle => UpperAbyssWater.Instance;
        public override void OnInBiome(Player player)
        {
            player.GetModPlayer<PreventDeathFromJustBeingOutOfWater>().IsInModdedAbyssBiome = true;
        }
        
        public override int Music
        {
            get
            {
                if (CalamityPlayer.areThereAnyDamnBosses)
                    return Main.curMusic;

                return MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/FossilShaleOst");
            }
        }
        public override bool IsBiomeActive(Player player)
        {
            if (!AbyssGenUtils.Initialized)
                return false;

            Point tilePos = player.Center.ToTileCoordinates();
            FossilShaleLayer layer = ModContent.GetInstance<FossilShaleLayer>();
            
            return tilePos.X >= AbyssGenUtils.MinX &&
                   tilePos.X <= AbyssGenUtils.MaxX &&
                   tilePos.Y >= layer.StartHeight &&
                   tilePos.Y <= layer.EndHeight;
        }
    }
    public class FossilShaleBiomeTileCounter : ModSystem
    {
        public int BlockCount;

        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
        {
            BlockCount = tileCounts[ModContent.TileType<CarbonShale_Tile>()] + tileCounts[ModContent.TileType<CyanobacteriaSludge_Tile>()];

        }
    }
}

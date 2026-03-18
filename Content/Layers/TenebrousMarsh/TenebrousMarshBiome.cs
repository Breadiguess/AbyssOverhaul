using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Waters;

namespace AbyssOverhaul.Content.Layers.TenebrousMarsh
{
    internal class TenebrousMarshBiome : ModBiome
    {

        public override int BiomeTorchItemType => ModContent.ItemType<KelpTorch>();
        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;
        public override ModWaterStyle WaterStyle => MiddleAbyssWater.Instance;
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

                return MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/TenebrousMarshOst");
            }
        }
        public override bool IsBiomeActive(Player player)
        {
            if (!AbyssGenUtils.Initialized)
                return false;

            Point tilePos = player.Center.ToTileCoordinates();
            TenebrousMarshLayer layer = ModContent.GetInstance<TenebrousMarshLayer>();

            bool isInBiome = tilePos.X >= AbyssGenUtils.MinX &&
                   tilePos.X <= AbyssGenUtils.MaxX &&
                   tilePos.Y >= layer.StartHeight &&
                   tilePos.Y <= layer.EndHeight;

            //Main.NewText(isInBiome);
            return isInBiome;
        }
    }

}


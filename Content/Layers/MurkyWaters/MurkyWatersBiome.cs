using AbyssOverhaul.Content.Layers.FossilShale;
using AbyssOverhaul.Content.Layers.FossilShale.Tiles;
using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Utilities;
using CalamityMod.CalPlayer;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Waters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Layers.MurkyWaters
{
    internal class MurkyWatersBiome : ModBiome
    {
        public static MurkyWatersLayer instance => ModContent.GetInstance<MurkyWatersLayer>();
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


                return ModContent.GetInstance<CalamityMod.CalamityMod>().GetMusicFromMusicMod("AbyssLayer2")??  MusicID.Hell;//CalamityMod.CalamityMod.Instance.GetMusicFromMusicMod("AbyssLayer2") ?? MusicID.Hell;
            }
        }
        public override bool IsBiomeActive(Player player)
        {
            if (!AbyssGenUtils.Initialized)
                return false;

            Point tilePos = player.Center.ToTileCoordinates();
            

            return tilePos.X >= AbyssGenUtils.MinX &&
                   tilePos.X <= AbyssGenUtils.MaxX &&
                   tilePos.Y >= instance.StartHeight &&
                   tilePos.Y <= instance.EndHeight;
        }
    }
 
}

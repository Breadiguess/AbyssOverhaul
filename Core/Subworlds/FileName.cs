using AbyssOverhaul.Core.ModPlayers;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.Subworlds
{
    internal sealed class AbyssTransitionTriggerSystem : ModSystem
    {
      
        public override void PostUpdatePlayers()
        {
            if (Main.dedServ || Main.gameMenu || AbyssTransitionSystem.IsBusy)
                return;

            Player player = Main.LocalPlayer;
            if (player is null || !player.active || player.dead)
                return;

            if (!SubworldSystem.IsActive<AbyssSubworld>())
            {
                if(player.GetModPlayer<PressurePlayer>().InPressureZone)//if (PlayerIntersectsTileRect(player, ))
                {

                    AbyssEntrySaveSkipSystem.SkipNextEntryPlayerBackup = true;
                    bool success = SubworldSystem.Enter<AbyssSubworld>();

                    if (!success)
                        AbyssEntrySaveSkipSystem.SkipNextEntryPlayerBackup = false;
                }
            }
            else
            {
               // if (PlayerIntersectsTileRect(player, SubworldExitZoneTiles))
                        AbyssTransitionSystem.RequestExit();
            }
        }

        private static bool PlayerIntersectsTileRect(Player player, Rectangle tileRect)
        {
            Rectangle worldRect = new(
                tileRect.X * 16,
                tileRect.Y * 16,
                tileRect.Width * 16,
                tileRect.Height * 16);

            return player.Hitbox.Intersects(worldRect);
        }
    }
}
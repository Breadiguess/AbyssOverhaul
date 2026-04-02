using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Subworlds
{
   

    internal sealed class AbyssSubworldEntryPlayer : ModPlayer
    {
        public int EntryCooldown;

        public override void PreUpdate()
        {
            if (EntryCooldown > 0)
                EntryCooldown--;
        }
    }

    internal static class AbyssSubworldActions
    {
        public static void TryEnter(Player player)
        {
            if (player is null || !player.active || player.dead || Main.gameMenu)
                return;

            if (SubworldSystem.IsActive<AbyssSubworld>())
                return;

            AbyssSubworldEntryPlayer mp = player.GetModPlayer<AbyssSubworldEntryPlayer>();
            if (mp.EntryCooldown > 0)
                return;

            mp.EntryCooldown = 20;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                SubworldSystem.Enter<AbyssSubworld>();
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer)
            {
                ModPacket packet = ModContent.GetInstance<AbyssOverhaul>().GetPacket();
                packet.Write((byte)AbyssOverhaulMessageType.RequestEnterAbyss);
                packet.Send();
            }
        }
    }
}

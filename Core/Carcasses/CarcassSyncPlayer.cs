using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Core.Carcasses
{
    internal class CarcassSyncPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            if (Main.netMode == NetmodeID.Server)
                CarcassSystem.SyncFull(Player.whoAmI);
        }
    }
}

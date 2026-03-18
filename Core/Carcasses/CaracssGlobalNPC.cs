using AbyssOverhaul.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Carcasses
{
    public class CarcassGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
                return;

            if (!ShouldCreateCarcass(npc))
                return;

            int flesh = GetInitialFlesh(npc);

            CarcassEntity carcass = CarcassSystem.CreateCarcass(npc.Center, npc, flesh, npc.Hitbox);
            CarcassSystem.SyncAdd(carcass);
        }

        private bool ShouldCreateCarcass(NPC npc)
        {
            if (!npc.active)
                return false;

            if (npc.friendly)
                return false;

            if (npc.townNPC)
                return false;

            if (npc.lifeMax <= 5)
                return false;

            if (npc.dontTakeDamage)
                return false;


            if (!Main.rand.NextBool(5))
                return false;
            if (!AbyssGenUtils.InAbyssX(npc.Center.ToTileCoordinates().X))
                return false;

            return true;
        }

        private int GetInitialFlesh(NPC npc)
        {
            return npc.lifeMax / 2;
        }
    }
}

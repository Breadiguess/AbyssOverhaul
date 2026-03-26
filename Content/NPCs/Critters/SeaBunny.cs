using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Critters
{
    internal class SeaBunny : ModNPC
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.CountsAsCritter[Type] = true;
        }
        public override void SetDefaults()
        {
            NPC.lifeMax = 40;
            NPC.Size = new(20);
            NPC.aiStyle = NPCAIStyleID.CritterWorm;
        }
        
    }
}

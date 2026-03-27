using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs
{
    internal class HagFish : ModNPC, IEcologyParticipant
    {
        public void SetupEcology(NPC npc, EcologyGlobalNPC ecology)
        {
            ecology.Traits.Append(NpcTraitFlags.Scavager);
        }

        public enum state
        {

        }
        public override void SetDefaults()
        {
            NPC.lifeMax = 60_000;
            

        }

        public override void AI()
        {
            
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {


            return false;
        }
    }
}

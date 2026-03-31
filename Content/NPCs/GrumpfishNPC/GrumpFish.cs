using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.GrumpfishNPC
{
    internal class GrumpFish : ModNPC, IEcologyParticipant
    {
        public void SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.Prey);
            definition.AddTraits(NpcTraitFlags.Schooling);
            definition.BaseAggression = 0.7f;
            definition.BaseCowardice = 1;
        }

        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {

        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 24;
        }
        public override void AI()
        {
            
        }


        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

   
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.Hostile.GrumpfishNPC
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
        public enum State
        {
            SwimAimlessly,

            AttackPlayer,
            ChargingMeatball,

            GroupUpWithOtherGrumpfish
        }

        public State CurrentState;
        public override void SetDefaults()
        {
            NPC.Size = new Vector2(50);

            NPC.damage = 40;
            NPC.defense = 12;
            NPC.lifeMax = 600;
        }

        public override void AI()
        {
            NPC.TargetClosest(false);


            switch (CurrentState)
            {
                case State.SwimAimlessly:

                    break;


                case State.ChargingMeatball:

                    break;

                case State.GroupUpWithOtherGrumpfish:

                    break;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            
        }


        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

   
    }
}

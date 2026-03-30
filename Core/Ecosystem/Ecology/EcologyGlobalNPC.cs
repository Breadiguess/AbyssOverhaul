using System;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
  


    public sealed class EcologyGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;


        /// <summary>
        /// Stores the entire species' dietary classification, which can be used to determine what other NPCs it interacts with and how. 
        /// This is separate from traits because it's more fundamental to the NPC's role in the ecosystem, 
        /// while traits can be more variable and individual.
        /// </summary>
        public static FoodConsumerType FoodConsumer;

        /// <summary>
        /// Speices wide traits.
        /// </summary>
        public List<NpcTraitFlags> SpeciesTraits;
        public List<NpcTraitFlags> IndividualTraits;
        public int SchoolLeader = -1;



        public int Hunger;

        public float Aggression;
        public float Fear;
        public float Curiosity;
        public float PreferredDepth;
        public float PreferredSpacing = 64f;
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return EcologyRegistry.HasParticipant(entity.type);
        }


        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            npc.Ecology().IndividualTraits = SpeciesTraits.ToList();
            base.OnSpawn(npc, source);

        }



        public void UpdateNPC(NPC npc)
        {

        }


        public bool HasTrait(NpcTraitFlags flag) => (SpeciesTraits.Contains(flag));
    }

 
    public static class EcologyExtensions
    {
      
        public static EcologyGlobalNPC Ecology(this NPC npc) =>
            npc.GetGlobalNPC<EcologyGlobalNPC>();
    }
}
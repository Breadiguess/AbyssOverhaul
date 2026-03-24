using System;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    [Flags]
    public enum NpcTraitFlags
    {
        None = 0,
        Predator = 1 << 0,
        Prey = 1 << 1,
        Schooling = 1 << 2,
        AmbushPredator = 1 << 3,
        Territorial = 1 << 4
    }
//god help me i am writing this on my phone

    public sealed class EcologyGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public NpcTraitFlags[] Traits;
        public int SchoolLeader = -1;
        
        
        
        public float Aggression;
        public float Fear;
        public float Curiosity;
        public float PreferredDepth;
        public float PreferredSpacing = 64f;
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return EcologyRegistry.HasParticipant(entity.type);
        }

       

        public bool HasTrait(NpcTraitFlags flag) => (Traits.Contains(flag));
    }

 
    public static class EcologyExtensions
    {
      
        public static EcologyGlobalNPC Ecology(this NPC npc) =>
            npc.GetGlobalNPC<EcologyGlobalNPC>();
    }
}
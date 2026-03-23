using System;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Common.Ecology
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

        public NpcTraitFlags Traits;
        public int SchoolLeader = -1;
        
        
        
        public float Aggression;
        public float Fear;
        public float Curiosity;
        public float PreferredDepth;
        public float PreferredSpacing = 64f;
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return EcologyRegistry.HasTraits(entity.type);
        }

        public bool HasTrait(NpcTraitFlags flag) => (Traits & flag) != 0;
    }

    public static class EcologyRegistry
    {
        private static readonly HashSet<int> TraitNpcTypes = new();

        public static void Register(int npcType)
        {
            TraitNpcTypes.Add(npcType);
        }

        public static bool HasTraits(int npcType)
        {
            return TraitNpcTypes.Contains(npcType);
        }
    }

    public static class EcologyExtensions
    {
        public static bool HasEcology(this NPC npc) =>
            EcologyRegistry.HasTraits(npc.type);

        public static EcologyGlobalNPC Ecology(this NPC npc) =>
            npc.GetGlobalNPC<EcologyGlobalNPC>();
    }
}
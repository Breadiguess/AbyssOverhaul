using AbyssOverhaul.Core.Ecosystem.FoodSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Simulation
{

    public static class EcologyActorBridge
    {
        public static void CopyNpcToActor(NPC npc, EcologyGlobalNPC eco, EcologyActor actor)
        {
            actor.SpeciesID = npc.type;
            actor.LastKnownWorldPosition = npc.Center;

            actor.IndividualTraitOverrides = eco.IndividualTraitOverrides;
            actor.MaxHungerSpecies = eco.MaxHungerSpecies;
            actor.Hunger = eco.Hunger;
            actor.Aggression = eco.Aggression;
            actor.Fear = eco.Fear;
            actor.Curiosity = eco.Curiosity;
            actor.PreferredDepth = eco.PreferredDepth;
            actor.PreferredSpacing = eco.PreferredSpacing;
            actor.FoodConsumer = eco.FoodConsumer;

            actor.Metabolism.Energy = eco.Metabolism?.Energy ?? 0f;
            actor.Metabolism.StomachContent = eco.Metabolism?.StomachContent ?? 0f;
            actor.Metabolism.BodyCondition = eco.Metabolism?.BodyCondition ?? 0f;
            actor.Metabolism.Fatigue = eco.Metabolism?.Fatigue ?? 0f;
            actor.Metabolism.Hunger = eco.Metabolism?.Hunger ?? 0f;
        }

        public static void CopyActorToNpc(EcologyActor actor, NPC npc, EcologyGlobalNPC eco)
        {
            eco.SpeciesDefinition = EcologyRegistry.GetSpecies(actor.SpeciesID);
            eco.IndividualTraitOverrides = actor.IndividualTraitOverrides;
            eco.MaxHungerSpecies = actor.MaxHungerSpecies;
            eco.Hunger = actor.Hunger;
            eco.Aggression = actor.Aggression;
            eco.Fear = actor.Fear;
            eco.Curiosity = actor.Curiosity;
            eco.PreferredDepth = actor.PreferredDepth;
            eco.PreferredSpacing = actor.PreferredSpacing;
            eco.FoodConsumer = actor.FoodConsumer;

            eco.Metabolism ??= new MetabolismState();
            eco.Metabolism.Energy = actor.Metabolism.Energy;
            eco.Metabolism.StomachContent = actor.Metabolism.StomachContent;
            eco.Metabolism.BodyCondition = actor.Metabolism.BodyCondition;
            eco.Metabolism.Fatigue = actor.Metabolism.Fatigue;
            eco.Metabolism.Hunger = actor.Metabolism.Hunger;
        }
    }

}

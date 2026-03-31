using AbyssOverhaul.Core.Ecosystem.FoodSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    public sealed class SpeciesEcologyDefinition
    {
        public int SpeciesID { get; }

        public NpcTraitFlags Traits;
        public FoodConsumerType FoodConsumer;

        public int BaseMaxHunger;
        public float BaseAggression;
        public float BaseFear;
        public float BaseCuriosity;
        public float BaseCowardice;


        public float BasePreferredDepth;
        public float BasePreferredSpacing = 64f;

        public MetabolismDefinition Metabolism = new();

        public SpeciesEcologyDefinition(int speciesID)
        {
            SpeciesID = speciesID;
            Traits = NpcTraitFlags.None;
        }

        public SpeciesEcologyDefinition AddTraits(params NpcTraitFlags[] traits)
        {
            for (int i = 0; i < traits.Length; i++)
                Traits |= traits[i];

            return this;
        }

        public SpeciesEcologyDefinition RemoveTraits(params NpcTraitFlags[] traits)
        {
            for (int i = 0; i < traits.Length; i++)
                Traits &= ~traits[i];

            return this;
        }
    }
}

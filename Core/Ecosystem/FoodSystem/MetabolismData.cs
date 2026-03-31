using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.FoodSystem
{
    public sealed class MetabolismState
    {
        public float Energy;
        public float StomachContent;
        public float BodyCondition;
        public float Fatigue;
        public float Hunger;

        public void InitializeFrom(MetabolismDefinition def)
        {
            Energy = def.MaxEnergy;
            StomachContent = def.MaxStomachContent * 0.35f;
            BodyCondition = def.MaxBodyCondition;
            Fatigue = 0f;
            Hunger = 0f;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.FoodSystem
{
    public sealed class MetabolismDefinition
    {
        public float MaxEnergy = 100f;
        public float MaxStomachContent = 60f;
        public float MaxBodyCondition = 50f;
        public float MaxFatigue = 100f;

        public float BasalMetabolicRate = 1f;
        public float DigestiveRate = 3f;
        public float ActivityCost = 1f;

        public float HungerSensitivity = 1f;
        public float ReserveConversionRate = 2f;

        public float HuntThreshold = 0.55f;
        public float DesperationThreshold = 0.8f;
        public float RestThreshold = 0.65f;
    }

}

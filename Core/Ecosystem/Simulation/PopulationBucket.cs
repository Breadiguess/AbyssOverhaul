using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Simulation
{
    public sealed class PopulationBucket
    {
        public int SpeciesID;
        public int Count;
        public float AverageHunger;
        public float AverageBodyCondition;
        public float AverageFatigue;
    }

}

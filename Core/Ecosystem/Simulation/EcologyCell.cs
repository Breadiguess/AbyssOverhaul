using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Simulation
{
    [Flags]
    public enum CellTraversalFlags
    {
        none = 0,
        Swim = 1 << 0,
        Walk = 1<<1,
        Fly = 1 << 2,
    }
    public sealed class EcologyCell
    {
        public Point Coord;
        public Rectangle WorldBounds;

        public List<long> ActorIDs = new();
        public Dictionary<int, PopulationBucket> Populations = new();

        public float PlantFood;
        public float MeatFood;
        public float Carrion;
        public float Shelter;
        public float Threat;
        public float HabitatQuality;
        public double LastSimulatedTime;

        public CellTraversalFlags Traversal;
        public bool IsSolidBlocked;
        public float WaterCoverage;
        public float OpenAirCoverage;
        public float WalkableCoverage;
    }



}

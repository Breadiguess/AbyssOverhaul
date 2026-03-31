using AbyssOverhaul.Core.Ecosystem.FoodSystem;
using AbyssOverhaul.Core.Ecosystem.Simulation.AbyssOverhaul.Core.Ecosystem.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Simulation
{
    public sealed class EcologyActor
    {
        public long ActorID;
        public int SpeciesID;

        public Point CellCoord;
        public Vector2 LastKnownWorldPosition;

        public long GroupID;
        public int TerritoryID = -1;

        public MetabolismState Metabolism = new();
        public NpcTraitFlags IndividualTraitOverrides;

        public float Aggression;
        public float Fear;
        public float Curiosity;
        public float PreferredDepth;
        public float PreferredSpacing;

        public int MaxHungerSpecies;
        public int Hunger;

        public FoodConsumerType FoodConsumer;

        public EcologyIntent Intent;
        public long TargetActorID = -1;
        public int TargetSpeciesID = -1;

        public bool IsLoaded;
        public int LoadedNpcWhoAmI = -1;

        public bool ImportantIndividual;
        public bool Alive = true;
        public double LastSimulatedTime;
    }

}

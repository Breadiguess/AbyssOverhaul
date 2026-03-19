using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem
{
    public sealed class TileNutritionDefinition
    {
        public int TileType;
        public int BaseNutrition;
        public int MaxBites;
        public int ReplenishTime;
        public FoodConsumerType AllowedConsumers;
        public FoodKind Kind;
        public bool RequiresSolidTileStillPresent;
        public bool AutoRestoreBites;

        public bool CanBeEatenBy(FoodConsumerType consumerType)=>(AllowedConsumers & consumerType) != 0;
    }
}

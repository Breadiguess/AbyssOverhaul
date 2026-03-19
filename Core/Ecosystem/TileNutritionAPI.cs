using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Core.Ecosystem
{
    public static class TileNutritionAPI
    {
        public static bool TryGetDefinition(int i, int j, out TileNutritionDefinition def)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            if (!tile.HasTile)
            {
                def = null;
                    return false;
            }
            return TileNutritionRegistry.tryGet(tile.TileType, out def);
        }

        public static int GetNutritionAt(int i, int j, FoodConsumerType consumer)
        {
            if(!TryGetDefinition(i, j, out TileNutritionDefinition def))
            {
                return 0;
            }
            Point16 pos = new(i, j);
            TileNutritionState state = NutritionWorldData.GetOrCreate(pos, def);

            if (state.BitesRemaining <= 0)
                return 0;

            int perBite = Math.Max(1, def.BaseNutrition/Math.Max(1, def.MaxBites));
            return perBite;
        }

        public static bool CanEat(int i, int j, FoodConsumerType consumer)
        {
            return GetNutritionAt(i, j, consumer) > 0;
        }
        public static int Consume(int i, int j, FoodConsumerType consumer, int requestedNutrition =int.MaxValue)
        {
            if (!TryGetDefinition(i, j, out var def) || !def.CanBeEatenBy(consumer))
            {
                return 0;
            }

            Tile tile = Framing.GetTileSafely(i, j);
            if (!tile.HasTile)
            {
                return 0;
            }

            Point16 pos = new Point16(i, j);
            TileNutritionState state = NutritionWorldData.GetOrCreate(pos, def);
            if (state.BitesRemaining <= 0)
                return 0;

            int perBite = Math.Max(1, def.BaseNutrition / Math.Max(1, def.MaxBites));

            int bitesWanted=Math.Max(1, requestedNutrition/perBite);
            int bitesTaken =Math.Min(state.BitesRemaining, bitesWanted);

            state.BitesRemaining -= (byte)bitesWanted;
            int restored = bitesTaken * perBite;

            if (state.BitesRemaining <= 0 && def.AutoRestoreBites)
                state.NextReplenishTick = (int)Main.GameUpdateCount + def.ReplenishTime;

            return restored;

        }
    }
}

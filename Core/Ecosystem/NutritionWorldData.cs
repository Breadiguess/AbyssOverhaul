using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Core.Ecosystem
{
    public static class NutritionWorldData
    {
       public static readonly Dictionary<Point16, TileNutritionState> states = new();
        public static IReadOnlyDictionary<Point16, TileNutritionState> State => states;

        public static void Clear() => states.Clear();

        public static TileNutritionState GetOrCreate(Point16 pos, TileNutritionDefinition def)
        {
            if(!states.TryGetValue(pos, out var state))
            {
                state = new TileNutritionState()
                {
                    BitesRemaining = (byte)def.MaxBites,
                    NextReplenishTick = 0
                };

                states.Add(pos, state);
            }
            return state;
        }

        public static bool TryGet(Point16 pos, out TileNutritionState state) =>
            states.TryGetValue(pos, out state);

        public static void Remove(Point16 pos) => states.Remove(pos);

        public static void CleanupDefaultStates()
        {
            var toRemove = new List<Point16>();
            foreach (var pair in states)
            {
                Point16 p = pair.Key;
                Tile tile = Framing.GetTileSafely(p);

                if (!tile.HasTile || !TileNutritionRegistry.tryGet(tile.TileType, out var def))
                {
                    toRemove.Add(p);
                    continue;
                }

                if(pair.Value.BitesRemaining>=def.MaxBites && pair.Value.NextReplenishTick <= 0)
                {
                    toRemove.Add(p);
                }

            }
            foreach (var pair in toRemove)
                states.Remove(pair);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem
{
    public static class TileNutritionRegistry
    {

        public static readonly Dictionary<int, TileNutritionDefinition> definitions = new();
        public static void Register(TileNutritionDefinition def)=>
            definitions[def.TileType] = def;
        public static bool tryGet(int TileType, out TileNutritionDefinition def) =>
            definitions.TryGetValue(TileType, out def);

        public static TileNutritionDefinition GetOrNull(int TileType)
        {
            definitions.TryGetValue(TileType, out var def);
            return def;
        }

        public static void Clear() => definitions.Clear();
    }
}

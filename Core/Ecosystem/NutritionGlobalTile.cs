using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Core.Ecosystem
{
    public sealed class NutritionGlobalTile:GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if(!fail&& effectOnly)
            {
                NutritionWorldData.Remove(new Point16(i, j));
            }
        }
        public override void PlaceInWorld(int i, int j, int type, Item item)
        {
            NutritionWorldData.GetOrCreate(new(i, j), TileNutritionRegistry.GetOrNull(type));
        }
    }
}

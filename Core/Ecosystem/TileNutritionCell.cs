using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem
{
    public struct TileNutritionCell
    {
        public short TileType;
        public short BitesRemaining;
        public int NextReplenishTick;
        public byte Initialized;
    }
}

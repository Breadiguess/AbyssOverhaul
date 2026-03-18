using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.WorldGen
{
    public readonly struct AbyssRegion
    {
        public readonly int MinX;
        public readonly int MaxX;
        public readonly int StartY;
        public readonly int EndY;

        public int Width => MaxX - MinX;
        public int Height => EndY - StartY;
        public int CenterX => (MinX + MaxX) / 2;
        public int CenterY => (StartY + EndY) / 2;

        public AbyssRegion(int minX, int maxX, int startY, int endY)
        {
            MinX = minX;
            MaxX = maxX;
            StartY = startY;
            EndY = endY;
        }

        public bool Contains(int x, int y) =>
            x >= MinX && x <= MaxX &&
            y >= StartY && y <= EndY;
    }

}

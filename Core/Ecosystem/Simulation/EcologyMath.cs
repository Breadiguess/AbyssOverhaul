using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.Simulation
{
    public static class EcologyMath
    {
        public const int CellSizeTiles = 100;
        public const int CellSizePixels = CellSizeTiles * 16;

        public static Point WorldToCell(Vector2 worldPos)
        {
            return new Point(
                (int)MathF.Floor(worldPos.X / CellSizePixels),
                (int)MathF.Floor(worldPos.Y / CellSizePixels));
        }

        public static Rectangle CellToWorldBounds(Point coord)
        {
            return new Rectangle(
                coord.X * CellSizePixels,
                coord.Y * CellSizePixels,
                CellSizePixels,
                CellSizePixels);
        }

        public static IEnumerable<Point> GetCellsInRadius(Point center, int radius)
        {
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                    yield return new Point(x, y);
            }
        }
    }
}

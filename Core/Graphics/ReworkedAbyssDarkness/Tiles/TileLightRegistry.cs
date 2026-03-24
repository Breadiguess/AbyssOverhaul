using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness.Tiles
{
    internal static class TileLightRegistry
    {
        public delegate bool TileLightEmitter(int i, int j, int type, Tile tile);

        private static readonly Dictionary<int, TileLightEmitter> _emitters = new();

        public static void Register(int tileType, TileLightEmitter emitter)
        {
            _emitters[tileType] = emitter;
        }

        public static bool TryEmit(int i, int j, int type, Tile tile)
        {
            if (_emitters.TryGetValue(type, out var emitter))
                return emitter(i, j, type, tile);

            return false;
        }

        public static void Clear()
        {
            _emitters.Clear();
        }

        public static bool IsTopLeftOfMultiTile(Tile tile, int frameWidth, int frameHeight)
        {
            return tile.TileFrameX % frameWidth == 0 && tile.TileFrameY % frameHeight == 0;
        }
    }
}

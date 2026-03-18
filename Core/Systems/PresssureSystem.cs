using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Systems
{
    public class PresssureSystem : ModSystem
    {
        public static IReadOnlyList<AbyssLayer> OrderedLayers => _orderedLayers;
        private static List<AbyssLayer> _orderedLayers = new();

        public override void PostSetupContent()
        {
            RebuildLayerCache();
        }

        public override void OnWorldLoad()
        {
            RebuildLayerCache();
        }

        private static void RebuildLayerCache()
        {
            _orderedLayers = AbyssLayerRegistry.Layers
                .Where(l => l is not null)
                .OrderBy(l => l.StartY)
                .ToList();
        }

        public static bool HasAnyLayers()
        {
            return _orderedLayers is not null && _orderedLayers.Count > 0;
        }

        public static int GetTopY()
        {
            if (!HasAnyLayers())
                return 0;

            return _orderedLayers[0].StartY;
        }

        public static int GetBottomY()
        {
            if (!HasAnyLayers())
                return 0;

            return _orderedLayers[^1].EndY;
        }

        public static bool IsInAbyss(Player player)
        {
            if (!HasAnyLayers())
                return false;

            int yTile = (int)(player.Center.Y / 16f);
            return yTile >= GetTopY() && yTile <= GetBottomY();
        }

        public static AbyssLayer GetLayerForPlayer(Player player)
        {
            if (!HasAnyLayers())
                return null;

            int yTile = (int)(player.Center.Y / 16f);
            return GetLayerAtTileY(yTile);
        }

        public static AbyssLayer GetLayerAtTileY(int yTile)
        {
            if (!HasAnyLayers())
                return null;

            // Small layer count, linear scan is totally fine.
            foreach (AbyssLayer layer in _orderedLayers)
            {
                if (layer.ContainsY(yTile))
                    return layer;
            }

            return null;
        }

        public static float GetGlobalDepthInterpolant(Player player)
        {
            if (!HasAnyLayers())
                return 0f;

            int yTile = (int)(player.Center.Y / 16f);
            return GetGlobalDepthInterpolant(yTile);
        }

        public static float GetGlobalDepthInterpolant(int yTile)
        {
            if (!HasAnyLayers())
                return 0f;

            int top = GetTopY();
            int bottom = GetBottomY();

            if (bottom <= top)
                return 0f;

            return MathHelper.Clamp((yTile - top) / (float)(bottom - top), 0f, 1f);
        }

        public static float GetLayerDepthInterpolant(Player player)
        {
            if (!HasAnyLayers())
                return 0f;

            int yTile = (int)(player.Center.Y / 16f);
            return GetLayerDepthInterpolant(yTile);
        }

        public static float GetLayerDepthInterpolant(int yTile)
        {
            AbyssLayer layer = GetLayerAtTileY(yTile);
            if (layer is null)
                return 0f;

            int top = layer.StartY;
            int bottom = layer.EndY;

            if (bottom <= top)
                return 0f;

            return MathHelper.Clamp((yTile - top) / (float)(bottom - top), 0f, 1f);
        }

        public static bool TryGetAbyssInfo(Player player, out AbyssInfo info)
        {
            info = default;

            if (!HasAnyLayers())
                return false;

            int yTile = (int)(player.Center.Y / 16f);
            AbyssLayer layer = GetLayerAtTileY(yTile);

            
            if (layer is null)
                return false;

            int abyssTop = GetTopY();
            int abyssBottom = GetBottomY();
            int left = player.Hitbox.Left / 16;
            int right = player.Hitbox.Right / 16;
            int centerX = (int)(player.Center.X / 16f);
           

            //Main.NewText($"X L:{left} C:{centerX} R:{right} | AbyssX {AbyssGenUtils.MinX}-{AbyssGenUtils.MaxX} | Y:{yTile}", Color.Cyan);
            if (right < AbyssGenUtils.MinX || left > AbyssGenUtils.MaxX)
                return false;
            info = new AbyssInfo
            {
                Layer = layer,
                TileY = yTile,
                AbyssTopY = abyssTop,
                AbyssBottomY = abyssBottom,
                GlobalDepthInterpolant = GetGlobalDepthInterpolant(yTile),
                LayerDepthInterpolant = GetLayerDepthInterpolant(yTile),
            };
            return true;
        }

        public static void RefreshAfterBoundsChanged()
        {
            RebuildLayerCache();
        }
    }

    public struct AbyssInfo
    {
        public AbyssLayer Layer;
        public int TileY;
        public int AbyssTopY;
        public int AbyssBottomY;
        public float GlobalDepthInterpolant;
        public float LayerDepthInterpolant;
    }

    
}

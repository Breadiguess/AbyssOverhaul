using AbyssOverhaul.Core.DataStructures;

namespace AbyssOverhaul.Core.WorldGen
{
    public static class AbyssLayerRegistry
    {
        public static readonly List<AbyssLayer> Layers = new();

        internal static void Register(AbyssLayer layer)
        {
            Layers.Add(layer);
        }
    }
}

using AbyssOverhaul.Content.Layers.FossilShale;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.Tiles.Abyss;
using CalamityMod.Waters;

namespace AbyssOverhaul.Content.Layers.TheVoid
{
    internal class TheVoidLayer : AbyssLayer
    {
        public override int StartHeight => AbyssGenUtils.YAt(0.8f);

        public override int EndHeight => AbyssGenUtils.YAt(1);

        public override ModWaterStyle ModWaterStyle => VoidWater.Instance;

        public static TheVoidLayer Instance => ModContent.GetInstance<TheVoidLayer>();
        public override Dictionary<int, float> NPCSpawnPool => new()
        {

        };

        public override void ModifyGenTasks()
        {
            AddGenTask("", static (_, progress, config) =>
            {
                CreateArena(
                    progress,
                    AbyssGenUtils.MinX,
                    AbyssGenUtils.MaxX,
                    Instance.StartY,
                    Instance.EndY,
                    ModContent.TileType<Voidstone>()
                );
            });
        }

        //todo: fill in a solid chunk, before carving out an oval shape in the  middle of the layer.
        private static void CreateArena(
             GenerationProgress progress,
             int minX,
             int maxX,
             int topY,
             int bottomY,
             int tileType)
        {
            progress.Message = "Forming the Void arena";

            int paddingX = 40;
            int paddingY = 12;

            minX += paddingX;
            maxX -= paddingX;
            topY += paddingY;
            bottomY -= paddingY;

            if (maxX <= minX || bottomY <= topY)
                return;

            int centerX = (minX + maxX) / 2;
            int centerY = (topY + bottomY) / 2;

            int width = maxX - minX;
            int height = bottomY - topY;

            AbyssWorldGenHelper.ForceSolidRect(minX, maxX, topY, bottomY, tileType, true);

            int arenaRadiusX = (int)(width * 0.38f);
            int arenaRadiusY = (int)(height * 0.28f);

            AbyssWorldGenHelper.CarveBlob(centerX, centerY, arenaRadiusX, arenaRadiusY, 0.08f, true);

            AbyssWorldGenHelper.RemoveLonelyTiles(minX, maxX, topY, bottomY, maxNeighbors: 2, chanceDenominator: 1, fillWithWater: true);
            AbyssWorldGenHelper.FloodOpenSpace(minX, maxX, topY, bottomY);
            AbyssWorldGenHelper.ReframeArea(minX, maxX, topY, bottomY);

            progress.Set(1f);
        }
    }
}

using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Subworlds.WorldGen
{

    internal sealed class AbyssBootstrapPass : GenPass
    {
        public AbyssBootstrapPass() : base("Abyss bootstrap", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Stabilizing abyss entrance";
            Main.spawnTileX = AbyssSubworld.EntryTileX;
            Main.spawnTileY = AbyssSubworld.EntryTileY;
        }
    }

    internal sealed class AbyssSubworldBoundsPass : GenPass
    {
        public AbyssSubworldBoundsPass() : base("Abyss bounds", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Defining abyss bounds";

            const int sidePadding = 10;
            const int topPadding = 10;
            const int bottomPadding = 14;

            int minX = sidePadding;
            int maxX = Main.maxTilesX - 1 - sidePadding;
            int topY = topPadding;
            int bottomY = Main.maxTilesY - 1 - bottomPadding;
            int chasmX = Main.maxTilesX / 2;

            AbyssGenUtils.SetBounds(
                minX,
                maxX,
                topY,
                bottomY,
                chasmX,
                false,
                ModContent.GetInstance<AbyssOverhaul>()
            );
        }
    }

    internal sealed class AbyssSubworldAbyssPass : GenPass
    {
        public AbyssSubworldAbyssPass() : base("Abyss", 10f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Generating abyss";
            CustomAbyssHole.PlaceAbyssFromCurrentBounds();
        }
    }

    internal sealed class AbyssEntryPocketPass : GenPass
    {
        public AbyssEntryPocketPass() : base("Entry pocket", 1f) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Opening entry pocket";

            int x = AbyssSubworld.EntryTileX;
            int y = AbyssSubworld.EntryTileY;

            AbyssWorldGenHelper.CarveBlob(x, y, 44, 20, 0.35f, true);

            AbyssWorldGenHelper.CarveTunnelBlobLineSmooth(
                new Vector2(x, y + 10),
                new Vector2(AbyssGenUtils.ChasmX, AbyssGenUtils.TopY + 24),
                12,
                16,
                0.2f,
                true
            );

            AbyssWorldGenHelper.FloodOpenSpace(x - 60, x + 60, y - 30, y + 45);
            AbyssWorldGenHelper.ReframeArea(x - 65, x + 65, y - 35, y + 50);
        }
    }
}

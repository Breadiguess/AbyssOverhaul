using AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness.Tiles;
using CalamityMod.Tiles.Abyss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness
{
    internal class AbyssLightingGlobalTile : GlobalTile
    {


        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            return;
            if (Main.dedServ) 
                return;
            if (Main.gamePaused)
                return;

                Tile tile = Framing.GetTileSafely(i, j);
            if (!tile.HasTile)
                return;

            TileLightRegistry.TryEmit(i, j, type, tile);
        }
    }
}

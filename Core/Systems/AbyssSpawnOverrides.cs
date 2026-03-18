using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.Systems
{
    public class AbyssSpawnOverrides : GlobalNPC
    {
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            if (player is null || !player.active)
                return;

            if (!PresssureSystem.IsInAbyss(player))
                return;

            if (!AbyssGenUtils.IsWithinAbyss(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY))
                return;

            foreach (var layer in AbyssLayerRegistry.Layers)
            {
                if (!layer.AppliesToSpawn(spawnInfo))
                    continue;

                pool.Clear();
                layer.ModifySpawnPool(pool, spawnInfo);
                break;
            }
        }
    }
}
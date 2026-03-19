using CalamityMod.Tiles.Abyss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Core.Ecosystem
{
    public class NutritionSystem : ModSystem
    {
        public override void OnModLoad()
        {
            TileNutritionRegistry.Register(new()
            {
                TileType = ModContent.TileType<PlantyMush>(),
                BaseNutrition = 10,
                AllowedConsumers = FoodConsumerType.Herbivore,
                Kind = FoodKind.Plant

            });
        }

        public override void PostUpdateWorld()
        {
            if (Main.gamePaused)
                return;

            
            foreach(var pair in NutritionWorldData.State.ToArray())
            {
                Point16 pos = pair.Key;
                TileNutritionState state = pair.Value;

                Tile tile = Framing.GetTileSafely(pos); 

                if(!tile.HasTile ||!TileNutritionRegistry.tryGet(tile.TileType, out var def))
                {
                    NutritionWorldData.Remove(pos);
                    continue;
                }
                if(state.BitesRemaining <=0 && state.NextReplenishTick>0 && Main.GameUpdateCount >= state.NextReplenishTick)
                {
                    state.BitesRemaining = (byte)def.MaxBites;
                    state.NextReplenishTick = 0;
                }


            }


        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem
{
    internal class NutritionDebug : ModSystem
    {
        public override void PostDrawTiles()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, default, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix); ;

            foreach(var key in NutritionWorldData.State)
            {

                if (key.Key.ToWorldCoordinates().Distance(Main.LocalPlayer.Center) > 1000)
                    continue;
                Vector2 DrawPos = key.Key.ToWorldCoordinates() - Main.screenPosition;


                string msg = "";

                msg += TileNutritionAPI.GetNutritionAt(key.Key.X, key.Key.Y, FoodConsumerType.None) + $"\n";
                msg += key.Value.NextReplenishTick + $"\n";
                msg += key.Value.BitesRemaining + $"\n";

                Utils.DrawBorderString(Main.spriteBatch, msg, DrawPos, Color.White);
            }




            Main.spriteBatch.End();
        }
    }
}

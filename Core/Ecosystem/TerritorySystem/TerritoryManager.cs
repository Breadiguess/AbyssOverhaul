using AbyssOverhaul.Core.Ecosystem.Simulation;
using AbyssOverhaul.Core.Ecosystem.Simulation.AbyssOverhaul.Core.Ecosystem.Persistence;

namespace AbyssOverhaul.Core.Ecosystem.TerritorySystem
{
    internal class TerritoryManager : ModSystem
    {
        public override void PostUpdateNPCs()
        {
            TerritoryRegistry.RemoveDeadTerritories();
        }



        public override void PostDrawTiles()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            try
            {
                foreach ((Point _, EcologyCell cell) in EcologySystem.Instance.Cells)
                {
                    Utils.DrawRect(Main.spriteBatch, cell.WorldBounds, Color.White);
                }
                    
            }
            catch
            {

            }

            Main.spriteBatch.End();
        }
    }
}

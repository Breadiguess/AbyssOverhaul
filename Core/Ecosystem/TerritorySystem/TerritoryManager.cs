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

            foreach (var territory in TerritoryRegistry.Territories)
            {
                territory.Draw();
            }


            Main.spriteBatch.End();
        }
    }
}

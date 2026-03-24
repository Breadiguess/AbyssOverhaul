using Terraria.ModLoader;
using AbyssOverhaul.Core.NPCOverrides;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    public sealed class EcologyAutoloadSystem : ModSystem
    {
        public override void PostSetupContent()
       
        {
        }

        public override void Unload()
        {
            EcologyRegistry.Clear();
        }
    }
}
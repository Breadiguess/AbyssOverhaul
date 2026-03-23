using Terraria.ModLoader;
using AbyssOverhaul.Common.NPCs;
using AbyssOverhaul.Core.NPCOverrides;

namespace AbyssOverhaul.Common.Ecology
{
    public sealed class EcologyAutoloadSystem : ModSystem
    {
        public override void Load()
        {
            EcologyRegistry.Clear();

            foreach (ModNPC modNpc in Mod.GetContent<ModNPC>())
            {
                if (modNpc is IEcologyParticipant participant)
                    EcologyRegistry.Register(modNpc.Type, participant);
            }

            foreach (NPCBehaviorOverride behaviorOverride in Mod.GetContent<NPCBehaviorOverride>())
            {
                if (behaviorOverride is IEcologyParticipant participant)
                    EcologyRegistry.Register(behaviorOverride.NPCType, participant);
            }
        }

        public override void Unload()
        {
            EcologyRegistry.Clear();
        }
    }
}
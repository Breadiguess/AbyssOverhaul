using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.NPCOverrides
{
    [Autoload(Side = ModSide.Both)]
    public sealed class NPCOverrideDetourSystem : ModSystem
    {
        public override void Load()
        {
            On_NPC.AI += AIHook;
            On_NPC.FindFrame += FindFrameHook;
        }

       
        public override void Unload()
        {
            On_NPC.AI -= AIHook;
            On_NPC.FindFrame -= FindFrameHook;
        }

        private void AIHook(On_NPC.orig_AI orig, NPC self)
        {
            NPCBehaviorOverride ov = NPCOverrideRegistry.Get(self);

            if (ov is not null && ov.OverrideAI(self))
            {
                // We handled AI completely.
                // orig() is skipped, so vanilla + tML AI pipeline + Infernum override pipeline do not run.
                return;
            }

            orig(self);
        }

        private void FindFrameHook(On_NPC.orig_FindFrame orig, NPC self)
        {
            NPCBehaviorOverride ov = NPCOverrideRegistry.Get(self);

            if (ov is not null && ov.OverrideFindFrame(self))
                return;

            orig(self);
        }
    }
}
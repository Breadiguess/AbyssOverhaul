using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AbyssOverhaul.Core.NPCOverrides
{
    [Autoload(Side = ModSide.Client)]
    public sealed class NPCOverrideDrawDetourSystem : ModSystem
    {
        public override void Load()
        {
            if (Main.dedServ)
                return;

            On_Main.DrawNPCDirect += DrawNPCDirectHook;
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            On_Main.DrawNPCDirect -= DrawNPCDirectHook;
        }

        private void DrawNPCDirectHook(On_Main.orig_DrawNPCDirect orig, Main self, SpriteBatch spriteBatch, NPC npc, bool behindTiles, Vector2 screenPos)
        {
            NPCBehaviorOverride ov = npc.GetGlobalNPC<NPCOverrideGlobalNPC>().GetOverride(npc);

            if (ov is not null && ov.DrawDirect(npc, spriteBatch, screenPos, behindTiles))
                return;

            orig(self, spriteBatch, npc, behindTiles, screenPos);
        }
    }
}
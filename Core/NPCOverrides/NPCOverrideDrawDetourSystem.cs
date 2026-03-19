using System.Reflection;

public sealed class NPCOverrideDrawDetourSystem : ModSystem
{
    private static MethodInfo drawNPCDirectInnerMethod;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        drawNPCDirectInnerMethod = typeof(Main).GetMethod(
            "DrawNPCDirect_Inner",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (drawNPCDirectInnerMethod is null)
            throw new Exception("Could not find Main.DrawNPCDirect_Inner");

        On_Main.DrawNPCDirect += DrawNPCDirectHook2;
    }

    public override void Unload()
    {
        if (Main.dedServ)
            return;

        On_Main.DrawNPCDirect -= DrawNPCDirectHook2;
        drawNPCDirectInnerMethod = null;
    }

    private void DrawNPCDirectHook2(On_Main.orig_DrawNPCDirect orig, Main self, SpriteBatch spriteBatch, NPC npc, bool behindTiles, Vector2 screenPos)
    {
        NPCOverrideGlobalNPC g = npc.GetGlobalNPC<NPCOverrideGlobalNPC>();
        NPCBehaviorOverride ov = NPCOverrideRegistry.GetPrototype(npc);

        // Existing hard direct-draw path.
        if (ov is not null && ov.DrawDirect(npc, spriteBatch, screenPos, behindTiles))
            return;

        // New path: preserve normal draw pipeline, but suppress original PostDraw.
        if (g.SuppressOriginalPostDrawThisFrame)
        {
            DrawNPCDirectWithoutPostDraw(self, spriteBatch, npc, behindTiles, screenPos);
            return;
        }

        orig(self, spriteBatch, npc, behindTiles, screenPos);
    }

    private static void DrawNPCDirectWithoutPostDraw(Main self, SpriteBatch spriteBatch, NPC npc, bool behindTiles, Vector2 screenPos)
    {
        //yeah, this is hardcoded.... sorry....
        Color npcColor = npc.GetAlpha(Lighting.GetColor((int)npc.Center.X / 16, (int)npc.Center.Y / 16));

        NPCLoader.DrawEffects(npc, ref npcColor);

        if (NPCLoader.PreDraw(npc, spriteBatch, screenPos, npcColor))
        {
            object[] args =
            {
                    spriteBatch,
                    npc,
                    behindTiles,
                    screenPos,
                    npcColor
                };

            drawNPCDirectInnerMethod.Invoke(self, args);

            // because npcColor is passed by ref, pull the boxed value back out if needed
            npcColor = (Color)args[4];
        }

        //FUCK YOU FAPSOL
        //GET FUCKED YOU GREASY PEDO 
        // NPCLoader.PostDraw(npc, spriteBatch, screenPos, npcColor);
    }
}

using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Localization;

namespace AbyssOverhaul.Content.BehaviorOverrides.GooGazerNPC
{
    internal class GooGazer: NPCBehaviorOverride
    {
        public override int NPCType => ModContent.NPCType<Laserfish>();
        public static Asset<Texture2D> Tex;
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            typeName = Language.GetOrRegister($"Mods.AbyssOverhaul.NPCOverrides.LaserFish").Value;
        }
        public override void Load()
        {
            string path = this.GetPath();
            Tex = ModContent.Request<Texture2D>(path);
        }
        public ModularNpcBrain<PreyNpcContext> NpcBrain;

        public override void SetDefaults(NPC NPC)
        {
            
        }

        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Tex == null)
                return false;



            var tex = Tex.Value;
            Vector2 DrawPos = NPC.Center - screenPos;

            var frame = tex.Frame(1, 8, 0, 0);
            Main.EntitySpriteDraw(tex, DrawPos, frame, drawColor, NPC.rotation, frame.Size() / 2, NPC.scale, (-NPC.spriteDirection).ToSpriteDirection(), 0);


            return base.PreDraw(NPC, spriteBatch, screenPos, drawColor);
        }
    }
}

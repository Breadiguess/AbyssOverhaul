using AbyssOverhaul.Core.ModPlayers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.NPCOverrides
{
    public sealed class NPCOverrideGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        
       
        public override void SetDefaults(NPC npc)
        {
            NPCOverrideRegistry.Get(npc)?.SetDefaults(npc);
        }
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            NPCOverrideRegistry.Get(npc)?.OnSpawn(npc, source);
        }
      

        public override bool CheckDead(NPC npc)
        {
            NPCBehaviorOverride ov = NPCOverrideRegistry.Get(npc);
            if (ov is null)
                return true;

            return ov.CheckDead(npc);
        }

        public override void BossHeadSlot(NPC npc, ref int index)
        {
            NPCOverrideRegistry.Get(npc)?.BossHeadSlot(npc, ref index);
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            NPCOverrideRegistry.Get(npc)?.SendExtraAI(npc, binaryWriter);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            NPCOverrideRegistry.Get(npc)?.ReceiveExtraAI(npc, binaryReader);
        }

        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            NPCOverrideRegistry.Get(npc)?.OnHitPlayer(npc, target, hurtInfo);
        }



        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPCBehaviorOverride ov = NPCOverrideRegistry.Get(npc);
            if (ov is null)
                return base.PreDraw(npc, spriteBatch, screenPos, drawColor);

            return ov.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }
        
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPCBehaviorOverride ov = NPCOverrideRegistry.Get(npc);
            if (ov is not null)
                ov.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }
    }
}
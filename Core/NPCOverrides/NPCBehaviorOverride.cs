using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria.DataStructures;

namespace AbyssOverhaul.Core.NPCOverrides
{
    public abstract class NPCBehaviorOverride
    {
        /// <summary>
        /// The vanilla/modded NPC type this override is for.
        /// </summary>
        public abstract int NPCType { get; }

        /// <summary>
        /// Extra condition beyond NPC.type.
        /// Useful if you want to only override under certain configs/world states.
        /// </summary>
        public virtual bool ShouldOverride(NPC NPC) => NPC.type == NPCType;

        /// <summary>
        /// Runs once after the override is instantiated and registered.
        /// </summary>
        public virtual void Load() { }

        /// <summary>
        /// Runs from GlobalNPC.SetDefaults.
        /// </summary>
        public virtual void SetDefaults(NPC NPC) { }

        public virtual void OnSpawn(NPC NPC, IEntitySource source)
        {

        }

        
        /// <summary>
        /// Hard AI replacement. Return true if you handled AI and want to suppress orig.
        /// </summary>
        public virtual bool OverrideAI(NPC NPC) => false;

        public virtual void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
        }



        /// <summary>
        /// Hard FindFrame replacement. Return true if you handled framing and want to suppress orig.
        /// </summary>
        public virtual bool OverrideFindFrame(NPC NPC) => false;
        public virtual void PostDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) { }
        /// <summary>
        /// Soft draw replacement via GlobalNPC.PreDraw.
        /// Return false to suppress vanilla drawing.
        /// </summary>
        public virtual bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;

        /// <summary>
        /// Optional full draw replacement if you later detour Main.DrawNPCDirect.
        /// Return true if you fully drew the NPC and want to skip orig.
        /// </summary>
        public virtual bool DrawDirect(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, bool behindTiles) => false;

        public virtual bool CheckDead(NPC NPC) => true;

        public virtual void BossHeadSlot(NPC NPC, ref int index) { }

        public virtual void SendExtraAI(NPC NPC, BinaryWriter writer) { }

        public virtual void ReceiveExtraAI(NPC NPC, BinaryReader reader) { }
    }
}

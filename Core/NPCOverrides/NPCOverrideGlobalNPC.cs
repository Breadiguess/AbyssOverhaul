using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.NPCOverrides
{
    public sealed class NPCOverrideGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool SuppressOriginalPostDrawThisFrame = true;

        public NPCBehaviorOverride OverrideInstance;

        public NPCBehaviorOverride GetOverride(NPC npc)
        {
            if (OverrideInstance is not null)
                return OverrideInstance;

            OverrideInstance = NPCOverrideRegistry.CreateFor(npc);
            return OverrideInstance;
        }

        public NPCBehaviorOverride GetOverride(int npcType)
        {
            if (OverrideInstance is not null)
                return OverrideInstance;

            OverrideInstance = NPCOverrideRegistry.CreateFor(NPCLoader.npcs[npcType].NPC);
            return OverrideInstance;
        }



        public override void SetBestiary(NPC npc, BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // Important: use the prototype, not the per-entity instance.
            NPCOverrideRegistry.GetPrototype(npc)?.SetBestiary(npc, database, bestiaryEntry);
        }
        public override void SetDefaults(NPC npc)
        {
            GetOverride(npc)?.SetDefaults(npc);
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            GetOverride(npc)?.OnSpawn(npc, source);
        }

        public override void SpawnNPC(int npc, int tileX, int tileY)
        {
            GetOverride(npc)?.SpawnNPC(npc, tileX, tileY);
        }

        public override bool CheckActive(NPC npc)
        {
            bool result = base.CheckActive(npc);
            if ((GetOverride(npc)?.CheckActive(npc)).HasValue)
            {
                result = (GetOverride(npc)?.CheckActive(npc)).Value;
            }
            return result;
        }
        public override bool CheckDead(NPC npc)
        {
            NPCBehaviorOverride ov = GetOverride(npc);
            return ov?.CheckDead(npc) ?? true;
        }

        public override void BossHeadSlot(NPC npc, ref int index)
        {
            GetOverride(npc)?.BossHeadSlot(npc, ref index);
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            GetOverride(npc)?.SendExtraAI(npc, binaryWriter);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            GetOverride(npc)?.ReceiveExtraAI(npc, binaryReader);
        }

        #region Hit stuff
        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            GetOverride(npc)?.OnHitPlayer(npc, target, hurtInfo);
        }
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            GetOverride(npc)?.ModifyHitPlayer(npc, target, ref modifiers);
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            GetOverride(npc)?.OnHitByItem(npc, player, item, hit, damageDone);
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            GetOverride(npc)?.OnHitByProjectile(npc, projectile, hit, damageDone);
        }

        public override bool ModifyCollisionData(NPC npc, Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {

            if ((GetOverride(npc)?.ModifyCollisionData(npc, victimHitbox, ref immunityCooldownSlot, ref damageMultiplier, ref npcHitbox)).HasValue)
                return (GetOverride(npc)?.ModifyCollisionData(npc, victimHitbox, ref immunityCooldownSlot, ref damageMultiplier, ref npcHitbox)).Value;

            return base.ModifyCollisionData(npc, victimHitbox, ref immunityCooldownSlot, ref damageMultiplier, ref npcHitbox);
        }

        #endregion

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPCBehaviorOverride ov = GetOverride(npc);
            return ov?.PreDraw(npc, spriteBatch, screenPos, drawColor) ?? base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            GetOverride(npc)?.PostDraw(npc, spriteBatch, screenPos, drawColor);
        }

        //lets get this baby rolling !!
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            GetOverride(npc)?.ModifyTypeName(npc, ref typeName);
        }

    }
}
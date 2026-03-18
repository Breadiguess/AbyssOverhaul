using CalamityMod;
using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
#pragma warning disable CS8604
    internal class BlackTideEarringPlayer : ModPlayer
    {
        private ref readonly System.Collections.Generic.Dictionary<string, CooldownInstance> cooldowns
            => ref Player.Calamity().cooldowns;

        public const int NormalBlastCooldown = 60 * 5;
        public bool Active;


        public Vector2 MoonPos;


        public override void ResetEffects()
        {
            Active = false;
        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            if (!Active || npc is null || !npc.active || npc.friendly || npc.dontTakeDamage)
                return;

            ApplyMoonJudgment(npc);
        }

        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
        {
            if (!Active || proj is null || !proj.active || !proj.hostile)
                return;

            var sourceData = proj.GetGlobalProjectile<BlackTideSourceTrackingProjectile>();
            int npcIndex = sourceData.SourceNpcIndex;

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return;

            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.friendly)
                return;

            ApplyMoonJudgment(npc);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Active || target is null || !target.active)
                return;

            if (!proj.DamageType.CountsAsClass<RangedDamageClass>())
                return;

            TryTriggerBlacktideBlast(target, damageDone);
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Active || target is null || !target.active)
                return;

            if (!item.DamageType.CountsAsClass<RangedDamageClass>())
                return;

            TryTriggerBlacktideBlast(target, damageDone);
        }

        private void ApplyMoonJudgment(NPC npc)
        {
            BlackTideEarringNPC blacktide = npc.GetGlobalNPC<BlackTideEarringNPC>();
            blacktide.ApplyJudgmentHit(npc);

            switch (blacktide.JudgmentStage)
            {
                case 1:
                    // First hit: focused. Mostly visual / stateful.
                    break;

                case 2:
                    // Second hit: inflict lunacy.
                    ApplyLunacy(npc, 60 * 4);
                    break;

                case 3:
                    // Third hit: anchored.
                    ApplyLunacy(npc, 60 * 5);
                    break;
            }
        }

        private void TryTriggerBlacktideBlast(NPC target, int damageDone)
        {
            if (target.friendly || target.dontTakeDamage)
                return;

            BlackTideEarringNPC blacktide = target.GetGlobalNPC<BlackTideEarringNPC>();
            bool bypassCooldown = blacktide.IsAnchored;
            bool hasCooldown = cooldowns.ContainsKey(BlackTideEarringCooldown.ID);

            if (!bypassCooldown && hasCooldown)
                return;

            if (!bypassCooldown)
                Player.AddCooldown(BlackTideEarringCooldown.ID, NormalBlastCooldown);

            FireBlacktideBlast(target, damageDone);
        }

        private void FireBlacktideBlast(NPC target, int triggeringHitDamage)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            // Placeholder scaling.
            // Tune this however you want.
            int blastDamage = (int)(triggeringHitDamage * 0.75f);
            if (blastDamage < 1)
                blastDamage = 1;

            Vector2 spawnPosition = Player.Center;
            Vector2 velocity = Player.DirectionTo(target.Center);
            if (velocity == Vector2.Zero)
                velocity = -Vector2.UnitY;
            velocity *= 12f;

                int proj = Projectile.NewProjectile(
                 Player.GetSource_Accessory(Player.HeldItem),
                 spawnPosition,
                 velocity,
                 ModContent.ProjectileType<BlackTideBlastProjectile>(),
                 blastDamage,
                 0f,
                 Player.whoAmI,
                 target.whoAmI
             );

            ApplyLunacy(target, 60 * 3);

            BlackTideEarringNPC blacktide = target.GetGlobalNPC<BlackTideEarringNPC>();
            blacktide.BlacktideBlastHits++;

            if (blacktide.BlacktideBlastHits >= 5)
            {
                blacktide.BlacktideBlastHits = 0;
                SummonTheMoon(target, blastDamage);
            }

            target.netUpdate = true;
        }

        private void SummonTheMoon(NPC target, int damage)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            // TODO: replace with your real moon projectile / aura projectile.
            // Suggested behavior:
            // - spawn on target.Center
            // - follows / hovers
            // - applies Lunacy in radius every tick or every few ticks
            //
            // Projectile.NewProjectile(
            //     Player.GetSource_Accessory(Player.HeldItem),
            //     target.Center,
            //     Vector2.Zero,
            //     ModContent.ProjectileType<BlacktideMoonProjectile>(),
            //     damage,
            //     0f,
            //     Player.whoAmI,
            //     target.whoAmI
            // );
        }

        private static void ApplyLunacy(NPC target, int time)
        {
            // TODO: replace with your actual buff.
            target.AddBuff(ModContent.BuffType<LunacyDebuff>(), time);

            // Temporary placeholder if you want visible testing:
            target.AddBuff(BuffID.Confused, time);
        }
    }
#pragma warning restore CS8604
}
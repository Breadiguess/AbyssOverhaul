using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideBlastProjectile : ModProjectile
    {
        public ref float TargetIndex => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];

        public const float BaseSpeed = 15f;
        public const float HomingStrength = 0.16f;
        public const float MaxTrackDistance = 1400f;
        public const int LunacyTime = 60 * 3;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BlackBolt;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;

            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.alpha = 0;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Time++;

            NPC target = GetTarget();
            if (target is not null && target.active && !target.friendly && !target.dontTakeDamage)
            {
                Vector2 toTarget = target.Center - Projectile.Center;
                float distance = toTarget.Length();

                if (distance <= MaxTrackDistance && distance > 8f)
                {
                    Vector2 desiredVelocity = toTarget.SafeNormalize(Vector2.UnitY) * BaseSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingStrength);
                }
            }

            if (Projectile.velocity.LengthSquared() < 0.01f)
                Projectile.velocity = -Vector2.UnitY * BaseSpeed;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            SpawnAmbientDust();

            Lighting.AddLight(Projectile.Center, 0.08f, 0.10f, 0.16f);
        }

        private NPC GetTarget()
        {
            int index = (int)TargetIndex;
            if (index >= 0 && index < Main.maxNPCs)
            {
                NPC npc = Main.npc[index];
                if (npc.active)
                    return npc;
            }
            return null;
        }

        private void SpawnAmbientDust()
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.ShadowbeamStaff,
                    -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120,
                    new Color(120, 100, 170),
                    Main.rand.NextFloat(0.75f, 1.25f)
                );
                d.noGravity = true;
            }

            if (Main.rand.NextBool(5))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.GemAmethyst,
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    100,
                    new Color(180, 130, 255),
                    Main.rand.NextFloat(0.55f, 0.95f)
                );
                d.noGravity = true;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target is null || !target.active || target.friendly || target.dontTakeDamage)
                return false;

            NPC intendedTarget = GetTarget();
            if (intendedTarget is not null && intendedTarget.active)
                return target.whoAmI == intendedTarget.whoAmI;

            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ApplyLunacy(target, LunacyTime);
            ScreenShakeSystem.StartShakeAtPoint(target.Center, 2);
            if (Main.myPlayer == Projectile.owner)
            {
                BlackTideEarringNPC data = target.GetGlobalNPC<BlackTideEarringNPC>();
                data.BlacktideBlastHits++;

                if (data.BlacktideBlastHits >= 5)
                {
                    data.BlacktideBlastHits = 0;

                    Player owner = Main.player[Projectile.owner];
                    if (owner.active)
                    {
                        BlackTideEarringPlayer modPlayer = owner.GetModPlayer<BlackTideEarringPlayer>();
                        //modPlayer.TriggerMoonFromBlast(target, Projectile.damage);
                    }
                }

                target.netUpdate = true;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = -0.25f, Volume = 0.7f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.4f, Volume = 0.55f }, Projectile.Center);

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.8f, 2.8f);

                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.ShadowbeamStaff,
                    velocity,
                    100,
                    new Color(130, 120, 170),
                    Main.rand.NextFloat(0.9f, 1.4f)
                );
                d.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemAmethyst,
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    110,
                    new Color(210, 170, 255),
                    Main.rand.NextFloat(0.8f, 1.15f)
                );
                d.noGravity = true;
            }
        }

        private static void ApplyLunacy(NPC target, int time)
        {
            
            target.AddBuff(ModContent.BuffType<LunacyDebuff>(), time);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin = tex.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = i / (float)Projectile.oldPos.Length;
                Color color = Color.Lerp(new Color(25, 25, 35, 255), new Color(170, 140, 255, 250), 1f - completion) * (1f - completion) * 0.75f;

                Main.EntitySpriteDraw(
                    tex,
                    drawPos,
                    null,
                    color,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * MathHelper.Lerp(0.55f, 1f, 1f - completion),
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 255, 255, 220),
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(160, 120, 255, 100),
                Projectile.rotation,
                origin,
                Projectile.scale * 1.35f,
                SpriteEffects.None
            );

            return false;
        }
    }
}
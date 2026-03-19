using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Localization;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.Eschaton
{
    [PierceResistException]
    public class Eschaton_Holdout : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        public override int AssignedItemID => ModContent.ItemType<EschatonItem>();

        public override LocalizedText DisplayName => CalamityUtils.GetItemName<EschatonItem>();
        public override string Texture => EschatonItem.Path;
        public override float HitboxOutset => 175;

        public override Vector2 HitboxSize => new Vector2(210, 210) * Projectile.scale * 1.05f;
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45);

        public override Vector2 SpriteOrigin => new(0, 166);
        public Vector2 mousePos;
        public Vector2 aimVel;
        public bool doSwing = true;
        public bool postSwing = false;
        public float fadeIn = 0;
        public int useAnim;
        public int swingCount;
        public bool finalFlip = false;
        public bool swingSound = true;
        public int armoredHits = 0;


        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;//TrueMeleeDamageClass.Instance;


            const int slashLength = 34;
            _slashPositions = new Vector2[slashLength];
            _slashRotations = new float[slashLength];
            _slashScale = 2;
            //Projectile.extraUpdates = 1;

        }

        public override void WhenSpawned()
        {
            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
            Projectile.knockBack = 0;
            Projectile.scale = 1;
            Projectile.ai[1] = -1;

            mousePos = Owner.Calamity().mouseWorld;
            aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65;


            if (mousePos.X < Owner.Center.X) Owner.direction = -1;
            else Owner.direction = 1;

            FlipAsSword = Owner.direction == -1;

            ResetSlash();

        }
        private int previousUseAnim;
        private bool initializedUseAnim;
        public override void UseStyle()
        {
            if (AnimationProgress == 0)// (Owner.itemAnimation == Owner.itemAnimationMax)
            {
                //Main.NewText(Owner.itemAnimationMax - Owner.HeldItem.useTime);
                Projectile.scale = Owner.GetAdjustedItemScale(Owner.HeldItem);
                useAnim = Owner.itemAnimationMax;
            }
            int currentUseAnim = Math.Max(1, Owner.itemAnimationMax);

            if (!initializedUseAnim && AnimationProgress == 0)
            {
                previousUseAnim = currentUseAnim;
                initializedUseAnim = true;

                //Main.NewText($"ResetUseAnim:{useAnim}, {Animation}");
            }

            if (currentUseAnim != previousUseAnim)
            {
                float progress = Animation / previousUseAnim;
                Animation = Utils.Clamp(progress * currentUseAnim, 0f, currentUseAnim + 1f);
                previousUseAnim = currentUseAnim;
            }

            AnimationProgress = Animation % useAnim;
            DrawUnconditionally = false;

            if (CanHit || postSwing)
                mousePos = Owner.Center - aimVel;
            else
            {
                mousePos = Owner.Calamity().mouseWorld;
            }

            if (CanHit)
                fadeIn = MathHelper.Lerp(fadeIn, 1, 0.3f);
            else
                fadeIn = MathHelper.Lerp(fadeIn, 0, 0.35f);


            if (!doSwing)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                    Projectile.localNPCImmunity[i] = 0;

                Projectile.numHits = 0;

                mousePos = Owner.Calamity().mouseWorld;
                aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65;
                CanHit = false;
                if (mousePos.X < Owner.Center.X) Owner.direction = -1;
                else Owner.direction = 1;
                FlipAsSword = Owner.direction == -1;

                doSwing = true;
                swingCount++;
                finalFlip = false;
                swingSound = true;
                armoredHits = 0;
                ResetSlash();
            }
            else
            {
                if (!CanHit && !postSwing)
                {

                    if (mousePos.X < Owner.Center.X) Owner.direction = -1;
                    else Owner.direction = 1;

                    ResetSlash();
                }
                else
                {
                    if ((Owner.Center - aimVel).X < Owner.Center.X) Owner.direction = -1;
                    else Owner.direction = 1;
                }

                Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(mousePos) + MathHelper.ToRadians(45f), 0.1f);

                if (AnimationProgress < useAnim / 1.5f)
                {
                    aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65;
                    CanHit = false;
                    postSwing = false;
                    if (AnimationProgress == 0)
                    {
                        doSwing = false;
                        Projectile.ai[1] = -Projectile.ai[1];
                    }
                    RotationOffset = MathHelper.Lerp(RotationOffset, MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * (1 + Utils.GetLerpValue(useAnim * 0.8f, useAnim, Animation, true) * 0.35f)), 0.2f);
                }
                else
                {
                    if (!finalFlip)
                    {
                        FlipAsSword = Owner.direction < 0;
                    }

                    float time = AnimationProgress - useAnim / 3;
                    float timeMax = useAnim - useAnim / 3;

                    if (time >= (int)(timeMax * 0.4f) && swingSound)
                    {
                        SoundStyle fire = new("CalamityMod/Sounds/Item/HeavySwing");
                        SoundEngine.PlaySound(fire with { Volume = 0.8f, Pitch = -0.2f * (Owner.GetModPlayer<EschatonPlayer>().FinalityStacks / EschatonPlayer.MaxFinalityStacks), pitchVariance = 0.3f }, Projectile.Center);
                        swingSound = false;
                    }
                    if (time > (int)(timeMax * 0.3f) && time < (int)(timeMax * 0.8f))
                    {
                        CanHit = true;
                    }
                    else
                        CanHit = false;

                    Owner.SetDummyItemTime(2);
                    float attackT = Utilities.InverseLerp(0, timeMax, time);


                    float eased = CalamityUtils.ExpInOutEasing(attackT, 1);
                    float swingDegrees = MathHelper.Lerp(
                        150f * Projectile.ai[1] * Owner.direction,
                        120f * -Projectile.ai[1] * Owner.direction,
                        eased
                    );


                    RotationOffset = MathHelper.Lerp(
                       RotationOffset,
                       MathHelper.ToRadians(swingDegrees),
                       0.2f
                   );

                    if (time >= timeMax)
                    {
                        Owner.controlUseItem = true;
                        doSwing = false; postSwing = true;

                    }



                }

                UpdateSlash();
            }

            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {

            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.life <= 0 && target.realLife == -1 && Projectile.numHits > 0)
                Projectile.numHits -= 1;
            if (damageDone <= 2)
                armoredHits++;


            if (Projectile.numHits == 0)
            {
                Owner.SetScreenshake(6.5f);
                SoundStyle fire = new("CalamityMod/Sounds/NPCHit/ThanatosHitOpen1");
                SoundEngine.PlaySound(fire with { Volume = 0.75f, Pitch = -0.1f }, Projectile.Center);
                // SoundStyle fire2 = new("CalamityMod/Sounds/Item/FinalDawnSlash");
                //SoundEngine.PlaySound(fire2 with { Volume = 0.65f, Pitch = Main.rand.NextFloat(-0.2f, -0.3f) }, Projectile.Center);
            }

            int heal = MathHelper.Clamp(20 - Projectile.numHits * 12, 1, 20);
            if (Projectile.numHits < 10)
            {
                Owner.DoLifestealDirect(target, heal, 0.5f);
            }

            target.AddBuff(ModContent.BuffType<HadopelagicPressure>(), 60 * 5);
            target.AddBuff(ModContent.BuffType<Nightwither>(), 60 * 6);

            Vector2 SpawnPos = target.Center + new Vector2(target.width + 100, 0).RotatedBy(target.AngleFrom(Owner.Center));
            int Type = ModContent.ProjectileType<EschatonSoulProjectile>();
            Projectile a = Projectile.NewProjectileDirect(Projectile.GetItemSource_FromThis(), SpawnPos, new Vector2(3, 0).RotatedByRandom(1), Type, Projectile.damage / 2, 0);
            a.As<EschatonSoulProjectile>().TargetWhoami = target.whoAmI;

        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Owner.Calamity().mouseRight)
            {
                modifiers.SourceDamage *= 0;
                modifiers.FinalDamage.Flat = 0.1f;
            }
            else
            {
                float minMult = 0.5f;
                int hitsToMinMult = 15;
                float damageMult = Utils.Remap(Projectile.numHits - armoredHits, 0, hitsToMinMult, 1, minMult, true);
                modifiers.SourceDamage *= damageMult;
            }
        }


        #region SwingTrail
        public static float SwordLength = 132f;
        private float _previousSlashAngle;
        private bool _hasPreviousSlashAngle;

        public void ResetSlash()
        {
            _slashScale = 1f;
            SwordLength = 130;
            float slashAngle = FinalRotation - MathHelper.PiOver4;
            Vector2 slashOffset = new Vector2(SwordLength * Projectile.scale * _slashScale, 0f).RotatedBy(slashAngle);

            for (int i = 0; i < _slashPositions.Length; i++)
            {
                _slashPositions[i] = slashOffset;
                _slashRotations[i] = slashAngle + MathHelper.PiOver2;
            }

            _previousSlashAngle = slashAngle;
            _hasPreviousSlashAngle = true;
        }

        public void UpdateSlash()
        {
            float currentAngle = FinalRotation - MathHelper.PiOver4;

            if (!_hasPreviousSlashAngle)
            {
                _previousSlashAngle = currentAngle;
                _hasPreviousSlashAngle = true;
            }

            float delta = MathHelper.WrapAngle(currentAngle - _previousSlashAngle);

            // Lower = smoother arc, but consumes history faster.
            float maxStep = MathHelper.ToRadians(6f);
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) / maxStep));

            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                float angle = _previousSlashAngle + delta * t;
                Vector2 offset = new Vector2(SwordLength * Projectile.scale * _slashScale, 0f).RotatedBy(angle);

                for (int i = _slashPositions.Length - 1; i > 0; i--)
                {
                    _slashPositions[i] = _slashPositions[i - 1];
                    _slashRotations[i] = _slashRotations[i - 1];
                }

                _slashPositions[0] = offset;
                _slashRotations[0] = angle + MathHelper.PiOver2;
            }

            _previousSlashAngle = currentAngle;
        }
        private VertexStrip _slashStrip;
        private Vector2[] _slashPositions;
        private float[] _slashRotations;
        private float _slashScale;

        private void DrawSlash()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            /*
            var trailShader = ShaderManager.GetShader("AbyssOverhaul.LonginusSlash");


            trailShader.SetTexture(ModContent.Request<Texture2D>("AbyssOverhaul/Assets/Textures/T_VoronoiNoiseCA001").Value, 0, SamplerState.PointClamp);


            trailShader.TrySetParameter("uTime", Main.GlobalTimeWrappedHourly);
            trailShader.TrySetParameter("uWorldViewProjection", Main.GameViewMatrix.NormalizedTransformationmatrix);
            trailShader.TrySetParameter("uColor", Color.White.ToVector4() * 1.0f);
            trailShader.Apply();
            */
            // Rendering primitives involves setting vertices of each triangle to form quads
            // This does it for us
            // Have a list of positions and rotations to create vertices, width function to determine how far vertices are from the center
            // Color function determines each vertex's color, which can be used in the shader
            _slashStrip ??= new VertexStrip();
            _slashStrip.PrepareStrip(_slashPositions, _slashRotations, TrailColorFunction, TrailWidthFunction, Owner.Center - Main.screenPosition, _slashPositions.Length, true);
            _slashStrip.DrawTrail();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
        private float TrailWidthFunction(float p)
        {
            return 100 * Projectile.scale * _slashScale * Projectile.direction;
        }

        private Color TrailColorFunction(float p)
        {
            return Color.Lerp
            (
                Color.White with
                {
                    A = 120
                },
                Color.DarkCyan with
                {
                    A = 1
                },
                p
            );
        }
        #endregion
        public override bool PreDraw(ref Color lightColor)
        {
            // Only draw the projectile if the projectile's owner is currently using the item this projectile is attached to.
            if ((useAnim > 0 || DrawUnconditionally) && Owner.ItemAnimationActive)
            {
                if (AnimationProgress > 0)
                    DrawSlash();
                Asset<Texture2D> tex = ModContent.Request<Texture2D>(Texture);
                Asset<Texture2D> glowTex = ModContent.Request<Texture2D>(EschatonItem.Path + "_Glow");

                float r = FlipAsSword ? MathHelper.ToRadians(90) : 0f;




                Main.EntitySpriteDraw(tex.Value, Projectile.Center - Main.screenPosition + new Vector2(0, Owner.gfxOffY), tex.Frame(1, FrameCount, 0, Frame), lightColor, Projectile.rotation + RotationOffset + r, FlipAsSword ? new Vector2(tex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin, Projectile.scale, spriteEffects != SpriteEffects.None ? spriteEffects : FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
                Main.EntitySpriteDraw(glowTex.Value, Projectile.Center - Main.screenPosition + new Vector2(0, Owner.gfxOffY), glowTex.Frame(1, FrameCount, 0, Frame), Color.White, Projectile.rotation + RotationOffset + r, FlipAsSword ? new Vector2(glowTex.Width() - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin, Projectile.scale, spriteEffects != SpriteEffects.None ? spriteEffects : FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            }

            //Utils.DrawLine(Main.spriteBatch, Projectile.Center, Projectile.Center + new Vector2(500, 0).RotatedBy(FinalRotation-MathHelper.PiOver4), Color.White);
            return false;
        }
    }
}

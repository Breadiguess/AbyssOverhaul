using AbyssOverhaul.Core.Graphics.Shaders;
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
    public class Eschaton_Holdout : ModProjectile, ILocalizedModType
    {
        public enum SwingState
        {
            Windup,
            Swing,
            Recover
        }
        public SwingState State = SwingState.Windup;
        public ref Player Owner => ref Main.player[Projectile.owner];
        public int AssignedItemID => ModContent.ItemType<EschatonItem>();

        public override LocalizedText DisplayName => CalamityUtils.GetItemName<EschatonItem>();
        public override string Texture => EschatonItem.Path;

        

        public float FinalRotation { get; private set; }

        public Vector2 mousePos =>  Owner.Calamity().mouseWorld;
        public Vector2 aimVel;
        public bool doSwing = true;
        public bool postSwing = false;
        public float fadeIn = 0;
        public int useAnim;
        public int swingCount;
        public bool finalFlip = false;
        public bool swingSound = true;
        public int armoredHits = 0;


        public static Asset<Texture2D> SwordTex;
        public static Asset<Texture2D> glowTex;


        public override void SetStaticDefaults()
        {
            SwordTex = ModContent.Request<Texture2D>(EschatonItem.Path);
            glowTex = ModContent.Request<Texture2D>(EschatonItem.Path + "_Glow");
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = -1;
            
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            
            Projectile.DamageType = DamageClass.MeleeNoSpeed;//TrueMeleeDamageClass.Instance;


            const int slashLength = 34;
            _slashPositions = new Vector2[slashLength];
            _slashRotations = new float[slashLength];
            _slashScale = 2;
            //Projectile.extraUpdates = 1;

        }
        public override bool PreAI()
        {
            if(Owner.HeldItem.type != AssignedItemID || Owner.dead || Owner.CCed)
            {
                Projectile.active = false;

            }

            else
            {
                Owner.heldProj = this.Projectile.whoAmI;
                Projectile.timeLeft = 2;
            }

                return true;
        }
        public override void AI()
        {
            Projectile.Center = Owner.Center;

            StateMachine();
        }

        void StateMachine()
        {
            switch (State)
            {
                case SwingState.Windup:
                    FinalRotation = FinalRotation.AngleLerp(MathHelper.ToRadians(-60) + Owner.AngleTo(Owner.Calamity().mouseWorld), 0.2f);
                    State = SwingState.Swing;
                    break;

                case SwingState.Swing:

                    FinalRotation = FinalRotation.AngleLerp(MathHelper.ToRadians(120), 0.15f);
                    break;

                case SwingState.Recover:

                    break;
            }
        }

      

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Collision.CheckAABBvAABBCollision(projHitbox.Center.ToVector2(), projHitbox.Size(), projHitbox.Center.ToVector2() + new Vector2(SwordLength, 0).RotatedBy(FinalRotation), projHitbox.Size());
            //return base.Colliding(projHitbox, targetHitbox);
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
          


            Effect trailShader = ShaderHolder.EschatonSlash.Value;
            ShaderUtilities.BindTexture(ModContent.Request<Texture2D>("AbyssOverhaul/Assets/Textures/T_VoronoiNoiseCA001").Value, 0, SamplerState.PointWrap);
            ShaderUtilities.SetParameter(trailShader, "uTime", Main.GlobalTimeWrappedHourly);
            ShaderUtilities.SetParameter(trailShader, "uWorldViewProjection", Main.GameViewMatrix.NormalizedTransformationmatrix);
            ShaderUtilities.SetParameter(trailShader, "uColor", Color.White.ToVector3());
            trailShader.CurrentTechnique.Passes[0].Apply();
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
            var tex = SwordTex.Value;
            Vector2 DrawPos = Projectile.Center - Main.screenPosition;
            Vector2 Origin = new Vector2(0, tex.Height);
            Main.EntitySpriteDraw(tex, DrawPos, null, lightColor, FinalRotation, Origin, Projectile.scale, SpriteEffects.None);



            Utils.DrawLine(Main.spriteBatch, Projectile.Center, Projectile.Center + new Vector2(500, 0).RotatedBy(FinalRotation-MathHelper.PiOver4), Color.White);
            return false;
        }
    }
}

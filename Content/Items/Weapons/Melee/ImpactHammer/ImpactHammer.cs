using AbyssOverhaul.Content.Items.Accessories.BlackTide;
using BreadLibrary.Common.IK;
using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Sounds;
using CalamityMod;
using CalamityMod.NPCs;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    public class ImpactHammer : ModProjectile, ILocalizedModType
    {

        public ref float Time => ref Projectile.ai[0];
        public ref Player Owner => ref Main.player[Projectile.owner];
        public override string LocalizationCategory => "Items.Weapons.Melee";



        public static Asset<Texture2D> HeadTex;
        public static Asset<Texture2D> ArmTex;
        public bool HitReady = false;
        public bool HitRealeased;
        public enum State
        {
            Idle,
            Charging,
            Hitting,
        }
        public State CurrentState
        {
            get => (State)Projectile.ai[1];
            set => Projectile.ai[1] = (float)value;
        }


        public float ExtensionAmount => 12 * -ChargeInterpolant;

        public float ChargeInterpolant;

        public float ChargePitchVariance
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }
        public override void Load()
        {
            string path = this.GetPath();
            HeadTex = ModContent.Request<Texture2D>($"{path}Head");
            ArmTex = ModContent.Request<Texture2D>($"{path}Arm");
        }

        public LoopedSoundInstance? Compression;
        public LoopedSoundInstance? ChargeHeld;

        public IKSkeletonAnalytic ArmJoint;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DismountsPlayersOnHit[Type] = true;
            ProjectileID.Sets.DontCancelChannelOnKill[Type] = true;
            ProjectileID.Sets.NoLiquidDistortion[Type] = true;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HitReady);
            writer.Write(HitRealeased);
            writer.Write(ChargeInterpolant);
            writer.Write(HasPlayedSound);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HitReady = reader.ReadBoolean();
            HitRealeased = reader.ReadBoolean();
            ChargeInterpolant = reader.ReadSingle();
            HasPlayedSound = reader.ReadBoolean();
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 24;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }


        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
        }
        private readonly HashSet<int> deflectedProjectiles = new();
        public override void AI()
        {
            Vector2 newAim = Owner.Calamity().mouseWorld;

            SyncedAimWorld = newAim;


            ManageIK();
            DoPlayerCheck();
            SetPosition();

            if (Compression is not null)
            {
                Compression.Update(Projectile.Center, sound =>
                {
                    sound.Volume = 0.4f * Utilities.InverseLerpBump(0, 30, 59, 60, 60f * ChargeInterpolant);
                    sound.Pitch = 0.1f * ChargeInterpolant + ChargePitchVariance;
                });
            }

            if (ChargeHeld is not null)
            {
                ChargeHeld.Update(Projectile.Center, sound =>
                {
                    sound.Volume = 0.02f;
                    sound.Pitch = 1+1*MathF.Sin(Time*0.1f);

                }
                );
            }

            StateMachine();
            if (HitRealeased)
                TryDeflectProjectiles();


            if (!Owner.channel)
                ChargeInterpolant = float.Lerp(ChargeInterpolant, 0f, 0.2f);

            Time++;
        }



        #region StateMachine
        public Vector2 SyncedAimWorld;
        private bool HasPlayedSound = false;
        private void StateMachine()
        {
            switch (CurrentState)
            {
                case State.Idle:
                    if (Owner.controlUseItem)
                    {
                        Owner.StartChanneling();

                        Compression?.Stop();
                        ChargePitchVariance = Main.rand.NextFloat(0f, 0.4f);
                        Compression = LoopedSoundManager.CreateNew(
                            Assets.Sounds.Items.Melee.ImpactHammer.HydralicRetraction.Asset with { pitchVariance = 0.2f, PauseBehavior = PauseBehavior.PauseWithGame },
                            () => !Projectile.active
                        );

                        CurrentState = State.Charging;
                        Time = -1f;
                        HasMadeVisual = false;
                        HitReady = false;
                        HitRealeased = false;
                        HasPlayedSound = false;
                        Projectile.netUpdate = true;
                    }
                    break;

                case State.Charging:
                    if (Owner.channel)
                    {
                        float oldCharge = ChargeInterpolant;
                        bool oldReady = HitReady;

                        ChargeInterpolant = Utilities.InverseLerp(0f, 60f, Time);

                        if (ChargeInterpolant >= 1f)
                        {
                            ChargeInterpolant = 1f;
                            ChargeHeld = LoopedSoundManager.CreateNew(
                                Assets.Sounds.Items.Melee.ImpactHammer.ChargeHeldLoop.Asset with { pitchVariance = 0.2f, PauseBehavior = PauseBehavior.PauseWithGame },
                                () => !Projectile.active || HitRealeased
                            );
                            if (!HasPlayedSound)
                            {
                                SoundEngine.PlaySound(
                                    Assets.Sounds.Items.Melee.ImpactHammer.ChargeCompleted.Asset with { pitchVariance = 0.2f },
                                    Projectile.Center
                                );
                                HasPlayedSound = true;
                            }

                            HitReady = true;
                        }

                        if (oldCharge != ChargeInterpolant || oldReady != HitReady)
                            Projectile.netUpdate = true;
                    }
                    else
                    {
                        ChargeHeld?.Stop();
                        if (!HitReady)
                        {
                            CurrentState = State.Idle;
                            Time = 0f;
                            Projectile.netUpdate = true;
                        }
                        else
                        {
                            HasPlayedSound = false;
                            CurrentState = State.Hitting;
                            Time = -1f;
                            Projectile.netUpdate = true;
                        }
                    }
                    break;

                case State.Hitting:
                    Vector2 HeadPos = Projectile.Center + new Vector2(40, 0).RotatedBy(Projectile.rotation);
                    if (!HasPlayedSound )
                    {
                        SoundEngine.PlaySound(
                            Assets.Sounds.Items.Melee.ImpactHammer.MechanicalImpact.Asset with { pitchVariance = 0.3f, volume = 0.7f},
                            Owner.Center
                        );
                        HasPlayedSound = true;

                        ImpactHammer_HitParticle Particle = new ImpactHammer_HitParticle();
                        Particle.Prepare(HeadPos, Projectile.rotation, 60);
                        ParticleEngine.ShaderParticles.Add( Particle );
                    }
                    if(Time <=10)
                    {
                        ImpactHammer_CloudParticle particle = new();
                        particle.Prepare(HeadPos, Projectile.rotation.ToRotationVector2().RotatedByRandom(0.3f) * 10, Main.rand.NextFloat(3), 40);
                        ParticleEngine.ShaderParticles.Add(particle);

                    }



                    HitRealeased = true;
                    ChargeInterpolant = float.Lerp(ChargeInterpolant, -0.4f, 0.45f);

                    if (Time > 10f && HitRealeased)
                    {
                        HitRealeased = false;
                        Projectile.netUpdate = true;
                    }

                    // You currently never leave Hitting, so add an exit:
                    if (Time > 16f)
                    {
                        Projectile.ResetLocalNPCHitImmunity();
                        CurrentState = State.Idle;

                        deflectedProjectiles.Clear();
                        Time = 0f;
                        HitReady = false;
                        HitRealeased = false;
                        HasPlayedSound = false;
                        Projectile.netUpdate = true;
                    }
                    break;
            }
        }

        #endregion


        #region Collisions And Damage
        public bool HasMadeVisual = false;
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            info.Dodgeable = false;

        }
        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            modifiers.KnockbackImmunityEffectiveness *= 0;
            modifiers.Knockback *= 4;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.CanBeMoved(true))
            {
                target.noTileCollide = false;
                Vector2 launchVel = Utils.DirectionTo(Owner.Center, Owner.Calamity().mouseWorld);
                float launchPower = 30 * 1;
                target.MoveNPC(launchVel, launchPower * 0.5f, true);

                // Remove knockback resist, just like it used to
                target.knockBackResist = 1;

                // Apply tile collison damage (is boosted even further if both final bosses are gone)
                float damageMults = 8;
                int damage = (int)(Projectile.damage * damageMults);
                target.GetGlobalNPC<CalamityTileCollisionHarmNPC>().ApplyCollisionDamage(target, Owner, damage, launchVel * launchPower, 5f, true);
            }
        }
        public override bool CanHitPlayer(Player target)
        {
            return HitRealeased;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return HitRealeased;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (ArmJoint is null || Owner is null || !Owner.active)
                return false;

            if (projHitbox.Intersects(targetHitbox))
                return true;

            if (Owner.Hitbox.Intersects(targetHitbox))
                return true;

            const int rootPadding = 4;
            Rectangle rootBox = Utils.CenteredRectangle(ArmJoint.Root, new Vector2(rootPadding * 2));
            if (rootBox.Intersects(targetHitbox))
                return true;

            float collisionPoint = 0f;
            bool upperHit = Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                ArmJoint.Root,
                ArmJoint.Joint,
                2f,
                ref collisionPoint
            );

            Rectangle hammerBox = GetHammerBoundingBox();

            if (hammerBox.Intersects(targetHitbox))
                return true;

            return upperHit;
        }
        #endregion

        #region Helper

        #region Deflect

        private void TryDeflectProjectiles()
        {
            if (Owner.whoAmI != Main.myPlayer)
                return;

            if (ArmJoint is null || !Owner.active)
                return;

            Rectangle hammerSearchArea = GetHammerBoundingBox();
            hammerSearchArea.Inflate(48, 48);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];

                if (!CanDeflectProjectile(other))
                    continue;

                if (deflectedProjectiles.Contains(i))
                    continue;

                if (!other.Hitbox.Intersects(hammerSearchArea))
                    continue;

                if (!ProjectileIntersectsHammer(other))
                    continue;

                DeflectProjectile(other);
                deflectedProjectiles.Add(i);
            }
        }
        private bool ProjectileIntersectsHammer(Projectile other)
        {
            Rectangle targetHitbox = other.Hitbox;

            // Hammer body
            if (Projectile.Hitbox.Intersects(targetHitbox))
                return true;

            // Let it catch things overlapping the player as well.
            if (Owner.Hitbox.Intersects(targetHitbox))
                return true;

            // Root / shoulder area
            const int rootPadding = 4;
            Rectangle rootBox = Utils.CenteredRectangle(ArmJoint.Root, new Vector2(rootPadding * 2));
            if (rootBox.Intersects(targetHitbox))
                return true;

            float collisionPoint = 0f;
            bool upperHit = Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                ArmJoint.Root,
                ArmJoint.Joint,
                8f,
                ref collisionPoint
            );

            collisionPoint = 0f;
            bool lowerHit = Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                ArmJoint.Joint,
                ArmJoint.Tip,
                12f,
                ref collisionPoint
            );

            return upperHit || lowerHit;
        }
        private void DeflectProjectile(Projectile other)
        {
            Vector2 origin = other.Center;

            // Use the hammer/mouse direction as the center of the cone.
            Vector2 coneDirection = (Owner.Calamity().mouseWorld - Owner.Center)
                .SafeNormalize(Vector2.UnitX * Owner.direction);

            const float coneHalfAngle = MathHelper.PiOver2;
            const float maxSeekDistance = 900f;

            Entity target = FindRedirectTarget(other, origin, coneDirection, coneHalfAngle, maxSeekDistance);

            float speed = Math.Max(other.velocity.Length(), 30f);

            Vector2 finalDirection = coneDirection;
            if (target is not null)
                finalDirection = (target.Center - other.Center).SafeNormalize(coneDirection);

            other.velocity = finalDirection * speed;

            // Convert hostile enemy projectile into a friendly reflected shot.
            other.hostile = false;
            other.friendly = true;
            other.owner = Owner.whoAmI;
            other.DamageType = DamageClass.Melee;
            other.damage = (int)(Projectile.damage * 10f);
            other.netUpdate = true;
            if (!HasMadeVisual)
            {
                ImpactHammer_HitParticle particle = new ImpactHammer_HitParticle();
                particle.Prepare(Projectile.Center + new Vector2(40, 0).RotatedBy(Projectile.rotation), other.velocity.ToRotation(), isAReflect: true);
                ParticleEngine.ShaderParticles.Add(particle);
                HasMadeVisual = false;
            }
            other.netUpdate = true;
            
            // Nice audiovisual feedback.
            SoundEngine.PlaySound(
                Assets.Sounds.Items.Melee.ImpactHammer.Reflect.Asset with { pitchVariance = 0.2f, volume = 3 },
                other.Center
            );
        }
        private bool CanDeflectProjectile(Projectile other)
        {
            if (!other.active)
                return false;

            if (other.type == this.Projectile.type)
                return false;
            // Ignore harmless visuals and things that cannot deal damage.
            if (other.damage <= 0)
                return false;

            // Normal enemy projectile
            if (other.hostile && !other.friendly)
                return true;

            // PvP projectile from another player
            if (IsHostilePvPProjectile(other))
                return true;

           
            return true;
        }
        private bool IsHostilePvPProjectile(Projectile other)
        {
            if (!Main.projectile.IndexInRange(other.whoAmI))
                return false;

            if (other.GetGlobalProjectile<BlackTideSourceTrackingProjectile>().SourceNpcIndex > 0)
                return false;
            if (!other.friendly || other.hostile)
                return false;

            if (other.owner < 0 || other.owner >= Main.maxPlayers)
                return false;

            Player attacker = Main.player[other.owner];

            if (attacker is null || !attacker.active || attacker.dead)
                return false;

            if (attacker.whoAmI == Owner.whoAmI)
                return false;

            // Both players must be in PvP.
            if (!attacker.hostile || !Owner.hostile)
                return false;

            // Same team usually cannot hurt each other in PvP.
            if (attacker.team != 0 && attacker.team == Owner.team)
                return false;

            return false;
        }
        private Rectangle GetHammerBoundingBox()
        {
            Vector2 direction = (ArmJoint.Tip - ArmJoint.Joint).SafeNormalize(Vector2.UnitX * Projectile.direction);

            Vector2 headCenter = Projectile.Center + direction * 28f;

            const int width = 75;
            const int height = 75;

            return new Rectangle(
                (int)(headCenter.X - width * 0.5f),
                (int)(headCenter.Y - height * 0.5f),
                width,
                height
            );
        }
        private Entity FindRedirectTarget(Projectile other, Vector2 origin, Vector2 coneDirection, float coneAngle, float maxDistance)
        {
            Entity bestTarget = null;
            float bestScore = float.MaxValue;

            // NPC targets
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5)
                    continue;

                // If the projectile came from an NPC/enemy source, redirected shots should usually hit NPC enemies.
                // Skip town NPCs / critters / invalid targets as needed.
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toTarget = npc.Center - origin;
                float distance = toTarget.Length();
                if (distance > maxDistance || distance <= 0.001f)
                    continue;

                Vector2 dirToTarget = toTarget / distance;
                float angle = coneDirection.AngleBetween(dirToTarget);
                if (angle > coneAngle)
                    continue;

                // Prefer closer targets and ones nearer the cone center.
                float score = distance + angle * 200f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }
        #endregion
        private void ManageIK()
        {

            ArmJoint ??= new IKSkeletonAnalytic()
            {
                UpperLength = ArmTex.Value.Height * 0.7f,
                LowerLength = 12f
            };

            ArmJoint.Root = Owner.MountedCenter + new Vector2(-8f * Owner.direction, 4f);
            ///fixed !!
            Vector2 target = Owner.Calamity().mouseWorldDeltaFromPlayer+Owner.Center;
            Vector2 pole = ArmJoint.Root + new Vector2(-10f * Owner.direction, 28f);
            ArmJoint.Solve(target, pole);
        }
        private void SetPosition()
        {

            Projectile.Center = ArmJoint.Joint;
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.Calamity().mouseWorld.AngleFrom(Projectile.Center), 0.4f);
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * 12;
        }
        private void DoPlayerCheck()
        {
            if (Owner.HeldItem.type == ModContent.ItemType<ImpactHammerItem>() && !Owner.dead)
            {
                Projectile.timeLeft = 2;
                Owner.heldProj = this.Projectile.whoAmI;
                Owner.direction = Projectile.velocity.X.DirectionalSign();
            }
        }
        #endregion


        #region DrawCode

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        private void DrawHead(Vector2 PosOffset, float rot, Color lightColor)
        {
            Vector2 DrawPos = Projectile.Center - Main.screenPosition + PosOffset;

            var tex = HeadTex.Value;

            Main.EntitySpriteDraw(tex, DrawPos, null, lightColor, rot, new Vector2(0, tex.Height / 2f), 1, 0);
        }
        private void DrawArmPiece(Color lightColor)
        {
            var tex = ArmTex.Value;

            float rot = ArmJoint.Root.AngleTo(ArmJoint.Joint);
            Main.EntitySpriteDraw(tex, ArmJoint.Root - Main.screenPosition, null, lightColor, rot - MathHelper.PiOver2, tex.Size() / 2, 1, Projectile.direction.ToSpriteDirection());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;


            Vector2 DrawPos = Projectile.Center - Main.screenPosition;

            DrawArmPiece(lightColor);

            float rot = ArmJoint.Joint.AngleTo(ArmJoint.Tip);
            Vector2 Offset = new Vector2(ExtensionAmount, 2 * -Projectile.direction).RotatedBy(rot);

            DrawHead(Offset, rot, lightColor);


            SpriteEffects flip = Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;


            Main.EntitySpriteDraw(tex, DrawPos, null, lightColor, rot, tex.Size() / 2 - new Vector2(-10, 0), Projectile.scale, flip);


            Utils.DrawRect(Main.spriteBatch, GetHammerBoundingBox(), Color.White);
            Utils.DrawLine(Main.spriteBatch, Projectile.Center, Projectile.Center+ ArmJoint.Tip.AngleFrom(ArmJoint.Joint).ToRotationVector2()*100, Color.Red);

            if (ArmJoint is not null)
            {
                //Utils.DrawLine(Main.spriteBatch, ArmJoint.Root, ArmJoint.Joint, Color.White);
                //Utils.DrawLine(Main.spriteBatch, ArmJoint.Joint, ArmJoint.Tip, Color.White, Color.Wheat, 10);
            }

            string msg = "";
            msg += CurrentState.ToString() + $"\n";
            //Utils.DrawBorderString(Main.spriteBatch, msg, DrawPos, Color.White);
            return false;
        }
        #endregion
    }
}
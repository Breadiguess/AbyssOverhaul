using AbyssOverhaul.Content.Items.Accessories.BlackTide;
using BreadLibrary.Common.IK;
using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Graphics.PixelationShit;
using BreadLibrary.Core.ScreenShake;
using BreadLibrary.Core.Sounds;
using BreadLibrary.Core.Verlet;
using CalamityMod;
using CalamityMod.Items.Armor.Hydrothermic;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.ImpactHammer
{
    public class ImpactHammer : ModProjectile, ILocalizedModType, IDrawPixellated
    {

       
        public override string LocalizationCategory => "Items.Weapons.Melee";

        #region Values

        public ref float Time => ref Projectile.ai[0];
        public ref Player Owner => ref Main.player[Projectile.owner];
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

        public LoopedSoundInstance? Compression;
        public LoopedSoundInstance? ChargeHeld;

        public IKSkeletonAnalytic ArmJoint;
        public Vector2 SyncedAimWorld;

        public bool ThisChargeIsFaster;
        private bool HasPlayedSound = false;


        public VerletChain FunChain;
        private readonly HashSet<int> deflectedProjectiles = new();

        public int ChargeTime => !ThisChargeIsFaster ? 60 : 30;

        #endregion


        public override void Load()
        {
            string path = this.GetPath();
            HeadTex = ModContent.Request<Texture2D>($"{path}Head");
            ArmTex = ModContent.Request<Texture2D>($"{path}Arm");
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HitReady);
            writer.Write(HitRealeased);
            writer.Write(ChargeInterpolant);
            writer.Write(HasPlayedSound);
            writer.Write(ThisChargeIsFaster);
            writer.WriteVector2(SyncedAimWorld);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HitReady = reader.ReadBoolean();
            HitRealeased = reader.ReadBoolean();
            ChargeInterpolant = reader.ReadSingle();
            HasPlayedSound = reader.ReadBoolean();
            ThisChargeIsFaster = reader.ReadBoolean();
            SyncedAimWorld = reader.ReadVector2();

        }


        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DismountsPlayersOnHit[Type] = true;
            ProjectileID.Sets.DontCancelChannelOnKill[Type] = true;
            ProjectileID.Sets.NoLiquidDistortion[Type] = true;
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
        public override void AI()
        {
            Vector2 newAim = Owner.Calamity().mouseWorld;

            SyncedAimWorld = newAim;


            ManageIK();
            DoPlayerCheck();
            SetPosition(); 
            MangeVerlet();
            if (Compression is not null)
            {
               

                Compression.Update(Projectile.Center, sound =>
                {
                    sound.Volume = 0.4f * Utilities.InverseLerpBump(0, ChargeTime/2, ChargeTime-4, ChargeTime, ChargeTime * ChargeInterpolant);
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

                        
                        ChargeInterpolant = Utilities.InverseLerp(0f, ChargeTime, Time);

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

                            for(int i = 0; i< 2; i++)
                            {

                                Vector2 Direction = (ArmJoint.Joint.AngleFrom(ArmJoint.Tip)+ i/2f*MathHelper.PiOver2 * -Projectile.direction).ToRotationVector2().RotatedByRandom(0.2f) * 10;
                                MediumMistParticle mist = new MediumMistParticle(ArmJoint.Joint, Direction,
                                 Main.rand.NextBool(3) ? Color.LightSteelBlue : Color.SteelBlue, Color.LightSlateGray, Main.rand.NextFloat(0.4f, 0.65f), 130);
                                GeneralParticleHandler.SpawnParticle(mist);
                            }
                            HitReady = true;
                        }

                        if (oldCharge != ChargeInterpolant || oldReady != HitReady)
                            Projectile.netUpdate = true;
                    }
                    else
                    {
                        ThisChargeIsFaster = false;
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
                        ScreenShakeSystem.ShakeAt(HeadPos, 20, 10);
                        SoundEngine.PlaySound(
                            Assets.Sounds.Items.Melee.ImpactHammer.MechanicalImpact.Asset with { pitchVariance = 1f, volume = 0.7f},
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
                        particle.Prepare(HeadPos, Projectile.rotation.ToRotationVector2().RotatedByRandom(1f) * 10, Main.rand.NextFloat(3), 40);
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
                 HasMadeVisual = true;
                ThisChargeIsFaster = true;
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
        private void MangeVerlet()
        {
            if(FunChain is null)
            {
                FunChain = new VerletChain(10, 2, Projectile.Center);
            }
            FunChain.Positions[0] = Projectile.Center;
            FunChain.Positions[^1] = Projectile.Center + new Vector2(Projectile.width/2, -8*Projectile.direction).RotatedBy(Projectile.rotation);
            FunChain.Simulate(Vector2.zeroVector, Projectile.Center,2, 0.7f);
        }

        private void SetPosition()
        {

            Projectile.Center = ArmJoint.Joint + Main.rand.NextVector2Unit() * ChargeInterpolant*0.5f;
            Projectile.rotation = Projectile.rotation.AngleLerp(Owner.Calamity().mouseWorld.AngleFrom(Projectile.Center), 0.4f);
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * 12;
        }
        private void DoPlayerCheck()
        {
            if (Owner.HeldItem.type == ModContent.ItemType<ImpactHammerItem>() && !Owner.dead)
            {
                Projectile.timeLeft = 2;
                Owner.heldProj = this.Projectile.whoAmI;
                Owner.direction = (SyncedAimWorld - Owner.Center).X.DirectionalSign();
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None, 0);
            }
        }
        #endregion


        #region DrawCode

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        private void DrawHead(Vector2 PosOffset, float rot, Color lightColor, SpriteEffects flip)
        {
            Vector2 DrawPos = Projectile.Center - Main.screenPosition + PosOffset;

            var tex = HeadTex.Value;

            Main.EntitySpriteDraw(tex, DrawPos, null, lightColor, rot, new Vector2(0, tex.Height / 2f), 1, flip);
        }
        private void DrawArmPiece(Color lightColor)
        {
            var tex = ArmTex.Value;

            float rot = ArmJoint.Root.AngleTo(ArmJoint.Joint);
            Main.EntitySpriteDraw(tex, ArmJoint.Root - Main.screenPosition, null, lightColor, rot - MathHelper.PiOver2, tex.Size() / 2, 1, Projectile.direction.ToSpriteDirection());
        }

        #region Primitive
        private BasicEffect ChainEffect;
        private VertexPositionColorTexture[] chainVertices;

        private short[] chainIndices;
        private void EnsureChainEffect()
        {
            if (Main.dedServ)
                return;

            if (ChainEffect is null || ChainEffect.IsDisposed)
            {
                ChainEffect = new BasicEffect(Main.instance.GraphicsDevice)
                {
                    VertexColorEnabled = true,
                    TextureEnabled = true
                };
            }
        }
        private void DrawFunChainPrimitive(Vector2[] points, float width, Color color)
        {
            if (points is null || points.Length < 2 || Main.dedServ)
                return;

            EnsureChainEffect();

            GraphicsDevice gd = Main.instance.GraphicsDevice;

            int pointCount = points.Length;
            int vertexCount = pointCount * 2;
            int indexCount = (pointCount - 1) * 6;

            if (chainVertices is null || chainVertices.Length != vertexCount)
                chainVertices = new VertexPositionColorTexture[vertexCount];

            if (chainIndices is null || chainIndices.Length != indexCount)
            {
                chainIndices = new short[indexCount];
                int idx = 0;

                for (int i = 0; i < pointCount - 1; i++)
                {
                    short a = (short)(i * 2);
                    short b = (short)(i * 2 + 1);
                    short c = (short)(i * 2 + 2);
                    short d = (short)(i * 2 + 3);

                    chainIndices[idx++] = a;
                    chainIndices[idx++] = b;
                    chainIndices[idx++] = c;

                    chainIndices[idx++] = c;
                    chainIndices[idx++] = b;
                    chainIndices[idx++] = d;
                }
            }

            float halfWidth = width * 0.5f;

            Vector2[] segmentNormals = new Vector2[pointCount - 1];
            for (int i = 0; i < pointCount - 1; i++)
            {
                Vector2 diff = points[i + 1] - points[i];
                if (diff.LengthSquared() < 0.0001f)
                    diff = Vector2.UnitX;

                diff.Normalize();
                segmentNormals[i] = diff.RotatedBy(MathHelper.PiOver2);
            }

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 normal;

                if (i == 0)
                {
                    normal = segmentNormals[0];
                }
                else if (i == pointCount - 1)
                {
                    normal = segmentNormals[pointCount - 2];
                }
                else
                {
                    Vector2 n0 = segmentNormals[i - 1];
                    Vector2 n1 = segmentNormals[i];

                    if (Vector2.Dot(n0, n1) < 0f)
                        n1 = -n1;

                    Vector2 miter = n0 + n1;

                    if (miter.LengthSquared() < 0.0001f)
                        normal = n1;
                    else
                    {
                        miter.Normalize();

                        float denom = Vector2.Dot(miter, n1);
                        if (MathF.Abs(denom) < 0.15f)
                            denom = 0.15f * MathF.Sign(denom == 0f ? 1f : denom);

                        float miterLength = halfWidth / denom;

                        // Prevent absurd spikes on sharp bends.
                        miterLength = MathHelper.Clamp(miterLength, -halfWidth * 2f, halfWidth * 2f);

                        normal = miter * miterLength / halfWidth;
                    }
                }

                Vector2 offset = normal * halfWidth;

                Vector2 left = points[i] - offset;
                Vector2 right = points[i] + offset;

                float u = i / (float)(pointCount - 1);


                Color Actual = color.MultiplyRGB(Lighting.GetColor(points[i].ToTileCoordinates()));
                chainVertices[i * 2] = new VertexPositionColorTexture(
                    new Vector3(left - Main.screenPosition, 0f),
                    Actual,
                    new Vector2(u, 0f)
                );

                chainVertices[i * 2 + 1] = new VertexPositionColorTexture(
                    new Vector3(right - Main.screenPosition, 0f),
                   Actual,
                    new Vector2(u, 1f)
                );
            }

            ChainEffect.World = Matrix.Identity;
            ChainEffect.View = Matrix.identity;
            ChainEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0f,
                Main.screenWidth,
                Main.screenHeight,
                0f,
                0f,
                1f
            );
            ChainEffect.Texture = TextureAssets.MagicPixel.Value;

            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.WireFrame};
            gd.SamplerStates[0] = SamplerState.LinearClamp;

            foreach (EffectPass pass in ChainEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    chainVertices,
                    0,
                    vertexCount,
                    chainIndices,
                    0,
                    indexCount / 3
                );
            }
        }
        /// <summary>
        /// this is so stupid bruh.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="subdivisionsPerSegment"></param>
        /// <returns></returns>
        private static Vector2[] SubdividePointsLinear(Vector2[] points, int subdivisionsPerSegment)
        {
            if (points is null || points.Length < 2)
                return points;

            if (subdivisionsPerSegment <= 1)
                return points;

            int segmentCount = points.Length - 1;
            int newCount = segmentCount * subdivisionsPerSegment + 1;
            Vector2[] result = new Vector2[newCount];

            int index = 0;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[i + 1];

                for (int j = 0; j < subdivisionsPerSegment; j++)
                {
                    float t = j / (float)subdivisionsPerSegment;
                    result[index++] = Vector2.Lerp(start, end, t);
                }
            }

            result[index] = points[^1];
            return result;
        }
        private static Vector2[] SubdividePointsCatmullRom(Vector2[] points, int subdivisionsPerSegment)
        {
            if (points is null || points.Length < 2)
                return points;

            if (subdivisionsPerSegment <= 1 || points.Length < 3)
                return SubdividePointsLinear(points, subdivisionsPerSegment);

            int segmentCount = points.Length - 1;
            List<Vector2> result = new List<Vector2>(segmentCount * subdivisionsPerSegment + 1);

            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 p0 = points[Math.Max(i - 1, 0)];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = points[Math.Min(i + 2, points.Length - 1)];

                for (int j = 0; j < subdivisionsPerSegment; j++)
                {
                    float t = j / (float)subdivisionsPerSegment;
                    result.Add(Vector2.CatmullRom(p0, p1, p2, p3, t));
                }
            }

            result.Add(points[^1]);
            return result.ToArray();
        }
        #endregion
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;


            Vector2 DrawPos = Projectile.Center - Main.screenPosition;

            DrawArmPiece(lightColor);

            float rot = ArmJoint.Joint.AngleTo(ArmJoint.Tip);
            Vector2 Offset = new Vector2(ExtensionAmount, 2 * -Projectile.direction).RotatedBy(rot);

            SpriteEffects flip = Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            DrawHead(Offset, rot, lightColor, flip);

            


            Main.EntitySpriteDraw(tex, DrawPos, null, lightColor, rot, tex.Size() / 2 - new Vector2(-10, 0), Projectile.scale, flip);


            //Utils.DrawRect(Main.spriteBatch, GetHammerBoundingBox(), Color.White);
            //Utils.DrawLine(Main.spriteBatch, Projectile.Center, Projectile.Center+ ArmJoint.Tip.AngleFrom(ArmJoint.Joint).ToRotationVector2()*100, Color.Red);

            

         
            return false;
        }

        PixelLayer IDrawPixellated.PixelLayer => PixelLayer.AbovePlayer;
        void IDrawPixellated.DrawPixelated(SpriteBatch spriteBatch)
        {
            if (FunChain is not null && FunChain.Positions is { Length: >= 2 })
            {
                Vector2[] smoothedPoints = SubdividePointsCatmullRom(FunChain.Positions, 4);
                DrawFunChainPrimitive(smoothedPoints, 1f, Color.White);

            }
        }
        #endregion
    }
}
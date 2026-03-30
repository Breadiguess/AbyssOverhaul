using BreadLibrary.Core.Verlet;
using CalamityMod;
using System.IO;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Summoner
{
    internal class SurveyUnitProjectile : ModProjectile
    {
        #region Values
        private struct WingState
        {
            public float WingRotation;
            public float ThrusterRotation;
            public Vector2 Anchor;
            public Vector2 Tip;
            public Vector2 ForceDirection;
        }

        private readonly Vector2[] _wingOffsets =
        {
            new Vector2(12f, 3f),   // right wing anchor
            new Vector2(-12f, 3f),  // left wing anchor
        };

        private readonly WingState[] _wings = new WingState[2];

        public ref Player Owner => ref Main.player[Projectile.owner];

        public bool HasReachedTargetLocation;
        public bool HasTarget;
        public Vector2 TargetLocation;

        public Vector2 EyeDir;
        private Vector2 _lastSteering;
        private Vector2 _visualForceRequest;

        private const float MaxSpeed = 10f;
        private const float MaxAcceleration = 0.45f;
        private const float Drag = 0.96f;
        private const float ArriveRadius = 12f;
        private const float SlowRadius = 96f;

        private const float HoverLiftVisualBias = 1.35f;
        private const float MaxThrusterTilt = 0.75f; // radians from straight down
        private const float ThrusterLerp = 0.18f;
        private const float WingLerp = 0.16f;

        private float MaxBodyLean = 0.40f; // keep it subtle
        private float BodyRotationLerp = 0.10f;

        private float WingLength = 20f;
        private float ThrusterLength = 8f;
        private const float ExhaustVisualLength = 16f;

        public VerletChain Antennae;


        public enum State
        {
            Debug,
            Idle,
            InspectOre,

            Fight
        }

        public State CurrentState
        {
            get => (State)Projectile.ai[1];
            set => Projectile.ai[1] = (float)value;
        }
        private struct ManipulatorArm
        {
            public IKSkeleton IKSkeleton;

            public ManipulatorArm(IKSkeleton _skeleton)
            {
                IKSkeleton = _skeleton;
            }
        }


        private List<ManipulatorArm> _Arms;

        public List<Vector2> ArmTargets;
        public int CurrentArmAmount;




        #endregion
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 20;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.timeLeft = 2;

            if (Antennae is null)
            {
                Antennae = new(5, 6, Projectile.Center);
            }

            if (_Arms is null)
            {
                _Arms = new List<ManipulatorArm>();
                ArmTargets = new List<Vector2>();
                IKSkeleton.JointSetup[] b = new IKSkeleton.JointSetup[]
               {
                    new(20,0, 180),
                    new(20,0, 60)
               };

                _Arms.Add(new(new(b)));
                ArmTargets.Add(Projectile.Center);

            }

            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.owner == Main.myPlayer && Owner.controlUseItem)
            {
                SetTarget(Owner.Calamity().mouseWorld);
            }

            Antennae.Simulate(Vector2.zeroVector, Projectile.TopLeft, -2, 0.9f, 10, false);

            StateMachine();

            UpdateArmTargeting();
            UpdateIK();
            UpdateMovement();
            UpdateVisuals();
        }
        #region StateMachine
        private void StateMachine()
        {
            switch (CurrentState)
            {
                case State.Debug:
                    CurrentState = State.Idle;
                    break;
                case State.Idle:
                    TargetLocation = Owner.Center + new Vector2(40 * Owner.direction, -40);
                    break;


                case State.InspectOre:

                    break;


                case State.Fight:

                    break;




            }
        }

        #endregion



        public void UpdateArmTargeting()
        {
            for (int i = 0; i < ArmTargets.Count; i++)
            {
                var t = ArmTargets[i];

                t = Projectile.Center + Projectile.AngleTo(Main.MouseWorld).ToRotationVector2() * 40;

                ArmTargets[i] = t;
            }
        }

        public void AddArm()
        {

            IKSkeleton.JointSetup[] b = new IKSkeleton.JointSetup[]
            {
                new(30,0, 0),
                new(40,0, 0)
            };


            _Arms.Append(new(new(b)));
            ArmTargets.Append(Projectile.Center);
        }




        private void UpdateIK()
        {
            if (_Arms is null || ArmTargets is null)
                return;

            for (int i = 0; i < _Arms.Count; i++)
            {
                var Arm = _Arms[i];
                Arm.IKSkeleton.Update(Projectile.Bottom, ArmTargets[i]);
                _Arms[i] = Arm;
            }

        }

        private void SetTarget(Vector2 newTarget)
        {
            HasTarget = true;
            HasReachedTargetLocation = false;

            if (Vector2.DistanceSquared(TargetLocation, newTarget) > 4f * 4f)
            {
                TargetLocation = newTarget;
                Projectile.netUpdate = true;
            }
        }
        #region DroneMovements
        private void UpdateMovement()
        {
            _lastSteering = Vector2.Zero;

            if (!HasTarget)
            {
                Projectile.velocity *= 0.90f;
                UpdateBodyRotation(Vector2.Zero);
                _visualForceRequest = -Vector2.UnitY;
                return;
            }

            Vector2 toTarget = TargetLocation - Projectile.Center;
            float distance = toTarget.Length();

            if (distance <= ArriveRadius)
            {
                Projectile.velocity *= 0.84f;

                if (Projectile.velocity.LengthSquared() < 0.03f)
                {
                    Projectile.velocity = Vector2.Zero;
                    HasReachedTargetLocation = true;
                }
            }
            else
            {
                HasReachedTargetLocation = false;

                float speedFactor = MathHelper.Clamp(distance / SlowRadius, 0.15f, 1f);
                float desiredSpeed = MaxSpeed * speedFactor;

                Vector2 desiredVelocity = toTarget.SafeNormalize(Vector2.Zero) * desiredSpeed;
                Vector2 steering = desiredVelocity - Projectile.velocity;

                if (steering.LengthSquared() > MaxAcceleration * MaxAcceleration)
                    steering = steering.SafeNormalize(Vector2.Zero) * MaxAcceleration;

                _lastSteering = steering;
                Projectile.velocity += steering;
            }

            Projectile.velocity *= Drag;

            if (Projectile.velocity.LengthSquared() > MaxSpeed * MaxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * MaxSpeed;

            UpdateBodyRotation(_lastSteering);

            // Visual-only force request:
            // upward lift + movement correction
            _visualForceRequest = new Vector2(_lastSteering.X * 2.4f, _lastSteering.Y * 1.2f) + new Vector2(0f, -HoverLiftVisualBias);

            if (_visualForceRequest.LengthSquared() < 0.0001f)
                _visualForceRequest = -Vector2.UnitY;

            Projectile.velocity += Vector2.UnitY * MathF.Sin(Main.GameUpdateCount * 0.05f) * 0.2f;
        }

        private void UpdateBodyRotation(Vector2 steering)
        {
            // Slight lean only. Set targetLean = 0f if you want perfectly upright.
            float targetLean = MathHelper.Clamp(-Projectile.velocity.X * 0.018f, -MaxBodyLean, MaxBodyLean);
            Projectile.rotation = Projectile.rotation.AngleLerp(targetLean, BodyRotationLerp);
        }

        private void UpdateVisuals()
        {
            // Thruster exhaust should point opposite the force being applied to the drone.
            Vector2 exhaustDirection = (-_visualForceRequest).SafeNormalize(Vector2.UnitY);
            float baseThrusterRotation = exhaustDirection.ToRotation();

            // Straight down in Terraria screen space is +Pi/2.
            baseThrusterRotation = ClampAngleAround(baseThrusterRotation, MathHelper.PiOver2, MaxThrusterTilt);

            // Small stabilization correction so the two thrusters are not perfectly identical all the time.
            float stabilizationOffset = MathHelper.Clamp(
                MathHelper.WrapAngle(-Projectile.rotation) * 1.4f - Projectile.velocity.X * 0.01f,
                -0.18f,
                0.18f
            );
            Vector2 localVelocity = -Projectile.velocity.RotatedBy(-Projectile.rotation);
            float speedFactor = Utils.Remap(Projectile.velocity.Length(), 0f, 6f, 0f, 1f, true);


            float verticalFactor = MathHelper.Clamp(localVelocity.Y / 5f, -1f, 1f);
            float horizontalFactor = MathHelper.Clamp(localVelocity.X / 5f, -1f, 1f);

            for (int i = 0; i < _wings.Length; i++)
            {
                bool isRightWing = i == 0;
                float sideSign = isRightWing ? 1f : -1f;

                Vector2 anchor = Projectile.Center + _wingOffsets[i].RotatedBy(Projectile.rotation);
                _wings[i].Anchor = anchor;

                float wingBaseRotation = isRightWing ? 0f : MathHelper.Pi;

                float thrusterInfluence = MathHelper.WrapAngle(baseThrusterRotation - MathHelper.PiOver2) * 0.15f;

                // Upward movement should make both wing tips go upward.
                // For the right wing that means rotating negative.
                // For the left wing that means rotating positive.
                float verticalWingOffset = -verticalFactor * 0.45f * speedFactor;
                float mirroredVerticalOffset = -sideSign * verticalWingOffset;

                float sweepOffset = horizontalFactor * 0.10f * speedFactor;

                float wingTargetRotation =
                    wingBaseRotation +
                    Projectile.rotation +
                    thrusterInfluence +
                    mirroredVerticalOffset +
                    sweepOffset;

                _wings[i].WingRotation = _wings[i].WingRotation.AngleLerp(wingTargetRotation, 0.2f);

                Vector2 wingDir = _wings[i].WingRotation.ToRotationVector2();
                Vector2 tip = anchor + wingDir * WingLength;
                _wings[i].Tip = tip;

                float thrusterTargetRotation = baseThrusterRotation + stabilizationOffset * sideSign;
                thrusterTargetRotation = ClampAngleAround(thrusterTargetRotation, MathHelper.PiOver2, MaxThrusterTilt);

                _wings[i].ThrusterRotation = _wings[i].ThrusterRotation.AngleLerp(thrusterTargetRotation, ThrusterLerp);
                _wings[i].ForceDirection = -_wings[i].ThrusterRotation.ToRotationVector2();


                Vector2 start = (_wings[i].Anchor + _wings[i].Tip) / 2f;
                Point? DustPos = LineAlgorithm.RaycastTo(start, start - Vector2.UnitX.RotatedBy(_wings[i].ForceDirection.ToRotation()) * 100, debug: false);
                if (DustPos.HasValue)
                {
                    float EndLength = -start.Distance(DustPos.Value.ToWorldCoordinates());
                    if (Main.rand.NextBool((int)Math.Abs(EndLength) / 5 + 1))
                    {
                        Dust a = Dust.NewDustPerfect(start + Vector2.UnitX.RotatedBy(_wings[i].ForceDirection.ToRotation()) * EndLength, DustID.Cloud, _wings[i].ForceDirection.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.4f, 1f));
                        a.fadeIn = -0.4f;
                    }

                }
            }
        }
        #endregion

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HasTarget);
            writer.Write(HasReachedTargetLocation);
            writer.Write(TargetLocation.X);
            writer.Write(TargetLocation.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HasTarget = reader.ReadBoolean();
            HasReachedTargetLocation = reader.ReadBoolean();
            TargetLocation = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;

            Texture2D bodyTexture = TextureAssets.Projectile[Type].Value;
            Vector2 bodyOrigin = bodyTexture.Size() * 0.5f;

            var Eyetex = ModContent.Request<Texture2D>(this.GetPath() + "_Eye").Value;



            if (Antennae is not null)
            {
                for (int i = 0; i < Antennae.Positions.Length - 1; i++)
                {
                    Vector2 start = Antennae.Positions[i];
                    Vector2 end = Antennae.Positions[i + 1];
                    Utils.DrawLine(spriteBatch, start, end, Color.Black);


                    if (i == Antennae.Positions.Length - 2)
                    {
                        Main.EntitySpriteDraw(Eyetex, Antennae.Positions[^1] - Main.screenPosition, null, Color.White, 0, Eyetex.Size() / 2f, 1, 0);
                    }
                }
            }

            DrawWings(ref lightColor);

            if (HasTarget)
            {
                DrawCrosshair(spriteBatch, TargetLocation, 6f, Color.Cyan);
                DrawLine(spriteBatch, Projectile.Center, TargetLocation, Color.Cyan * 0.35f, 1f);
            }
            Main.EntitySpriteDraw(
             bodyTexture,
             Projectile.Center - Main.screenPosition - new Vector2(0, 3),
             null,
             lightColor,
             Projectile.rotation,
             bodyOrigin,
             Projectile.scale,
             SpriteEffects.None,
             0f
         );



            if (_Arms is not null)
            {
                for (int x = 0; x < _Arms.Count; x++)
                {
                    var Arm = _Arms[x];

                    if (Arm.IKSkeleton is not null)
                        for (int i = 0; i < Arm.IKSkeleton.JointCount; i++)
                        {
                            Utils.DrawLine(spriteBatch, Arm.IKSkeleton.Position(i), Arm.IKSkeleton.Position(i + 1), Color.White);
                            //DrawLine(spriteBatch, Arm.IKSkeleton.JointPositions[i],, Color.White, 12);
                        }
                }
            }



            // Main.EntitySpriteDraw(Eyetex, Projectile.Center- new Vector2(0,-10)+ EyeDir- Main.screenPosition, null, Color.White, 0, Eyetex.Size() / 2f, 1, 0);
            return false;
        }


        private void DrawWings(ref Color lightColor)
        {
            Texture2D WingTex = ModContent.Request<Texture2D>(this.GetPath() + "_Wings").Value;

            var FanTex = ModContent.Request<Texture2D>(this.GetPath() + "_Fan").Value;

            for (int i = 0; i < _wings.Length; i++)
            {
                WingState wing = _wings[i];
                bool isRightWing = i == 0;
                float sideSign = isRightWing ? 1f : -1f;

                SpriteEffects Flip = isRightWing ? SpriteEffects.None : SpriteEffects.FlipVertically;
                Vector2 thrusterEnd = Projectile.Center - wing.WingRotation.ToRotationVector2().RotatedBy(MathHelper.Pi * sideSign) * 22 + wing.ThrusterRotation.ToRotationVector2() * 4;

                Rectangle Frame = FanTex.Frame(1, 8, 0, (int)(Main.GameUpdateCount % 8));
                SpriteEffects FanFlip = isRightWing ? SpriteEffects.None : SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
                Main.EntitySpriteDraw(FanTex, thrusterEnd - Main.screenPosition, Frame, lightColor, wing.WingRotation - Projectile.rotation * (sideSign), Frame.Size() / 2f, 1, FanFlip);

                Main.EntitySpriteDraw(WingTex, Projectile.Center - Main.screenPosition, null, lightColor, wing.WingRotation + Projectile.rotation, new Vector2(-4, WingTex.Height / 2f), 2, Flip);




                float forceStrength = MathHelper.Clamp(_visualForceRequest.Length(), 0.2f, 1.5f);
                Vector2 exhaustEnd = thrusterEnd + wing.ThrusterRotation.ToRotationVector2() * (ExhaustVisualLength * forceStrength);
                //DrawLine(spriteBatch, thrusterEnd, exhaustEnd, Color.OrangeRed, 2f);
            }
        }



        private static float ClampAngleAround(float angle, float center, float maxOffset)
        {
            float offset = MathHelper.WrapAngle(angle - center);
            offset = MathHelper.Clamp(offset, -maxOffset, maxOffset);
            return center + offset;
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Utils.DrawLine(spriteBatch, start, end, color, color, width);
        }

        private static void DrawCrosshair(SpriteBatch spriteBatch, Vector2 center, float size, Color color)
        {
            DrawLine(spriteBatch, center + new Vector2(-size, 0f), center + new Vector2(size, 0f), color, 1f);
            DrawLine(spriteBatch, center + new Vector2(0f, -size), center + new Vector2(0f, size), color, 1f);
        }
    }
}

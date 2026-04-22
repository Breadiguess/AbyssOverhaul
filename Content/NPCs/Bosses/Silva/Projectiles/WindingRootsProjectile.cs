using AbyssOverhaul.Core.Graphics;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva.Projectiles
{
    internal class WindingRootsProjectile : ModProjectile
    {
        public override string Texture => Assets.Textures.Extra.Star.KEY;

        public const int MaxTrailLength = 120;

        private const int STATE_MOVING = 0;
        private const int STATE_STOPPING = 1;

        // Configurable values
        private const float MoveSpeed = 16f;
        private const float MaxTravelDistance = 480f;
        private const float WeaveAmplitude = 24f;
        private const float WeaveFrequency = 0.22f;
        private const float SlowdownFactor = 0.88f;
        private const float KillSpeedThreshold = 0.4f;

        public ref float State => ref Projectile.ai[0];
        public ref float DistanceTraveled => ref Projectile.ai[1];
        public ref float Time => ref Projectile.localAI[0];
        public ref float PreviousOffset => ref Projectile.localAI[1];
        public bool Dying = false;

        public Vector2[] _CachedPositions;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CanDistortWater[Type] = false;
            ProjectileID.Sets.TrailingMode[Type] = 0;
            ProjectileID.Sets.TrailCacheLength[Type] = MaxTrailLength;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
        }
        public override void SetDefaults()
        {
            Projectile.Size = new Vector2(50, 50);
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 1200;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
        }


        public override bool PreAI()
        {
            if (Projectile.oldPos[^1].Equals(Vector2.Zero))
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Projectile.oldPos[i] = Projectile.Center;
                }
            }



            return base.PreAI();
        }

        public override void AI()
        {
            Time++;

            if (State == STATE_MOVING)
                DoWeavingMovement();

            if (Projectile.velocity.LengthSquared() > 0.001f)
                Projectile.rotation = Projectile.velocity.ToRotation();


        }
        private void DoWeavingMovement()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = forward.RotatedBy(MathHelper.PiOver2);

            _CachedPositions = (Vector2[])Projectile.oldPos.Clone();
            // Record how far we've moved forward.
            DistanceTraveled += Projectile.velocity.Length();

            // Sinusoidal sideways motion
            float currentOffset = (float)System.Math.Sin(Time * WeaveFrequency) * 4*WeaveAmplitude;

            // Move only by the difference from last frame so the wave is stable.
            float offsetDelta = currentOffset - PreviousOffset;
            Projectile.position += perpendicular * offsetDelta;
            PreviousOffset = currentOffset;

            if (DistanceTraveled >= MaxTravelDistance + 900)
            {
                Time = 0;
                State = STATE_STOPPING;
                Projectile.velocity = Vector2.Zero;
                PreviousOffset = 0f;
                Dying = true;
            }
        }

            
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < MaxTrailLength; i++)
            {
                if (targetHitbox.IntersectsConeFastInaccurate(_CachedPositions[i] + Projectile.Size / 2, projHitbox.Height, 0, MathHelper.TwoPi))
                {

                    return true;
                }
            }


            return base.Colliding(projHitbox, targetHitbox);
        }


        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }
        private BasicEffect effect;
        short[] Indicies;
        VertexPositionColorTexture[] verticies;
        public override bool PreDraw(ref Color lightColor)
        {
            if (effect == null)
            {
                if (!Main.dedServ)
                {
                    var gd = Main.graphics.graphicsDevice;
                    effect ??= new BasicEffect(gd)
                    {
                        World = Matrix.Identity,
                        View = Main.GameViewMatrix.ZoomMatrix,
                        Projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1)
                    };
                }
            }
            if (effect is not null)
                if (Projectile.oldPos is not null)
                {
                    effect.World = Matrix.Identity;
                    effect.View = Main.GameViewMatrix.ZoomMatrix;
                    effect.Projection = Matrix.CreateOrthographicOffCenter( 0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1);
                    effect.TextureEnabled = true;
                    effect.Texture = Assets.Textures.Extra.TreeBark.Asset.Value;


                    Color color = Color.Lerp(Color.Brown, Color.Green, Dying ? Utilities.InverseLerp(0, 60, Time) : 0);
                    float interp = !Dying ? 1 : 1-Utilities.InverseLerp(0, 120  , Time);
                    EasyPrimRope.DrawSimpleChainPrimitive(effect, ref Indicies, ref verticies, EasyPrimRope.SubdividePointsCatmullRom(_CachedPositions, 4), 50, color, SamplerState.PointWrap,uOffset:Main.GameUpdateCount*0.5f*interp , useLighting: true, textureRepeatLength:effect.Texture.Width);
                }

            for(int i = 0; i< Projectile.oldPos.Length - 1; i++)
            {
                Utils.DrawBorderString(Main.spriteBatch, i.ToString(), Projectile.oldPos[i] - Main.screenPosition, Color.Red, 1);
            }


            return false;
        }
    }
}

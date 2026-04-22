using System.Collections.Generic;

namespace AbyssOverhaul.Content.NPCs.Bosses.Silva.Projectiles;


public class BrutalVine : ModProjectile
{
    internal record AppendangeData(Asset<Texture2D> Asset, Vector2 Origin);

    private static Asset<Texture2D> normalMapTexture;

    private static InstancedRequestableTarget target;

    private readonly List<Vector3> oldPositions = new(Lifetime);


    public override string Texture => Assets.Textures.Glow_2.KEY;
    /// <summary>
    ///     The owner of this vine.
    /// </summary>
    public ref Player Owner => ref Main.player[Projectile.owner];

    /// <summary>
    ///     The current Z position of this vine.
    /// </summary>
    public ref float Z => ref Projectile.ai[0];

    /// <summary>
    ///     How long this vine has existed for.
    /// </summary>
    public ref float Time => ref Projectile.ai[1];

    /// <summary>
    ///     The twist offset angle of this vine.
    /// </summary>
    public ref float VineTwistAngle => ref Projectile.localAI[0];

    /// <summary>
    ///     How long this vine should exist for.
    /// </summary>
    public static int Lifetime => 210;

    public override void SetStaticDefaults()
    {
        //normalMapTexture = ModContent.Request<Texture2D>($"{Texture}NormalMap");

        
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = Lifetime;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;

        target = new InstancedRequestableTarget();
        Main.ContentThatNeedsRenderTargets.Add(target);
        On_Main.DrawPlayers_AfterProjectiles += DrawVinesSeparately;
    }

    public override void SetDefaults()
    {
        var vineSize = Main.rand?.NextFloat().Cubed() ?? 0f;
        Projectile.width = (int)MathHelper.Lerp(16f, 54f, vineSize);
        Projectile.height = Projectile.width;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.MaxUpdates = 3;
        Projectile.hide = true;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 1;
        Projectile.DamageType = DamageClass.Magic;
        ProjectileID.Sets.TrailCacheLength[Type] = Lifetime;
    }

    public override void AI()
    {
       

        // Grow!
        Projectile.scale = MathF.Pow(Utilities.InverseLerpBump(0f, 15f, Lifetime - 54f, Lifetime, Time), 0.75f);

        

        SwirlAround();

        Z = (1f - Utilities.Cos01(MathHelper.TwoPi * Time / 60f)) * 100f + 600f;

        // Twist around while appearing.
        VineTwistAngle += Utilities.InverseLerp(56f, 16f, Time) * 0.075f + 0.002f;

        oldPositions.Add(new Vector3(Projectile.Center, Z));
        Time++;
    }

    /// <summary>
    ///     Makes this vine twist around at its front, giving winding shapes as it travels.
    /// </summary>
    private void SwirlAround()
    {
        var swirlTime = MathHelper.TwoPi * Time / 35f + Projectile.identity * 1.1f;
        var swirlAngle = MathF.Cos(swirlTime) * 0.9f;
        var swirl = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(swirlAngle);
        Projectile.Center += swirl * Projectile.scale * 15f;
    }


  

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (var position in oldPositions)
        {
            if (Utils.CenteredRectangle(new Vector2(position.X, position.Y), Projectile.Size * Projectile.scale).Intersects(targetHitbox))
            {
                return true;
            }
        }

        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        overPlayers.Add(index);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }

    private float CalculateScaleAtVineInterpolant(float vineInterpolant)
    {
        return MathHelper.SmoothStep(0f, 1f, Utilities.InverseLerp(-0.5f, 0f, vineInterpolant - (1f - Projectile.scale)));
    }

    // https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
    private static Vector3 RodriguesRotation(Vector3 v, Vector3 axis, float angle)
    {
        var cosine = MathF.Cos(angle);
        var sine = MathF.Sin(angle);

        return v * cosine + Vector3.Cross(v, axis) * sine + axis * Vector3.Dot(axis, v) * (1 - cosine);
    }

    private void RenderVine()
    {
        var cylinderWidth = 8;
        var cylinderHeight = oldPositions.Count - 1;
        var unwrapInterpolant = oldPositions.Count / (float)Lifetime;

        if (cylinderHeight <= 0)
        {
            return;
        }

        var vertices = new VertexPositionColorNormalTexture[(cylinderWidth + 1) * (cylinderHeight + 1)];
        var indices = new short[cylinderWidth * cylinderHeight * 6];

        for (var i = 0; i <= cylinderWidth; i++)
        {
            for (var j = 0; j < cylinderHeight; j++)
            {
                var vineInterpolant = j / (cylinderHeight - 1f);

                var frontInterpolant = MathF.Pow(Utilities.InverseLerp(0f, 0.06f / unwrapInterpolant, vineInterpolant), 0.7f);
                var tipInterpolant = Utilities.InverseLerp(0.75f, 1f, vineInterpolant) + 0.0001f;
                var width = frontInterpolant * MathHelper.SmoothStep(1f, 0f, tipInterpolant) * Projectile.width * CalculateScaleAtVineInterpolant(vineInterpolant) * 0.5f;

                // MATH!
                var angle = MathHelper.TwoPi * i / cylinderWidth - VineTwistAngle;
                var start = oldPositions[j];
                var end = oldPositions[j + 1];
                var direction = Vector3.Normalize(end - start);
                var normal = RodriguesRotation(Vector3.UnitZ, direction, angle);
                var position = start + normal * width;
                var uv = new Vector2(i / (float)cylinderWidth, vineInterpolant * unwrapInterpolant);

                vertices[i + (cylinderWidth + 1) * j] = new VertexPositionColorNormalTexture(position, new Color(255, 255, 255), uv, normal);
            }
        }

        var index = 0;

        for (short y = 0; y < cylinderHeight - 1; y++)
        {
            for (short x = 0; x < cylinderWidth; x++)
            {
                var topLeft = (short)(y * (cylinderWidth + 1) + x);
                var topRight = (short)(topLeft + 1);
                var bottomLeft = (short)((y + 1) * (cylinderWidth + 1) + x);
                var bottomRight = (short)(bottomLeft + 1);

                indices[index++] = topLeft;
                indices[index++] = bottomRight;
                indices[index++] = bottomLeft;

                indices[index++] = topLeft;
                indices[index++] = topRight;
                indices[index++] = bottomRight;
            }
        }
        /*
        var cameraPosition = new Vector3(Main.screenPosition + WotGUtils.ViewportSize * 0.5f, 0f);
        var view = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f) * Main.GameViewMatrix.TransformationMatrix;
        var projection = Matrix.CreateOrthographicOffCenter(0f, WotGUtils.ViewportSize.X, WotGUtils.ViewportSize.Y, 0f, -2000f, 2000f);
        var matrix = view * projection;
        var lightPosition = new Vector3(SunMoonPositionRecorder.SunPosition / Main.ScreenSize.ToVector2(), -0.51f);

        var vineShader = ShaderManager.GetShader("HeavenlyArsenal.BrutalForgivenessVineShader");
        vineShader.TrySetParameter("uWorldViewProjection", matrix);
        vineShader.TrySetParameter("screenSize", WotGUtils.ViewportSize);
        vineShader.TrySetParameter("gameZoom", Main.GameViewMatrix.Zoom);
        vineShader.TrySetParameter("textureLookupZoom", new Vector2(0.3f, 6f));
        vineShader.TrySetParameter("diffuseLightExponent", 2.85f);
        vineShader.TrySetParameter("ambientLight", Vector3.One);
        vineShader.TrySetParameter("lightPosition", lightPosition);
        vineShader.SetTexture(TextureAssets.Projectile[Type].Value, 1, SamplerState.LinearWrap);
        vineShader.SetTexture(normalMapTexture.Value, 2, SamplerState.LinearWrap);
        vineShader.SetTexture(LightingMaskTargetManager.LightTarget, 3);
        vineShader.Apply();

        Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0, indices.Length / 3);
        */
    }

   

    private void DrawVinesSeparately(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);

        // Not doing this results in frustrating layering artifacts on the vines, with back vertices rendering over front vertices.
        //if (Utilities.AnyProjectiles(Type))
        {
            target.Request
            (
                Main.screenWidth,
                Main.screenHeight,
                0,
                () =>
                {
                    Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

                    foreach (var vine in Main.ActiveProjectiles)
                    {
                        if (vine.type == Type)
                        {
                            vine.As<BrutalVine>().RenderVine();
                        }
                    }
                }
            );

            if (target.TryGetTarget(0, out var rt) && rt is not null)
            {
                Main.spriteBatch.Begin();
                Main.spriteBatch.Draw(rt, Main.screenLastPosition - Main.screenPosition, Color.White);
                Main.spriteBatch.End();
            }
        }
    }
}
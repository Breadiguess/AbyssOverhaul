namespace AbyssOverhaul.Core.Graphics
{
    internal static class EasyPrimRope
    {

        public static void DrawSimpleChainPrimitive(
            BasicEffect effect,
            ref short[] chainIndices,
            ref VertexPositionColorTexture[] chainVertices,
            Vector2[] points,
            float width,
            Color color,
            SamplerState samplerState,
            float textureRepeatLength = 16f,
            float uOffset = 0f,
            bool useLighting = true)
        {
            if (points is null || points.Length < 2 || Main.dedServ)
                return;

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
            float[] accumulatedLengths = new float[pointCount];
            accumulatedLengths[0] = 0f;

            for (int i = 0; i < pointCount - 1; i++)
            {
                Vector2 diff = points[i + 1] - points[i];
                float segmentLength = diff.Length();

                if (segmentLength < 0.0001f)
                {
                    diff = Vector2.UnitX;
                    segmentLength = 0f;
                }
                else
                    diff /= segmentLength;

                segmentNormals[i] = diff.RotatedBy(MathHelper.PiOver2);
                accumulatedLengths[i + 1] = accumulatedLengths[i] + segmentLength;
            }

            textureRepeatLength = Math.Max(textureRepeatLength, 0.001f);

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 normal;

                if (i == 0)
                    normal = segmentNormals[0];
                else if (i == pointCount - 1)
                    normal = segmentNormals[pointCount - 2];
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
                        miterLength = MathHelper.Clamp(miterLength, -halfWidth * 2f, halfWidth * 2f);

                        normal = miter * (miterLength / halfWidth);
                    }
                }

                Vector2 offset = normal * halfWidth;
                Vector2 left = points[i] - offset;
                Vector2 right = points[i] + offset;

                // This is the key change:
                // UV.x now advances by actual rope length, so the texture tiles.
                float u = accumulatedLengths[i] / textureRepeatLength + uOffset;

                Color actualColor = color;
                if (useLighting)
                    actualColor = color.MultiplyRGB(Lighting.GetColor(points[i].ToTileCoordinates()));

                chainVertices[i * 2] = new VertexPositionColorTexture(
                    new Vector3(left - Main.screenPosition, 0f),
                    actualColor,
                    new Vector2(u, 0f)
                );

                chainVertices[i * 2 + 1] = new VertexPositionColorTexture(
                    new Vector3(right - Main.screenPosition, 0f),
                    actualColor,
                    new Vector2(u, 1f)
                );
            }

            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.Solid};
            gd.SamplerStates[0] = samplerState;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
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

        public static readonly SamplerState LinearWrapSampler = new SamplerState
        {
            Filter = TextureFilter.Linear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Clamp
        };


        /// <summary>
        /// this is so stupid bruh.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="subdivisionsPerSegment"></param>
        /// <returns></returns>
        public static Vector2[] SubdividePointsLinear(Vector2[] points, int subdivisionsPerSegment)
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
        public static Vector2[] SubdividePointsCatmullRom(Vector2[] points, int subdivisionsPerSegment)
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
    }
}

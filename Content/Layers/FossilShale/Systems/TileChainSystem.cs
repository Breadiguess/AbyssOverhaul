using AbyssOverhaul.Core.Graphics;
using BreadLibrary.Core.Verlet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Content.Layers.FossilShale.Systems
{
    public sealed class TileToTileChainSystem : ModSystem
    {
        public static readonly List<AnchoredTileChain> Chains = new();
        private static int _nextId;

        public override void OnWorldLoad()
        {
            Chains.Clear();
            _nextId = 0;
        }

        public override void OnWorldUnload()
        {
            Chains.Clear();
            _nextId = 0;
        }

        public static AnchoredTileChain AddChain(
            Point16 startTile,
            Point16 endTile,
            float pixelsPerSegment = 10f,
            float gravity = 0.3f,
            float damping = 0.99f,
            int simulateIterations = 4,
            int anchorIterations = 4,
            bool collideWithTiles = true,
            float collisionRadius = 3f,
            Vector2? startOffset = null,
            Vector2? endOffset = null,
            float thickness = 2f)
        {
            Vector2 startWorld = TileAnchorWorld(startTile, startOffset ?? Vector2.Zero);
            Vector2 endWorld = TileAnchorWorld(endTile, endOffset ?? Vector2.Zero);

            float distance = Vector2.Distance(startWorld, endWorld);
            int pointCount = Math.Max(2, (int)MathF.Ceiling(distance / pixelsPerSegment) + 1);

            return AddChainByPointCount(
                startTile,
                endTile,
                pointCount,
                gravity,
                damping,
                simulateIterations,
                anchorIterations,
                collideWithTiles,
                collisionRadius,
                startOffset ?? Vector2.Zero,
                endOffset ?? Vector2.Zero,
                thickness);
        }

        public static AnchoredTileChain AddChainByPointCount(
            Point16 startTile,
            Point16 endTile,
            int pointCount,
            float gravity,
            float damping,
            int simulateIterations,
            int anchorIterations,
            bool collideWithTiles,
            float collisionRadius,
            Vector2 startOffset,
            Vector2 endOffset,
            float thickness)
        {
            Vector2 startWorld = TileAnchorWorld(startTile, startOffset);
            Vector2 endWorld = TileAnchorWorld(endTile, endOffset);

            pointCount = Math.Max(2, pointCount);

            float distance = Vector2.Distance(startWorld, endWorld);
            float segmentLength = distance / (pointCount - 1);

            VerletChain chain = new VerletChain(pointCount, segmentLength, startWorld);

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector2 pos = Vector2.Lerp(startWorld, endWorld, t);
                chain.Positions[i] = pos;
                chain.OldPositions[i] = pos;
            }

            var wrapped = new AnchoredTileChain
            {
                Id = _nextId++,
                Chain = chain,
                StartTile = startTile,
                EndTile = endTile,
                StartOffset = startOffset,
                EndOffset = endOffset,
                Gravity = gravity,
                Damping = damping,
                SimulateIterations = simulateIterations,
                AnchorIterations = anchorIterations,
                CollideWithTiles = collideWithTiles,
                CollisionRadius = collisionRadius,
                Thickness = thickness
            };

            Chains.Add(wrapped);
            return wrapped;
        }

        public static void RemoveChain(int id)
        {
            for (int i = Chains.Count - 1; i >= 0; i--)
            {
                if (Chains[i].Id == id)
                {
                    Chains.RemoveAt(i);
                    return;
                }
            }
        }

        public static void RemoveChainsBetween(Point16 a, Point16 b)
        {
            for (int i = Chains.Count - 1; i >= 0; i--)
            {
                bool sameForward = Chains[i].StartTile == a && Chains[i].EndTile == b;
                bool sameBackward = Chains[i].StartTile == b && Chains[i].EndTile == a;
                if (sameForward || sameBackward)
                    Chains.RemoveAt(i);
            }
        }

        public override void PostUpdateEverything()
        {
            for (int i = Chains.Count - 1; i >= 0; i--)
            {
                AnchoredTileChain chain = Chains[i];

                if (!chain.AnchorsAreValid())
                {
                    Chains.RemoveAt(i);
                    continue;
                }

                chain.Update();
            }
        }

        public override void PostDrawTiles()
        {
            SpriteBatch spriteBatch = Main.spriteBatch;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < Chains.Count; i++)
            {
                if (Chains[i].IsOnScreen())
                    Chains[i].Draw(spriteBatch);
            }
            Main.spriteBatch.End();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            List<TagCompound> chainTags = new();

            foreach (AnchoredTileChain chain in Chains)
                chainTags.Add(chain.Save());

            tag["TileChains"] = chainTags;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Chains.Clear();
            _nextId = 0;

            if (!tag.ContainsKey("TileChains"))
                return;

            List<TagCompound> chainTags = (List<TagCompound>)tag.GetList<TagCompound>("TileChains");
            foreach (TagCompound chainTag in chainTags)
            {
                AnchoredTileChain.Load(chainTag);
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Chains.Count);

            foreach (AnchoredTileChain chain in Chains)
                chain.NetSend(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            Chains.Clear();
            _nextId = 0;

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                AnchoredTileChain.NetReceive(reader);
        }

        public static Vector2 TileAnchorWorld(Point16 tilePos, Vector2 localOffset)
        {
            return new Vector2(tilePos.X * 16f + 8f, tilePos.Y * 16f + 8f) + localOffset;
        }

        public static bool IsValidAnchorTile(Point16 tilePos)
        {
            if (!Terraria.WorldGen.InWorld(tilePos.X, tilePos.Y, 1))
                return false;

            Tile tile = Framing.GetTileSafely(tilePos.X, tilePos.Y);
            return tile.HasTile;
        }
    }

    public sealed class AnchoredTileChain
    {
        public int Id;

        public VerletChain Chain;

        public const float DefaultGravity = 0.08f;
        public const float DefaultDamping = 0.99f;
        public const int DefaultSimulateIterations = 5;
        public const int DefaultAnchorIterations = 5;
        public const bool DefaultCollideWithTiles = true;
        public const float DefaultCollisionRadius = 3f;

        public Point16 StartTile;
        public Point16 EndTile;

        public Vector2 StartOffset;
        public Vector2 EndOffset;

        public float Gravity;
        public float Damping;
        public int SimulateIterations;
        public int AnchorIterations;
        public bool CollideWithTiles;
        public float CollisionRadius;
        public float Thickness;

        public Color ChainColor = new Color(255, 180, 180, 255);
        public Color ShadowColor = new Color(0, 0, 0, 90);

        public Vector2 StartWorld => TileToTileChainSystem.TileAnchorWorld(StartTile, StartOffset);
        public Vector2 EndWorld => TileToTileChainSystem.TileAnchorWorld(EndTile, EndOffset);

        public bool AnchorsAreValid()
        {
            return TileToTileChainSystem.IsValidAnchorTile(StartTile) &&
                   TileToTileChainSystem.IsValidAnchorTile(EndTile);
        }

        public void Update()
        {
            Vector2 start = StartWorld;
            Vector2 end = EndWorld;

            Chain.Simulate(
                externalVelocity: Vector2.Zero,
                root: start,
                gravity: Gravity,
                damping: Damping,
                constraintIterations: SimulateIterations,
                collideWithTiles: CollideWithTiles,
                collisionRadius: CollisionRadius);

            PinBothEndsAndResolve(start, end, AnchorIterations);
        }

        private void PinBothEndsAndResolve(Vector2 start, Vector2 end, int iterations)
        {
            int last = Chain.Positions.Length - 1;

            Chain.Positions[0] = start;
            Chain.OldPositions[0] = start;

            Chain.Positions[last] = end;
            Chain.OldPositions[last] = end;

            for (int k = 0; k < iterations; k++)
            {
                Chain.Positions[0] = start;
                Chain.Positions[last] = end;

                for (int i = 0; i < last; i++)
                {
                    Vector2 a = Chain.Positions[i];
                    Vector2 b = Chain.Positions[i + 1];

                    Vector2 delta = b - a;
                    float dist = delta.Length();
                    if (dist <= 0.0001f)
                        continue;

                    float targetLength = Chain.SegmentLength[i];
                    float error = dist - targetLength;
                    Vector2 dir = delta / dist;

                    bool aPinned = i == 0;
                    bool bPinned = i + 1 == last;

                    if (aPinned && bPinned)
                        continue;

                    if (aPinned)
                    {
                        Chain.Positions[i + 1] -= dir * error;
                    }
                    else if (bPinned)
                    {
                        Chain.Positions[i] += dir * error;
                    }
                    else
                    {
                        Vector2 correction = dir * error * 0.5f;
                        Chain.Positions[i] += correction;
                        Chain.Positions[i + 1] -= correction;
                    }
                }

                Chain.Positions[0] = start;
                Chain.Positions[last] = end;
            }
        }

        public bool IsOnScreen()
        {
            Rectangle screen = new Rectangle(
                (int)Main.screenPosition.X - 64,
                (int)Main.screenPosition.Y - 64,
                Main.screenWidth + 128,
                Main.screenHeight + 128);

            for (int i = 0; i < Chain.Positions.Length; i++)
            {
                if (screen.Contains(Chain.Positions[i].ToPoint()))
                    return true;
            }

            return false;
        }

        private BasicEffect effect;
        private short[] Indicies;
        private VertexPositionColorTexture[] verticies;
        public void Draw(SpriteBatch spriteBatch)
        {

            if (Main.dedServ)
                return;
            var gd = Main.graphics.graphicsDevice;
            effect ??= new BasicEffect(gd)
            {
                TextureEnabled = true,
                
            };
            effect.Texture = TextureAssets.MagicPixel.Value;
            effect.world = Matrix.Identity;
            effect.view = Main.GameViewMatrix.TransformationMatrix;
            effect.Projection = Matrix.CreateOrthographicOffCenter(
                0f,
                Main.screenWidth,
                Main.screenHeight,
                0f,
                -1f, 1);


            EasyPrimRope.DrawSimpleChainPrimitive(effect, ref Indicies, ref verticies, Chain.Positions, 12, ChainColor, SamplerState.PointWrap, textureRepeatLength: effect.Texture.Width, useLighting: true);

            for (int i = 0; i < Chain.Positions.Length - 1; i++)
            {
                Vector2 a = Chain.Positions[i];
                Vector2 b = Chain.Positions[i + 1];

                //DrawSegment(spriteBatch, a + Vector2.UnitY, b + Vector2.UnitY, ShadowColor, Thickness + 1f);
                //DrawSegment(spriteBatch, a, b, ChainColor, Thickness*12);
            }
        }

        private static void DrawSegment(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Texture2D tex = TextureAssets.MagicPixel.Value;

            Vector2 edge = end - start;
            float length = edge.Length();
            if (length <= 0.001f)
                return;

            var b = Lighting.GetSubLight((start + end) / 2);
            Color a = new(b.X, b.Y, b.Z);
            color = color.MultiplyRGB(a);

            Utilities.DrawLineBetter(spriteBatch, start, end, color, thickness);
        }

        public TagCompound Save()
        {
            return new TagCompound
            {
                ["StartX"] = (int)StartTile.X,
                ["StartY"] = (int)StartTile.Y,
                ["EndX"] = (int)EndTile.X,
                ["EndY"] = (int)EndTile.Y,
                ["PointCount"] = Chain.Positions.Length,
                ["StartOffsetX"] = StartOffset.X,
                ["StartOffsetY"] = StartOffset.Y,
                ["EndOffsetX"] = EndOffset.X,
                ["EndOffsetY"] = EndOffset.Y,
                ["Thickness"] = Thickness
            };
        }

        public static void Load(TagCompound tag)
        {
            TileToTileChainSystem.AddChainByPointCount(
                new Point16(tag.GetInt("StartX"), tag.GetInt("StartY")),
                new Point16(tag.GetInt("EndX"), tag.GetInt("EndY")),
                tag.GetInt("PointCount"),
                DefaultGravity,
                DefaultDamping,
                DefaultSimulateIterations,
                DefaultAnchorIterations,
                DefaultCollideWithTiles,
                DefaultCollisionRadius,
                new Vector2(tag.GetFloat("StartOffsetX"), tag.GetFloat("StartOffsetY")),
                new Vector2(tag.GetFloat("EndOffsetX"), tag.GetFloat("EndOffsetY")),
                tag.GetFloat("Thickness"));
        }

        public void NetSend(BinaryWriter writer)
        {
            writer.Write((short)StartTile.X);
            writer.Write((short)StartTile.Y);
            writer.Write((short)EndTile.X);
            writer.Write((short)EndTile.Y);
            writer.Write(Chain.Positions.Length);

            writer.Write(StartOffset.X);
            writer.Write(StartOffset.Y);
            writer.Write(EndOffset.X);
            writer.Write(EndOffset.Y);

            writer.Write(Gravity);
            writer.Write(Damping);
            writer.Write(SimulateIterations);
            writer.Write(AnchorIterations);
            writer.Write(CollideWithTiles);
            writer.Write(CollisionRadius);
            writer.Write(Thickness);
        }

        public static void NetReceive(BinaryReader reader)
        {
            TileToTileChainSystem.AddChainByPointCount(
                new Point16(reader.ReadInt16(), reader.ReadInt16()),
                new Point16(reader.ReadInt16(), reader.ReadInt16()),
                reader.ReadInt32(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadSingle(),
                new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                reader.ReadSingle());
        }
    }
}

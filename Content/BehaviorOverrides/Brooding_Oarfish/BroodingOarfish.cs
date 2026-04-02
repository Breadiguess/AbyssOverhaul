using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Core.DataStructures;
using AbyssOverhaul.Core.Graphics;
using BreadLibrary.Core;
using BreadLibrary.Core.Verlet;
using CalamityMod.Tiles.Abyss;
using Terraria.GameContent;
using Terraria.Localization;

namespace AbyssOverhaul.Content.BehaviorOverrides.Brooding_Oarfish
{
#pragma warning disable CS8618 
    public class BroodingOarfish : NPCBehaviorOverride, IMultiSegmentNPC, IEcologyParticipant
    {
        public void SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.None);
            definition.FoodConsumer = FoodConsumerType.Herbivore;
            definition.BaseAggression = -2f;
        }

        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {

        }
        public override int NPCType => ModContent.NPCType<OarfishHead>();


        public EvenlySpacedTrail BodyTrail;

        public ModularNpcBrain<CreatureNpcContext> NpcBrain;

        private List<ExtraNPCSegment> _ExtraHitBoxes;
        public List<VerletChain> MouthThings;

        public static Asset<Texture2D> BodyTex;
        public static Asset<Texture2D> TailTex;

        public override void Load()
        {
            string Path = this.GetPath();
            BodyTex = ModContent.Request<Texture2D>($"{Path}_Body");
            TailTex = ModContent.Request<Texture2D>($"{Path}_Tail");
        }
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            typeName = Language.GetOrRegister($"Mods.AbyssOverhaul.NPCOverrides.BroodingOarfish").Value;
        }
        private void Initialize(NPC NPC)
        {
            BodyTrail = new EvenlySpacedTrail(40, 10, 10);
            BodyTrail.Reset(NPC.Center);

            _ExtraHitBoxes = new List<ExtraNPCSegment>();
            int HitboxSize = 30;
            int HitboxStride = 2;
            for (int pointIndex = 1; pointIndex < BodyTrail.Points.Length; pointIndex += HitboxStride)
                _ExtraHitBoxes.Add(new ExtraNPCSegment(new Rectangle(0, 0, HitboxSize, HitboxSize)));

            MouthThings = new List<VerletChain>
            {
                new VerletChain(20, 2, NPC.Center),
                new VerletChain(20, 2, NPC.Center)
            };



            NpcBrain = new(new());

            NpcBrain.Modules.Add(new CreatureSwimWanderModule()
            {
                Score = 10
            });
            NpcBrain.Modules.Add(new AvoidTilesSwimModule()
            {
                ProbeDistance = 60
            });
            NpcBrain.Sensors.Add(new FindTileSensor(tile => tile.HasTile && tile.TileType == ModContent.TileType<PlantyMush>())
            {

            });
            NpcBrain.Sensors.Add(new CreatureVitalsSensor<CreatureNpcContext>());

        }
        public ref List<ExtraNPCSegment> ExtraHitBoxes()
        {
            return ref _ExtraHitBoxes;
        }
        public override void SetDefaults(NPC NPC)
        {

        }
        public override void OnSpawn(NPC NPC, IEntitySource source)
        {

        }

        public int Time(NPC npc)
        {

            return (int)npc.ai[0];
        }
        public override bool OverrideAI(NPC NPC)
        {
            if (BodyTrail is null || _ExtraHitBoxes is null)
            {
                Initialize(NPC);

            }

            NPC.noGravity = NPC.wet;
            NPC.GravityMultiplier *= 0;
            BodyTrail.Update(NPC.Center);





            NpcBrain.Update(NPC);
            //x1NPC.velocity = NPC.DirectionTo(Main.MouseWorld) * 3;
            NPC.rotation = NPC.velocity.ToRotation();

            UpdateVisuals(NPC);




            SyncExtraHitboxes();

            return true;
        }

        private void UpdateVisuals(NPC NPC)
        {

            for (int i = 0; i < MouthThings.Count; i++)
            {
                MouthThings[i].Simulate(Vector2.zeroVector, NPC.Center + new Vector2(i % 2 * 10, -10).RotatedBy(NPC.rotation), 1, 0.4f, collideWithTiles: false);
                Lighting.AddLight(MouthThings[i].Positions[^1], r: 1, 0.1f, 0.1f);
            }




        }
        private void SyncExtraHitboxes()
        {
            int hitboxIndex = 0;

            for (int pointIndex = 1; pointIndex < BodyTrail.Points.Length && hitboxIndex < _ExtraHitBoxes.Count; pointIndex += HitboxStride, hitboxIndex++)
            {
                _ExtraHitBoxes[hitboxIndex].Hitbox = new Rectangle(
                    (int)(BodyTrail.Points[pointIndex].X - HitboxSize * 0.5f),
                    (int)(BodyTrail.Points[pointIndex].Y - HitboxSize * 0.5f),
                    HitboxSize,
                    HitboxSize
                );
            }
        }

        #region DrawCode
        void DrawTendrils(NPC NPC, SpriteBatch spriteBatch)
        {
            if (MouthThings is null)
                return;

            for (int x = 0; x < MouthThings.Count; x++)
            {
                var thing = MouthThings[x];

                for (int i = 0; i < thing.Positions.Length - 1; i++)
                {

                    Vector2 start = thing.Positions[i];
                    Vector2 end = thing.Positions[i + 1];


                    Color t = Color.Azure;
                    Utilities.DrawLineBetter(spriteBatch, start, end, t, 3);
                }

            }
        }
        private void DrawDetectionCone(Vector2 DrawPos, float rotation)
        {
            Texture2D GlowCone = Assets.Textures.Glow_2.Asset.Value;
            Vector2 origin = new(0, GlowCone.Height / 2);

            Main.EntitySpriteDraw(GlowCone, DrawPos, null, Color.Red with { A = 0 }, rotation, origin, new Vector2(1f, 0.2f), 0);

        }



        void DrawDebuglinesToTiles(NPC NPC, SpriteBatch spriteBatch)
        {
            Utils.DrawLine(spriteBatch, NPC.Center, NpcBrain.Context.FoundTileWorld, Color.White);
        }

        BasicEffect ropeEffect;
        VertexPositionColorTexture[] vertex;
        short[] Thing;
        private int HitboxStride = 2;
        private int HitboxSize = 30;

        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                return true;
            if (_ExtraHitBoxes is null || BodyTrail is null)
                return false;

            for (int i = 0; i < BodyTrail.Points.Length - 1; i++)
            {
                //Utilities.DrawLineBetter(spriteBatch, BodyTrail.Points[i], BodyTrail.Points[i + 1], drawColor, 4);
            }

            for (int i = 0; i < _ExtraHitBoxes.Count; i++)
            {
                //Utils.DrawRect(spriteBatch, _ExtraHitBoxes[i].Hitbox, Color.White);
            }


            var thing = MouthThings[0];

            for (int i = 0; i < thing.Positions.Length - 1; i++)
            {

                Vector2 start = thing.Positions[i];
                Vector2 end = thing.Positions[i + 1];


                Color t = Color.Azure;
                Utilities.DrawLineBetter(spriteBatch, start, end, t, 3);
            }

            EnsureChainEffect();
            ropeEffect.World = Matrix.Identity;
            ropeEffect.View = Main.GameViewMatrix.TransformationMatrix;
            ropeEffect.projection = Matrix.CreateOrthographicOffCenter(
                0f,
                Main.screenWidth,
                Main.screenHeight,
                0f,
                -1f, 1);

            Vector2[] Pos = (Vector2[])BodyTrail.Points.Clone();

            EasyPrimRope.DrawSimpleChainPrimitive(ropeEffect, ref Thing, ref vertex, EasyPrimRope.SubdividePointsCatmullRom(Pos, 12), ropeEffect.Texture.Height, drawColor, SamplerState.PointWrap, ropeEffect.Texture.Width, useLighting:true);

            var TailTex = ModContent.Request<Texture2D>(this.GetPath() + "_Tail").Value;
            float angle = Pos[Pos.Length - 1].AngleFrom(Pos[Pos.Length - 2]) - MathHelper.PiOver2;
            Main.EntitySpriteDraw(TailTex, BodyTrail.Points[^1] - screenPos + new Vector2(-5, TailTex.Height / 2).RotatedBy(angle), null, drawColor, angle, TailTex.Size() / 2f, 1, 0);




            var tex = TextureAssets.Npc[this.NPCType].Value;
            Main.EntitySpriteDraw(tex, NPC.Center - screenPos - new Vector2(0, 4).RotatedBy(NPC.rotation), null, drawColor, NPC.rotation + MathHelper.PiOver2, tex.Size() / 2f, 1, SpriteEffects.None);

            var thing2 = MouthThings[1];

            for (int i = 0; i < thing.Positions.Length - 1; i++)
            {

                Vector2 start = thing.Positions[i];
                Vector2 end = thing.Positions[i + 1];


                Color t = Color.Azure;
                Utilities.DrawLineBetter(spriteBatch, start, end, t, 3);
            }

            DrawDetectionCone(NPC.Center - screenPos + new Vector2(10, 0).RotatedBy(NPC.rotation), NPC.rotation);


            // DrawDebuglinesToTiles(NPC, spriteBatch);
            return false;
        }

        private void EnsureChainEffect()
        {
            if (Main.dedServ)
                return;

            if (ropeEffect is null || ropeEffect.IsDisposed)
            {
                ropeEffect = new BasicEffect(Main.instance.GraphicsDevice)
                {
                    VertexColorEnabled = true,
                    TextureEnabled = true

                };
                ropeEffect.Texture = ModContent.Request<Texture2D>(this.GetPath() + "_Body").Value;


            }
        }

     

        #endregion
    }
#pragma warning restore CS8618 


}

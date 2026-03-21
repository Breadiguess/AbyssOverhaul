using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using BreadLibrary.Core;
using BreadLibrary.Core.SoftBodySim;
using BreadLibrary.Core.Verlet;
using CalamityMod.Tiles.Abyss;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;

namespace AbyssOverhaul.Content.BehaviorOverrides.Brooding_Oarfish
{
#pragma warning disable CS8618 
    public class BroodingOarfish : NPCBehaviorOverride, IMultiSegmentNPC
    {
        public override int NPCType => ModContent.NPCType<OarfishHead>();


        public ModularNpcBrain<CreatureNpcContext> NpcBrain;
        public VerletChain Body;
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
            Body = new VerletChain(20, 30, NPC.Center);


            _ExtraHitBoxes = new List<ExtraNPCSegment>();
            for (int i = 0; i < Body.Positions.Length; i++)
                _ExtraHitBoxes.Add(new ExtraNPCSegment(new(0, 0, 30, 30)));

            for (int i = 0; i < Body.Positions.Length; i++)
            {
                Body.Positions[i] = NPC.Center;
                Body.OldPositions[i] = NPC.Center;
            }

            MouthThings = new List<VerletChain>();
            MouthThings.Add(new(10, 2, NPC.Center));

            MouthThings.Add(new(10, 2, NPC.Center));


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
            if (Body is null || _ExtraHitBoxes is null)
            {
                Initialize(NPC);
            }

            NPC.noGravity = NPC.wet;
            NPC.GravityMultiplier *= 0;

            Body.Simulate(Vector2.zeroVector, NPC.Center, NPC.gravity, 0.95f, collideWithTiles: false);

            for (int i = 0; i < _ExtraHitBoxes.Count; i++)
            {
                _ExtraHitBoxes[i].Hitbox.Location = (Body.Positions[i] - _ExtraHitBoxes[i].Hitbox.Size() / 2).ToPoint();
            }

            NpcBrain.Update(NPC);
            //NPC.velocity = NPC.DirectionTo(Main.MouseWorld) * 3;
            NPC.rotation = NPC.velocity.ToRotation();

            UpdateVisuals(NPC);



            NPC.ai[0]++;

            if (NPC.ai[0] % 60==0)
            {
                Vector2 SpawnPos =
                Body.Positions[Body.Positions.Length - 4];
                Projectile a = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), SpawnPos, Vector2.zeroVector, ModContent.ProjectileType<FishFeed>(), 1, 0);
                
            }


            return true;
        }

        private void UpdateVisuals(NPC NPC)
        {

            for (int i = 0; i < MouthThings.Count; i++)
            {
                MouthThings[i].Simulate(Vector2.zeroVector, NPC.Center, 0, 0.4f, collideWithTiles: false);
                Lighting.AddLight(MouthThings[i].Positions[^1], r:1, 0.1f, 0.1f);
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
                    Utilities.DrawLineBetter(spriteBatch, start, end, t, 12);
                }

            }
        }
        private void DrawDetectionCone(Vector2 DrawPos, float rotation)
        {
            Texture2D GlowCone = Assets.Textures.Glow_2.Asset.Value;
            Vector2 origin = new(0, GlowCone.Height / 2);

            Main.EntitySpriteDraw(GlowCone, DrawPos, null, Color.Red with { A = 0 }, rotation, origin, new Vector2(1f, 0.2f), 0);

        }

        void DrawEggSack()
        {
            if (Body is null)
                return;


            Texture2D tex = ModContent.Request<Texture2D>("AbyssOverhaul/Content/BehaviorOverrides/Brooding_Oarfish/BroodingOarfish_Sack").Value;
            Vector2 DrawPos =
                Body.Positions[Body.Positions.Length - 4] - Main.screenPosition;
            float rot =
                Body.Positions[Body.Positions.Length - 4].AngleTo(
                Body.Positions[Body.Positions.Length - 3]);
            Main.EntitySpriteDraw(tex, DrawPos, null, Color.White, rot, tex.Size() / 2, 1, 0);

        }

        void DrawDebuglinesToTiles(NPC NPC, SpriteBatch spriteBatch)
        {
            Utils.DrawLine(spriteBatch,NPC.Center, NpcBrain.Context.FoundTileWorld, Color.White);
        }

        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
                return true;
            if (_ExtraHitBoxes is null || Body is null)
                return false;



            for (int i = 0; i < _ExtraHitBoxes.Count; i++)
            {
                Utils.DrawRect(spriteBatch, _ExtraHitBoxes[i].Hitbox, Color.White);
            }

            for (int i = 0; i < Body.Positions.Length - 1; i++)
            {
                Vector2 start = Body.Positions[i];
                Vector2 end = Body.Positions[i + 1];

                Utilities.DrawLineBetter(spriteBatch, start, end, Color.White, 40);
            }
            for (int i = 0; i < Body.Positions.Length-1; i++)
            {
                Vector2 DrawPos = Body.Positions[i] - Main.screenPosition;

                var tex = i == 0 ? TextureAssets.Npc[this.NPCType].Value : BodyTex.Value;

                float rotation = i == 0 ? NPC.rotation : Body.Positions[i].AngleFrom(Body.Positions[i + 1]);

                Main.EntitySpriteDraw(tex, DrawPos, null, drawColor, rotation, tex.Size() / 2, NPC.scale, 0);
            }


                //NpcBrain.DrawContextDebug(spriteBatch, NPC.Center - screenPos);






                DrawTendrils(NPC, spriteBatch);

            DrawDetectionCone(NPC.Center - screenPos, NPC.rotation);
            DrawEggSack();

            DrawDebuglinesToTiles(NPC, spriteBatch);
            return false;
        }
        #endregion
    }
#pragma warning restore CS8618 


}

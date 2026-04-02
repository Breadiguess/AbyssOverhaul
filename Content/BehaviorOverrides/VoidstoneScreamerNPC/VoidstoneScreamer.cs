using AbyssOverhaul.Content.BehaviorOverrides.VoidstoneScreamerNPC;
using AbyssOverhaul.Core.Graphics.ReworkedAbyssDarkness;
using AbyssOverhaul.Core.Utilities;
using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Sounds;
using CalamityMod;


namespace AbyssOverhaul.Content.BehaviorOverrides.VoidstoneScreamerNPC
{
    //that stupid fucking fish that i hate
    public sealed class VoidstoneScreamer : NPCBehaviorOverride, IEcologyParticipant
    {
        public void SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.Prey);
            definition.BaseCuriosity = 4;
            definition.BaseFear = 0.8f;
            definition.BaseMaxHunger = 60;
            definition.FoodConsumer = FoodConsumerType.Scavenger;
            definition.Metabolism.ActivityCost = 0.2f;
        }

        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {

        }
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.LuminousCorvina>();


        public static readonly SoundStyle ScreamSound = new("CalamityMod/Sounds/Custom/CorvinaScream");


        public enum state
        {
            FlopAroundOnLand,
            SwimAroundWaterWithoutBumpingIntoWalls,
            SpotIntruder,
            Investigate,

            Scream,
            Fuck_off_Before_Predators_Rip_you_to_shreds
        }
        public state CurrentState
        {
            get;
            set;
            //    get=> (state)NPC.ai[0];
            //    set => NPC.ai[0] = (int)value;
        }
        public static Asset<Texture2D> GlowTex;
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            typeName = Language.GetOrRegister("Mods.AbyssOverhaul.NPCOverrides.VoidstoneScreamer").Value;
        }
        public override string TexturePath => this.GetPath();

        public LoopedSoundInstance? ScreamLoop;
        public override void SetDefaults(NPC npc)
        {
            npc.noTileCollide = false;

            npc.lifeMax = 12_000;

        }

        public override void Load()
        {
            string path = this.GetPath();
            path += "_Glow";
            GlowTex = ModContent.Request<Texture2D>(path);
            Main.npcFrameCount[NPCType] = 2;
        }

        public override void SpawnNPC(int npc, int tileX, int tileY)
        {
            base.SpawnNPC(npc, tileX, tileY);
        }
        public override void OnSpawn(NPC npc, IEntitySource source)
        {


        }

        public float Interpolant;
        private bool HasMadeLight;
        public const int MAX_SCREAM_TIME = 60 * 7;
        public int ScreamTimer;
        public bool HasAlertedNearbyHostileNPCS;

        public Entity entity;
        public override bool OverrideAI(NPC npc)
        {

            npc.noGravity = npc.wet;


            StateMachine(npc);
            UpdateSoundLoop(npc);

            return true;
        }



        void StateMachine(NPC npc)
        {
            switch (CurrentState)
            {
                case state.FlopAroundOnLand:
                    {
                        if (npc.wet)
                            CurrentState = state.SwimAroundWaterWithoutBumpingIntoWalls;
                    }
                    break;
                case state.SwimAroundWaterWithoutBumpingIntoWalls:
                    {
                        Vector2 ahead = npc.Center + npc.velocity * 40f;
                        bool aboutToLeaveWorld = ahead.X >= Main.maxTilesX * 16f - 700f || ahead.X < 700f;
                        bool shouldTurnAround = aboutToLeaveWorld;
                        ref float hasFoundPlayer = ref npc.ai[1];

                        for (float x = -0.47f; x < 0.47f; x += 0.06f)
                        {
                            Vector2 checkDirection = npc.velocity.SafeNormalize(Vector2.Zero).RotatedBy(x);
                            if (!Collision.CanHit(npc.Center, 1, 1, npc.Center + checkDirection * 125f, 1, 1) ||
                                !Collision.WetCollision(npc.Center + checkDirection * 50f, npc.width, npc.height))
                            {
                                shouldTurnAround = true;
                                break;
                            }
                        }

                        // Avoid walls and exiting water.
                        if (shouldTurnAround)
                            AbyssUtilities.TurnAroundBehavior(npc, ahead, shouldTurnAround);
                        

                        // Move in some random direction if stuck.
                        if (npc.velocity == Vector2.Zero)
                        {
                            npc.velocity = npc.velocity.MoveTowards(Main.rand.NextVector2CircularEdge(4f, 4f), 0.14f);
                            npc.netUpdate = true;
                        }

                        // Clamp velocities.
                        if (npc.velocity.Length() < 2f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 2f;
                        if (npc.velocity.Length() < 5.4f)
                            npc.velocity *= 1.024f;
                        if (npc.velocity.Length() > 10f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 10f;

                        npc.velocity = npc.velocity.MoveTowards(npc.velocity.RotatedBy(MathF.Sin(Main.GameUpdateCount * 0.04f)*0.01f),0.15f);
                        // Define rotation.
                        npc.rotation = npc.velocity.ToRotation();
                        if (npc.spriteDirection == -1)
                            npc.rotation += MathHelper.Pi;


                        
                    }
                    break;
                case state.SpotIntruder:
                    break;
                case state.Investigate:
                    break;
                case state.Scream:


                    {

                        Rectangle Frame = TextureAssets.Npc[this.NPCType].Value.Frame(1, Main.npcFrameCount[NPCType], 0, 1);
                        if (!HasMadeLight)
                        {
                            ReworkedAbyssLighting.AddLight(new()
                            {
                                texture = GlowTex.Value,
                                frame = Frame,
                                center = npc.Center,
                                Origin = Frame.Size() / 2,
                                lifetime = 60,
                                rotation = npc.rotation,

                            });
                            HasMadeLight = true;
                        }

                        npc.velocity = Vector2.Zero;
                        int index = ReworkedAbyssLighting.lights.FindIndex(a => a.texture == GlowTex.Value && a.rotation == npc.rotation);

                        if(index != -1)
                        {
                            var light = ReworkedAbyssLighting.lights[index];
                            light.center = npc.Center;
                            light.lifetime = 60;
                            light.rotation = npc.rotation;
                            light.color = Color.White;
                            light.scale = npc.scale;
                            light.opacity = 0;
                            ReworkedAbyssLighting.lights[index] = light;


                        }
                        else
                        {
                            HasMadeLight = false;
                        }
                            Lighting.AddLight(npc.Center, TorchID.Ice);
                        npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(Main.LocalPlayer.Center), 0.2f);


                        npc.spriteDirection = (npc.Center - Main.LocalPlayer.Center).X.DirectionalSign();
                        if(Main.GameUpdateCount % 10 <= 1)
                        {

                            VoidstoneScreamParticle particle = new();
                            particle.Prepare(npc.Center+ npc.rotation.ToRotationVector2()*npc.width/2, npc.rotation.ToRotationVector2(), Color.Aqua, 0.4f, 60);
                            ParticleEngine.ShaderParticles.Add(particle);
                        }


                        ScreamTimer++;

                        if(ScreamTimer> MAX_SCREAM_TIME)
                        {
                            CurrentState = state.SwimAroundWaterWithoutBumpingIntoWalls;
                        }


                        //todo: find/manifest hostile npcs to come to this location, and then fuck off as fast as possible


                    }
                    break;

                case state.Fuck_off_Before_Predators_Rip_you_to_shreds:

                    break;
            }
        }

        void UpdateSoundLoop(NPC npc)
        {
            if (ScreamLoop is not null)
            {
                ScreamLoop.Update(npc.Center, (a) =>
                {
                    a.Volume = Interpolant;
                    a.Pitch = Interpolant;
                });
            }
        }

        public override bool OverrideFindFrame(NPC NPC)
        {
            NPC.frame.Y = (CurrentState == state.Scream) ? NPC.height : 0;
            return true;
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 DrawPos = npc.Center - screenPos;

            var texture = TextureAssets.Npc[this.NPCType].Value;
            var glowTex = GlowTex.Value;
            SpriteEffects flip = (-npc.spriteDirection).ToSpriteDirection();
            //flip = flip ^( npc.rotation > MathHelper.Pi? SpriteEffects.FlipVertically: SpriteEffects.None);
            Main.EntitySpriteDraw(texture, DrawPos, npc.frame, drawColor, npc.rotation, npc.frame.Size() / 2, npc.scale, flip);
            Main.EntitySpriteDraw(glowTex, DrawPos, npc.frame, Color.White, npc.rotation, npc.frame.Size() / 2, npc.scale, flip);


            //NpcBrain.DrawContextDebug(spriteBatch, DrawPos);    
            return false;
        }


    }
}
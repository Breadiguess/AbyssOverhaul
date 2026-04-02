using AbyssOverhaul.Content.BehaviorOverrides.VoidstoneScreamerNPC;
using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Sounds;


namespace AbyssOverhaul.Content.BehaviorOverrides
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

        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            typeName = Language.GetOrRegister("Mods.AbyssOverhaul.NPCOverrides.VoidstoneScreamer").Value;
        }
        public override string TexturePath => $"AbyssOverhaul/Content/BehaviorOverrides/VoidstoneScreamerNPC/VoidstoneScreamer";

        public LoopedSoundInstance? ScreamLoop;
        public override void SetDefaults(NPC npc)
        {
            npc.noTileCollide = false;

            npc.lifeMax = 12_000;

        }
        public override void Load()
        {

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
        public override bool OverrideAI(NPC npc)
        {
            npc.noGravity = npc.wet;

            if (Collision.CanHit(npc, Main.LocalPlayer))
            {
                Interpolant = float.Lerp(Interpolant, 1, 0.1f);
                if (ScreamLoop is null || ScreamLoop.HasBeenStopped)
                {
                    ScreamLoop = LoopedSoundManager.CreateNew(Assets.Sounds.NPCs.VoidstoneScreamer.ScreamLoop.Asset, () => npc == null || !npc.active);
                }
                else
                {
                    if (Main.GameUpdateCount % 50 <= 1)
                    {

                        VoidstoneScreamParticle particle = new();
                        particle.Prepare(npc.Center, Vector2.zeroVector, Color.Aquamarine, 0.2f, 20);
                        ParticleEngine.ShaderParticles.Add(particle);
                    }
                }
            }
            else
            {
                Interpolant = float.Lerp(Interpolant, 0, 0.1f);
            }

            StateMachine(npc);
            UpdateSoundLoop(npc);

            return true;
        }



        void StateMachine(NPC npc)
        {
            switch (CurrentState)
            {
                case state.FlopAroundOnLand:

                    break;
                case state.SwimAroundWaterWithoutBumpingIntoWalls:

                    break;
                case state.SpotIntruder:
                    break;
                case state.Investigate:
                    break;
                case state.Scream:
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

            SpriteEffects flip = npc.spriteDirection.ToSpriteDirection();
            Main.EntitySpriteDraw(texture, DrawPos, npc.frame, drawColor, npc.rotation, npc.frame.Size() / 2, npc.scale, flip);


            //NpcBrain.DrawContextDebug(spriteBatch, DrawPos);    
            return false;
        }


    }
}
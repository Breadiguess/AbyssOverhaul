using BreadLibrary.Core.Sounds;
using Terraria.Audio;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.BehaviorOverrides
{
    public sealed class VoidstoneScreamer : NPCBehaviorOverride, IEcologyParticipant
    {
        public void SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.None);
        }

        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {

        }
        public override string TexturePath => this.GetPath();
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.LuminousCorvina>();


        public static readonly SoundStyle ScreamSound = new("CalamityMod/Sounds/Custom/CorvinaScream");


        public enum state
        {
            FlopAroundOnLand,
            SwimAroundWaterWithoutBumpingIntoWalls,
            SpotIntruder,
            Investigate,

            Scream
        }
        //public override string TexturePath => $"{this.GetPath}";

        public LoopedSoundInstance? ScreamLoop;
        public override void SetDefaults(NPC npc)
        {
            npc.noTileCollide = false;

            npc.lifeMax = 12_000;


        }
        public override void Load()
        {

            Main.npcFrameCount[NPCType] = 1;
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            base.OnSpawn(npc, source);


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
                    ScreamLoop= LoopedSoundManager.CreateNew(Assets.Sounds.NPCs.VoidstoneScreamer.ScreamLoop.Asset, () => npc == null || !npc.active);
                }
                else
                {
                   
                }
            }
            else
            {
                Interpolant = float.Lerp(Interpolant, 0, 0.1f);
            }



            if(ScreamLoop is not null)
            {
                ScreamLoop.Update(npc.Center, (a) =>
                {
                    a.Volume = Interpolant;
                    a.Pitch = Interpolant;
                });
            }
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
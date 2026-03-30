using CalamityMod.Particles;

namespace AbyssOverhaul.Content.NPCs.CragheadNPC
{
    internal class Craghead : ModNPC, IEcologyParticipant
    {
        #region Values

        public void SetupEcology(NPC npc, EcologyGlobalNPC ecology)
        {
            ecology.SpeciesTraits.Add(NpcTraitFlags.Territorial);
        }
        #region OreType
        public enum OreType
        {
            None,
            Iron,
            Scoria,
            IronBoot
        }
            
        public OreType HeadMaterial
        {
            get => (OreType)NPC.ai[3];
            set => NPC.ai[3] = (float)value;
        }
        #endregion

        public enum Behavior
        {
            Debug,
            DefendTerritory,
            RamEntity
        }

        public Behavior CurrentState
        {
            get => (Behavior)NPC.ai[2];
            set => NPC.ai[2] = (float)value;
        }
        public bool LostHeadMaterial;


        #endregion
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = 34_000;
            NPC.Size = new Vector2(60, 40);
            NPC.noTileCollide = false;

        }   

        #region AI
        public override bool PreAI()
        {
            if (HeadMaterial == OreType.None && !LostHeadMaterial)
            {

                int Value = Main.rand.Next(1, 3);
                HeadMaterial = (OreType)Value;
            }

            return base.PreAI();
        }
        public override void AI()
        {

        }

        public override void PostAI()
        {
            UpdateVisualEffects(HeadMaterial);
        }

        #endregion

        #region DrawCode
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            Utils.DrawBorderString(spriteBatch, HeadMaterial.ToString(), NPC.Center - screenPos, drawColor);
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        #endregion

        #region Helpers
        private void UpdateVisualEffects(OreType type)
        {
            switch (type)
            {
                case OreType.None:
                    break;

                case OreType.Iron:
                    break;
                case OreType.Scoria:
                    Vector2 SpawnPos = new Vector2(NPC.width *0.5f * -NPC.spriteDirection, 0) + NPC.Center;
                    Vector2 Direction = new Vector2();
                    MediumMistParticle mist = new MediumMistParticle(SpawnPos, Direction,
                    Main.rand.NextBool(3) ? Color.LightSteelBlue : Color.SteelBlue, Color.LightSlateGray, Main.rand.NextFloat(0.4f, 0.65f), 130);
                    GeneralParticleHandler.SpawnParticle(mist);
                    break;

                case OreType.IronBoot:
                    break;
            }
        }

        public void HandleImpactEvent()
        {

        }

    

        #endregion
    }
}

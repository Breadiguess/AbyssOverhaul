using BreadLibrary.Core.SoftBodySim;
using BreadLibrary.Core.Verlet;
using CalamityMod;

namespace AbyssOverhaul.Content.BehaviorOverrides.MirageJellyifshNPC
{
    internal class MirageJellyfish : NPCBehaviorOverride
    {
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.MirageJelly>();


        public const int MAX_TENDRILS = 6;
        public List<VerletChain> Tendrils;
        public SoftbodyInstance Body;

        private void CreateBody(NPC NPC)
        {
            SoftbodySim sim = new();

            sim.Mat.Iterations = 3;
            sim.Mat.Damping = 0.55f;
            sim.Mat.ShapeMatchingStiffness = 0.029f;
            sim.Mat.StructuralStiffness = 0.2f;
            sim.Mat.BendStiffness = 0.07f;
            sim.Mat.ShapeMatchingStiffness = 0.15f;
            sim.Mat.StructuralStiffness = 0.30f;
            sim.Mat.BendStiffness = 0.10f;

            Body = new SoftbodyInstance(sim);

            Body.CreateEllipseBody(
                center: NPC.Top,
                count: 40,
                radiusX: 60f,
                radiusY: 50f,
                mass: 3f,
                nodeRadius: 4f
            );

            Body.DriverMode = SoftbodyInstance.TransformDriverMode.EntityCenter;
            Body.DriverEntity = NPC;
            Body.Collision.CollideWithPlayers = true;
            Body.Collision.EntityBounce = 0f;
            Body.Collision.IgnoreDriverEntity = true;
            Body.Collision.PlayerPushFactor = 1f;
            SoftbodySystem.Instances.Add(Body);
        }

        public override void OnSpawn(NPC NPC, IEntitySource source)
        {

        }
        public override void SetDefaults(NPC NPC)
        {
            base.SetDefaults(NPC);
            NPC.Size = new Vector2(100, 90);
        }
        public override bool OverrideAI(NPC NPC)
        {
            if (Body is null)
                CreateBody(NPC);

            if (Tendrils is null)
            {
                Tendrils = new(MAX_TENDRILS);
                for (int i = 0; i < MAX_TENDRILS; i++)
                    Tendrils.Add(new(30, 4, NPC.Center));
            }


            NPC.velocity = Vector2.UnitY * MathF.Cos(Main.GameUpdateCount * 0.01f)*1.4f;
          

            for (int i = 0; i< Tendrils.Count; i++)
            {
                var t = Tendrils[i];


                Vector2 root = Vector2.Lerp(NPC.BottomRight, NPC.BottomLeft, i / (float)MAX_TENDRILS);
                t.Simulate(Vector2.zeroVector, root, NPC.gravity, 0.6f, collideWithTiles: false);
            }

            Body.Sim.Mat.Iterations = 5;
            Body.DriverMode = SoftbodyInstance.TransformDriverMode.EntityCenter;


            return true;
        }


        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            if(Tendrils is not null)

                foreach (var t in Tendrils)
            {
                for(int i = 0; i< t.Positions.Length-1; i++)
                {

                    Vector2 start = t.Positions[i];
                    Vector2 end = t.Positions[i + 1];
                    Utilities.DrawLineBetter(spriteBatch, start, end, Color.Cyan, 4f);
                }
            }



            return false;
        }
    }
}

using AbyssOverhaul.Core.NPCOverrides;
using BreadLibrary.Core;
using BreadLibrary.Core.Verlet;
using CalamityMod.NPCs.Abyss;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.BehaviorOverrides.ReaperShark
{
    internal class ReaperSharkOverride : NPCBehaviorOverride
    {
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.ReaperShark>();



        public List<VerletChain> Dreadlocks;

        public ReaperSharkArm[] arms;



        public override void SetDefaults(NPC NPC)
        {
            NPC.lifeMax = 4000;
            NPC.defense = 4000;
            Dreadlocks = new List<VerletChain>();
            for(int i = 0; i< 7; i++)
            Dreadlocks.Add(new(20, 4, NPC.Center));

            var a = new IKSkeleton.JointSetup(40, 0, MathHelper.TwoPi);
            IKSkeleton skeleton = new IKSkeleton(a);
            arms = new ReaperSharkArm[2];
            arms[0] = new ReaperSharkArm(skeleton);

            arms[1] = new ReaperSharkArm(skeleton);
        }


        public override bool OverrideAI(NPC NPC)
        {


            for(int i = 0; i< arms.Length; i++)
            {
                var a = arms[i];
                
                a.Skeleton.Update(NPC.Center, Main.MouseWorld);
                arms[i] = a;
            }


            for(int i = 0; i< Dreadlocks.Count; i++)
            {
                var t = Dreadlocks[i];
                Vector2 Velocity = new Vector2(0,-1).RotatedBy(MathF.Cos(i+Main.GameUpdateCount*0.005f));
                t.Simulate(Velocity, NPC.Center + new Vector2(Dreadlocks.Count-i,0)*10, -1, 0.7f);
            }



            return true;
        }



        public override bool PreDraw(NPC NPC, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawDreadlocks(spriteBatch);
            DrawArms();
            return false;
        }
        void DrawArms()
        {
            if(arms is not null)
            {
                for(int i = 0; i < arms.Length; i++)
                {
                    var t = arms[i].Skeleton;

                    for(int x = 0; x< t.PositionCount; x++)
                    {
                        Vector2 start = t.Position(x);
                        Vector2 end = t.Position(x + 1);

                        Utils.DrawLine(Main.spriteBatch, start, end, Color.Black, Color.Black, 10);
                    }
                  
                }
            }
        }

        void DrawDreadlocks(SpriteBatch spriteBatch)
        {
            if(Dreadlocks is not null)
            {
                for(int x = 0; x< Dreadlocks.Count; x++)
                {
                    var t = Dreadlocks[x];
                    for(int i = 0; i< t.Positions.Length-1; i++)
                    {
                        Vector2 start = t.Positions[i];
                        Vector2 end = t.Positions[i+1];

                        Utils.DrawLine(spriteBatch, start, end, Color.Black, Color.Black, 10);
                        //Utilities.DrawLineBetter(spriteBatch, start, end, Color.Black, 10);
                    }
                }
            }
        }
    }
}

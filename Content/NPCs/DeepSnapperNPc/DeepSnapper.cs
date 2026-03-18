using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Content.NPCs.Critters.VoidDreamerNPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.NPCs.DeepSnapperNPc
{
    internal class DeepSnapper : ModNPC
    {
        public ModularNpcBrain<SchoolingNpcContext> NpcBrain;
        public override void SetDefaults()
        {
            NPC.lifeMax = 1200;
            NPC.Size = new Vector2(30);

        }

        private void InitializeAnythingMissing()
        {
            SchoolingNpcContext context = new()
            {
                NeighborRadius = 220f,
                SeparationRadius = 52f,
                DesiredSchoolDistanceFromTarget = 180f
            };


            NpcBrain = new(context);

            NpcBrain.Sensors.Add(new SchoolingSensor
            {
                SameTypeOnly = true,
                RequireLineOfSight = true,

            });

            NpcBrain.Modules.Add(new AvoidSameTypeModule());
            NpcBrain.Modules.Add(new SchoolingMovementModule()
            {
                
            });
            NpcBrain.Modules.Add(new PatrolModule()
            {
                PatrolPoints = new Vector2[]
                {
                    NPC.Center - Vector2.UnitX*540,
                    NPC.Center + Vector2.UnitX *140
                },
                MoveSpeed = 10,
                Score = 60

            });
        }

        public override bool PreAI()
        {
            NPC.noGravity = true;
            if(NpcBrain is null)
            InitializeAnythingMissing();
            return base.PreAI();
        }

        public override void AI()
        {
            if(NpcBrain is not null)
            {
                NpcBrain.Update(npc:NPC);
                //NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.velocity.RotatedBy(NPC.AngleTo(Main.MouseWorld)), 0.2f);
            }
        }
        public override void PostAI()
        {
            base.PostAI();
        }
    }
}

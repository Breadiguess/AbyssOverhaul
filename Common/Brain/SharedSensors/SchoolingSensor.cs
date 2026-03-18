using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    internal class SchoolingSensor : INpcSensor<SchoolingNpcContext>
    {
        public bool SameTypeOnly { get; set; } = true;
        public bool RequireLineOfSight { get; set; } = true;
        
        void INpcSensor<SchoolingNpcContext>.Update(SchoolingNpcContext context)
        {
            NPC self = context.Self;

            if (self is null || !self.active)
                return;

            Vector2 selfCenter = self.Center;
            float neighborRadiusSq = context.NeighborRadius * context.NeighborRadius;
            float separationRadiusSq = context.SeparationRadius * context.SeparationRadius;

            Vector2 accumulatedCenter = Vector2.Zero;
            Vector2 accumulatedVelocity = Vector2.Zero;
            Vector2 accumulatedSeparation = Vector2.Zero;

            int neighbors = 0;

            for(int i = 0; i< Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                    if(other is null || !other.active) continue;

                if (SameTypeOnly && other.type != self.type)
                    continue;

                Vector2 offset = other.Center - self.Center;
                float distSq = offset.LengthSquared();

                if (distSq > neighborRadiusSq || distSq < 0.0001f)
                    continue;

                if (RequireLineOfSight && !Collision.CanHitLine(self.position, self.width, self.height, other.position, other.width, other.height))
                    continue;

                float dist = MathF.Sqrt(distSq);

                neighbors++;
                accumulatedCenter += other.Center;
                accumulatedVelocity += other.velocity;

                if(dist < context.NearestNeighborDistance)
                {
                    context.NearestNeighborDistance = dist;
                    context.nearestNeighbor = other;
                }

                if(distSq <= separationRadiusSq)
                {
                    Vector2 away = self.Center - other.Center;
                    if (away != Vector2.Zero)
                        accumulatedSeparation += away / dist;
                }


            }
            context.NeighborCount = neighbors;
            if (neighbors > 0)
            {
                context.GroupCenter = accumulatedCenter / neighbors;
                context.AverageNeighborVelocity = accumulatedSeparation / neighbors;

                context.SeparationVector = accumulatedSeparation.SafeNormalize(Vector2.Zero);
                context.AlignmentVector = context.AverageNeighborVelocity.SafeNormalize(Vector2.Zero);
                context.CohesionVector2 = context.GroupCenter.SafeNormalize(Vector2.Zero);


            }
        }

       
    }
}

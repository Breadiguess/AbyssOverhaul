using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    internal class SchoolingMovementModule : INpcModule<SchoolingNpcContext>
    {
        public float SeparationWeight { get; set; } = 1.8f;
        public float AlignmentWeight { get; set; } = 0.9f;
        public float CohesionWeight { get; set; } = 1.1f;
        public float TargetWeight { get; set; } = 1.35f;
        public float MaxMoveSpeed { get; set; } = 4.5f;
        public float BaseScore { get; set; } = 3f;
        public float MinNeighborsToActivate { get; set; } = 1f;

        NpcDirective INpcModule<SchoolingNpcContext>.Evaluate(SchoolingNpcContext context)
        {
            if (context.Self is null || !context.Self.active)
                return NpcDirective.None;

            if (context.NeighborCount < MinNeighborsToActivate)
                return NpcDirective.None;

            Vector2 Desired = Vector2.Zero;

            if (context.SeparationVector != Vector2.Zero)
                Desired += context.SeparationVector * SeparationWeight;

            if (context.AlignmentVector != Vector2.Zero) 
                Desired += context.AlignmentVector * AlignmentWeight;

            if (context.CohesionVector2 != Vector2.Zero)
                Desired += context.CohesionVector2 * CohesionWeight;

            if (context.HasSchoolTarget)
            {
                Vector2 toTarget = context.SchoolTargetPosition - context.Self.Center;

                float targetDist = toTarget.Length();

                if (targetDist > 0.001f)
                {
                    toTarget /= targetDist;
                    float desiredDist = context.DesiredSchoolDistanceFromTarget;
                    float targetInfluence = targetDist > desiredDist + 24f ? 1f : targetDist < desiredDist - 24f ? -0.6f : 0f;
                    Desired += toTarget * targetInfluence * TargetWeight;
                }
            }
            if(Desired.LengthSquared() <= 0.0001f)
                return NpcDirective.None;

            Desired.Normalize();
            Desired *= MaxMoveSpeed;
            return new NpcDirective()
            {
                WantsControl = true,
                Score = BaseScore + context.NeighborCount * 0.35f,
                DesiredVelocity = Desired,

               
            };

        }
    }
}

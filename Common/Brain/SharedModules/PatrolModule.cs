using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class PatrolModule : INpcModule<NpcContext>   
    {
        public Vector2[] PatrolPoints;
        public int CurrentIndex;
        public float MoveSpeed = 2f;
        public float ArrivalDistance = 20f;
        public float Score = 5f;
        public string Name = "Patrol";

        public PatrolModule(params Vector2[] patrolPoints)
        {
            PatrolPoints = patrolPoints;
        }

        public NpcDirective Evaluate(NpcContext context)
        {
            if (PatrolPoints == null || PatrolPoints.Length == 0)
                return NpcDirective.None;

            Vector2 target = PatrolPoints[CurrentIndex];
            Vector2 toTarget = target - context.Self.Center;
            float dist = toTarget.Length();

            if (dist <= ArrivalDistance)
            {
                CurrentIndex++;
                if (CurrentIndex >= PatrolPoints.Length)
                    CurrentIndex = 0;

                target = PatrolPoints[CurrentIndex];
                toTarget = target - context.Self.Center;
            }
            Dust.NewDustPerfect(target, DustID.Cloud);

            Vector2 desiredVel = toTarget.SafeNormalize(Vector2.Zero) * MoveSpeed;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = desiredVel,
                DebugName = Name,
                DebugInfo = $"Patrolling to point {CurrentIndex + 1}/{PatrolPoints.Length}"
            };
        }
    }
}

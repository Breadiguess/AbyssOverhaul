using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class MoveToPointModule : INpcModule<NpcContext>
    {
        public float Score = 20f;
        public float MoveSpeed = 2.5f;
        public float ArrivalDistance = 24f;
        public string Name = "MoveToPoint";

        public NpcDirective Evaluate(NpcContext context)
        {
            if (!context.HasTargetPoint)
                return NpcDirective.None;

            Vector2 toTarget = context.TargetPoint - context.Self.Center;
            float dist = toTarget.Length();

            if (dist <= ArrivalDistance)
            {
                return new NpcDirective
                {
                    WantsControl = true,
                    Score = Score * 0.25f,
                    DesiredVelocity = Vector2.Zero,
                    DebugName = Name,
                    DebugInfo = $"Arrived ({dist:0.0}px away)"
                };
            }

            Vector2 desiredVel = toTarget.SafeNormalize(Vector2.Zero) * MoveSpeed;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = desiredVel,
                DebugName = Name,
                DebugInfo = $"Moving to target ({dist:0.0}px away)"
            };
        }
    }
}


using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class IdleHomeModule : INpcModule<NpcContext>
    {
        public float MoveSpeed = 1.5f;
        public float ReturnDistance = 80f;

        public NpcDirective Evaluate(NpcContext context)
        {
            Vector2 toHome = context.HomePosition - context.Self.Center;
            float dist = toHome.Length();

            if (dist < ReturnDistance)
            {
                return new NpcDirective
                {
                    WantsControl = true,
                    Score = 1f,
                    DesiredVelocity = context.Self.velocity * 0.9f,
                    DebugName = "IdleHome"
                };
            }

            return new NpcDirective
            {
                WantsControl = true,
                Score = 5f,
                DesiredVelocity = toHome.SafeNormalize(Vector2.Zero) * MoveSpeed,
                DebugName = "ReturnHome"
            };
        }
    }
}

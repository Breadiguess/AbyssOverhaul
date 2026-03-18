using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class IdleModule : INpcModule<NpcContext>
    {
        public float SlowFactor = 0.92f;
        public float Score = 0f;
        public string Name = "Idle";

        public NpcDirective Evaluate(NpcContext context)
        {
            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = context.Self.velocity * SlowFactor,
                DebugName = Name,
                DebugInfo = "No higher-priority task"
            };
        }
    }
}

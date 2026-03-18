using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class FleeThreatModule<TContext> : INpcModule<TContext>
    where TContext : NpcContext, IThreatAware
    {
        public float Score = 25f;
        public float MoveSpeed = 4f;
        public float MinThreatLevel = 0.2f;

        public NpcDirective Evaluate(TContext context)
        {
            if (!context.HasThreat || context.ThreatLevel < MinThreatLevel)
                return NpcDirective.None;

            Vector2 away = context.Self.Center - context.ThreatPosition;
            if (away == Vector2.Zero)
                return NpcDirective.None;

            away.Normalize();

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score + context.ThreatLevel * 20f,
                DesiredVelocity = away * MoveSpeed,
                DebugName = "FleeThreat",
                DebugInfo = $"Threat={context.ThreatLevel:0.00}"
            };
        }
    }
}
